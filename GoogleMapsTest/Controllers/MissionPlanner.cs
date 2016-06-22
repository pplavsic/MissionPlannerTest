using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GoogleMapsTest.Models;

namespace GoogleMapsTest.Controllers
{
    public struct GPSCoordinates
    {
        public double lat;
        public double lon;
    }

    public class MissionPlanner
    {
        // Niz GPS koordinata poligona
        private GPSCoordinates[] vert = new GPSCoordinates[0];
        private GPSCoordinates[] wpArray = new GPSCoordinates[0];
        private int[] wpNumPerStripe = new int[0];

        // Gopro Hero nema tacnu rezoluciju 4:3, pa moraju da se koriste 
        // uglovi alfa=71.4 za X (siri deo slike) i beta=53.74 za Y (uzi deo slike)
        // X = 2 * h * tg(alfa/2)
        // Y = 2 * h * tg(beta/2)
        // gde je h visina leta, tg(alfa/2) = 0.71855501, tg(alfa/2) = 0.50666667

        // Dimenzije slike na zemlji
        // X JE DUZA STRANICA SLIKE, TJ. KRETANJE JE U SMERU Y STRANICE SLIKE
        private double tgAlfaPola = 0.71855501;
        private double tgBetaPola = 0.50666667;

        private double Lx;
        private double Ly;

        // Rastojanje izmedju dva centra slike
        private double dx;
        private double dy;

        // Korigovano rastojanje izmedju dva centra slike
        private double deltaX;
        private double deltaY;

        public FlightPoint[] PlanMission(double[] lattitude, double[] longitude, float altitude, long missionFK)
        {
            double preklopX = 0.3; // 0.3 = 30%
            double preklopY = 0.6; // 0.6 = 60%

            Lx = 2 * altitude * tgAlfaPola;
            Ly = 2 * altitude * tgBetaPola;

            dx = (1 - preklopX) * Lx;
            dy = (1 - preklopY) * Ly;

            int vIndex;
            double heading;

            double dmax;
            int n;
            
            initCoord(lattitude, longitude);
            
            // Proveri da li je CW ili CCW poligon i okreni ga ako je CCW jer mora biti CW
            if (!checkIfPolyCW(vert)) vert = reversePoly(vert);

            GPSCoordinates[] newPoly = vert;
            //GPSCoordinates[] testPoly = new GPSCoordinates[0];


            vIndex = planFirstStripe(newPoly, out heading);            
            
            dmax = findFarthestVertexDistance(vert, vIndex);
            // n je broj prolaza
            n = (int)((dmax-(Lx-dx)) / dx) + 1;
            deltaX = (dmax - Lx) / (n - 1);

            // Vec smo uradili prvi stripe, pa idemo od 1 do n (n-1) puta
            // Svaki srtipe drugi se okrece redosled wp-ova
            for (int i = 1; i < n; i++)
            {
                newPoly = findNewPolygon(newPoly, vIndex, wpArray, heading);
                vIndex = planStripe(newPoly, heading);
            }

            int curIndex = 0;
            for(int i = 0; i < wpNumPerStripe.Length; i++)
            {
                if(i % 2 == 1)
                {
                    
                    GPSCoordinates[] tempArray = new GPSCoordinates[wpNumPerStripe[i]];
                    Array.Copy(wpArray, curIndex, tempArray, 0, wpNumPerStripe[i]);
                    for (int j = 0; j < wpNumPerStripe[i]; j++)
                    {
                        wpArray[curIndex + wpNumPerStripe[i] - 1 - j] = tempArray[j];
                    }
                }
                curIndex += wpNumPerStripe[i];
            }
           
            return convertToFlightPoint(heading, altitude, missionFK);
        }

        public FlightPoint[] reverseMission(FlightPoint[] fps)
        {
            FlightPoint[] reverseArray = new FlightPoint[fps.Length];

            for(int i = 0; i < fps.Length; i++)
            {
                reverseArray[i] = fps[fps.Length - 1 - i];
            }

            return reverseArray;
        }

        public FlightPoint[] mirrorMission(FlightPoint[] fps)
        {
            FlightPoint[] mirrorArray = new FlightPoint[fps.Length];

            GPSCoordinates v1 = new GPSCoordinates();
            GPSCoordinates v2 = new GPSCoordinates();

            if (fps.Length < 2) return null;

            double heading = fps[0].Target_yaw;
            double startHeading = 0;
            double reverseStartHeading = 0;

            v1.lat = fps[0].Latitude;
            v1.lon = fps[0].Longitude;

            v2.lat = fps[1].Latitude;
            v2.lon = fps[1].Longitude;

            startHeading = calcHeading(v1, v2);
            if (startHeading < 0) reverseStartHeading = 180 + startHeading;
            else reverseStartHeading = -180 + startHeading;

            int curStripeStartIndex = 0;
            int numOfWpInStripe = 1;
            bool endOfMission = false;

            int i = 0;
            while (!endOfMission)
            {
                numOfWpInStripe = 1;

                for (i = curStripeStartIndex; i < fps.Length; i++)
                {
                    v1.lat = fps[i].Latitude;
                    v1.lon = fps[i].Longitude;

                    if (i + 1 < fps.Length)
                    {
                        v2.lat = fps[i + 1].Latitude;
                        v2.lon = fps[i + 1].Longitude;
                    }
                    else
                    {
                        endOfMission = true;
                        break;
                    }

                    // ako se heading i target_yaw poklapaju, odnosno ako su supotni ali istog pravca
                    // Kada pocne sledeci stripe, heading ce biti pomeren za 90
                    if ((Math.Abs(calcHeading(v1, v2) - heading) < 2) ||
                        ((Math.Abs(calcHeading(v1, v2) - heading) > 178) && (Math.Abs(calcHeading(v1, v2) - heading) < 182)))
                    {
                        numOfWpInStripe++;
                    }
                    else break;
                }         
                         
                // Okreni ovaj stripe i okreni mu Target_Yaw
                for (int j = 0; j < numOfWpInStripe; j++)
                {
                    mirrorArray[curStripeStartIndex + j] = fps[curStripeStartIndex + numOfWpInStripe - 1 - j];
                    mirrorArray[curStripeStartIndex + j].Target_yaw = (short)reverseStartHeading;
                }

                // U prvom prolazu treba numOfWpInStripe - 1, ali + 1 da bi presli na prvi u sledecem stripe-u
                if (curStripeStartIndex == 0) curStripeStartIndex = numOfWpInStripe - 1 + 1;
                // Da bi poceo od prvog u sledecem stripe-u dodajemo + 1
                else curStripeStartIndex += numOfWpInStripe;

                if (i >= fps.Length - 1) endOfMission = true;
                
            }

            return mirrorArray;
        }

        private void initCoord(double[] lattitude, double[] longitude)
        {
            Array.Resize(ref vert, lattitude.Length);

            for(int i = 0; i < lattitude.Length; i++)
            {
                vert[i] = new GPSCoordinates();
                vert[i].lat = lattitude[i];
                vert[i].lon = longitude[i];
            }
        }

        private bool checkIfPolyCW(GPSCoordinates[] ver)
        {
            bool flagCW = true;

            double heading1 = calcHeading(ver[0], ver[1]);
            double reverseHeading1;
            if (heading1 < 0) reverseHeading1 = 180 + heading1;
            else reverseHeading1 = -180 + heading1;

            double heading2 = calcHeading(ver[1], ver[2]);

            if(heading1 > 0)
            {
                if (heading2 > 0)
                {
                    if (heading2 > heading1) flagCW = true;
                    else flagCW = false;                    
                }
                else
                {
                    if (heading2 < reverseHeading1) flagCW = true;
                    else flagCW = false;
                }
            }
            else
            {
                if (heading2 < 0)
                {
                    if (heading2 > heading1) flagCW = true;
                    else flagCW = false;
                }
                else
                {
                    if (heading2 < reverseHeading1) flagCW = true;
                    else flagCW = false;
                }
            }

            return flagCW;
        }

        private GPSCoordinates[] reversePoly(GPSCoordinates[] ver)
        {
            GPSCoordinates[] revArray = new GPSCoordinates[ver.Length];

            for(int i = 0; i < ver.Length; i++)
            {
                revArray[i] = ver[ver.Length - 1 - i];
            }

            return revArray;
        }

        private FlightPoint[] convertToFlightPoint(double heading, float alt, long missionFK)
        {
            FlightPoint[] fps = new FlightPoint[wpArray.Length];
            FlightPoint fp = new FlightPoint();

            for (int i = 0; i < wpArray.Length; i++)
            {
                fp = new FlightPoint();
                fp.MissionFK = missionFK;
                fp.Latitude = wpArray[i].lat;
                fp.Longitude = wpArray[i].lon;
                fp.Altitude = alt;
                fp.Target_yaw = (short)heading;
                fp.PIAction = PIAction.TakePicture;

                fp.Damping_distance = 0;
                fp.Target_gimbal_pitch = 0;
                fp.Turn_mode = TurnMode.Clockwise;
                fp.Has_action = HasAction.No_Action;
                fp.Action_time_limit = 5;
                fp.ACTION_action_repeat = 1;
                fp.ACTION_Command_list = new List<int>(1) { 1 };
                fp.ACTION_Param_list = new List<int>(1) { 1 };

                fps[i] = fp;
            }

            return fps;
        }

        // Calculate distance between two GPS points in meters
        private double calcDistance(GPSCoordinates v1, GPSCoordinates v2)
        {
            double C_EARTH = 6378137.0; // in meters

            double latti_diff = (v1.lat - v2.lat) * Math.PI / 180;
            double longi_diff = (v1.lon - v2.lon) * Math.PI / 180;

            double x = latti_diff * C_EARTH;
            double y = longi_diff * C_EARTH * Math.Cos(((v1.lat + v2.lat) * Math.PI / 180) / 2.0);

            double d = Math.Sqrt(x * x + y * y);

            return d;
        }

        // Move GPS point for "dist" meters from v1 in heading direction
        private GPSCoordinates moveGPS(GPSCoordinates v1, double dist, double heading)
        {
            // Ovo radi samo za North/East deo Zemlje
            double C_EARTH = 6378137.0;
            double movDirect = 0;
            GPSCoordinates result = new GPSCoordinates();

            double alfa = 0;
            double x, y;

            if (Math.Abs(heading) < 90) alfa = Math.Abs(heading);
            else alfa = 180 - Math.Abs(heading);

            x = dist * Math.Cos(alfa * Math.PI / 180);
            y = dist * Math.Sin(alfa * Math.PI / 180);

            double latti_diff = (x / C_EARTH) * 180 / Math.PI;
            double longi_diff = (y / (C_EARTH * Math.Cos(v1.lat))) * 180 / Math.PI;

            movDirect = heading;

            if (movDirect >= 0 && movDirect < 90)
            {
                result.lat = v1.lat + latti_diff;
                result.lon = v1.lon + longi_diff;
            }
            else if (movDirect >= 90 && movDirect < 180)
            {
                result.lat = v1.lat - latti_diff;
                result.lon = v1.lon + longi_diff;
            }
            else if (movDirect <= 0 && movDirect > -90)
            {
                result.lat = v1.lat + latti_diff;
                result.lon = v1.lon - longi_diff;
            }
            else if (movDirect <= -90 && movDirect > -180)
            {
                result.lat = v1.lat - latti_diff;
                result.lon = v1.lon - longi_diff;
            }

            return result;
        }

        // Calculate GPS that is (distX, distY) meters from v1 in heading direction
        private GPSCoordinates calcFirstWP_GPS(GPSCoordinates v1, double distX, double distY, double heading)
        {

            // Ovo radi samo za North/East deo Zemlje
            double C_EARTH = 6378137.0;
            double xDirect = 0;
            GPSCoordinates result = new GPSCoordinates();

            double latti_diff = (distX / C_EARTH) * 180 / Math.PI;
            double longi_diff = (distY / (C_EARTH * Math.Cos(v1.lat))) * 180 / Math.PI;

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // Kada se tacke poligona gledaju u smeru kazaljke na satu onda trba + 90
            // U suprotnom treba - 90, ali to nije uradjeno i za sada se uzima uvek u smeru kazaljke na satu.
            //////////////////////////////////////////////////////////////////////////////////////////////////
            xDirect = heading + 90;
            if (xDirect < -180) xDirect = 360 - xDirect;
            else if (xDirect > 180) xDirect = -360 + xDirect;

            // Pomeri po stranici (y dimenzija slike)
            result = moveGPS(v1, distY, heading);

            // Pomeri po normalno na stranicu (x dimenzija slike)
            result = moveGPS(result, distX, xDirect);

            return result;
        }

        // Calculates HEADING (-180, 180) from two GPS points. Returns -1 if GPS points are the same.
        private double calcHeading(GPSCoordinates v1, GPSCoordinates v2)
        {
            double heading = 0;

            if (v1.lat == v2.lat && v1.lon == v2.lon) heading = -1;
            else
            {
                heading = (Math.Atan2(Math.Sin(v2.lon - v1.lon) * Math.Cos(v2.lat),
                            Math.Cos(v1.lat) * Math.Sin(v2.lat) - Math.Sin(v1.lat) * Math.Cos(v2.lat) * Math.Cos(v2.lon - v1.lon))) * 180 / Math.PI;
                // Ovaj deo pravi da daje od 0 do 360 ali nama ovde treba od -180 do 180
                //if (heading < 0) heading += 360; 
            }

            return heading;
        }

        private double calcVertexAngle(GPSCoordinates[] ver, int verIndex)
        {
            ///////////////////////////////////////
            // Ne radi u slucaju konkavnih oblika
            ///////////////////////////////////////
            double angle = 0;
            double angle1 = 0;
            double angle2 = 0;

            if (verIndex == 0)
            {
                angle1 = calcHeading(ver[verIndex], ver[verIndex + 1]);
                angle2 = calcHeading(ver[verIndex], ver[ver.Length - 1]);
            }
            else if (verIndex == ver.Length - 1)
            {
                angle1 = calcHeading(ver[verIndex], ver[0]);
                angle2 = calcHeading(ver[verIndex], ver[verIndex - 1]);
            }
            else
            {
                angle1 = calcHeading(ver[verIndex], ver[verIndex + 1]);
                angle2 = calcHeading(ver[verIndex], ver[verIndex - 1]);
            }

            // Ovde ne radi ako su uglovi veci od 180, sto je slucaj kod konkavno nepravilnoh poligona
            if ((angle1 >= 0 && angle2 <= 0) || (angle1 <= 0 && angle2 >= 0))
            {
                angle = Math.Abs(angle1) + Math.Abs(angle2);
                if (angle > 180) angle = 360 - angle;
            }
            else
            {
                angle = Math.Abs(angle1 - angle2);
            }

            return angle;
        }

        // Calculates distance from v1 to v2 with adding cot(pi - angle) if angle > pi/2
        // Always calculate for ver[verIndex] to ver[verIndex + 1]
        private double calcDistVertex(GPSCoordinates[] ver, int verIndex, double heading, out double addedPrev, out double addedNext)
        //private double calcDistVertex(GPSCoordinates[] ver, int verIndex)
        {
            double dist = 0;
            int i = 0;
            double verAngle1, verAngle2;
            double angle, angleSumRad = 0;
            GPSCoordinates v1 = new GPSCoordinates();
            GPSCoordinates v2 = new GPSCoordinates();
            GPSCoordinates wp1 = new GPSCoordinates();
            GPSCoordinates wp2 = new GPSCoordinates();
            GPSCoordinates inters = new GPSCoordinates();
            double slopeWP, consWP, slopeV, consV;

            int verIndex1;

            if (verIndex != ver.Length - 1) verIndex1 = verIndex + 1;
            else verIndex1 = 0;

            dist = calcDistance(ver[verIndex], ver[verIndex1]);

            verAngle1 = calcVertexAngle(ver, verIndex);
            verAngle2 = calcVertexAngle(ver, verIndex1);

            /////////////////////////////////////////////////////////////////////////////////////
            // Ovo ne radi dobro ako se u delu produzivanja nadje jos neka tacka poligona.
            // Ovde se podrazumeva da je susedna tacka udaljena toliko da po Lx dimenziji slike
            // nece obuhvatiti i susednu tacku poligona.
            /////////////////////////////////////////////////////////////////////////////////////

            //if (verAngle1 > 90) dist = dist + Lx / Math.Tan(Math.PI - verAngle1 * Math.PI / 180);
            //if (verAngle2 > 90) dist = dist + Lx / Math.Tan(Math.PI - verAngle2 * Math.PI / 180);
                       
           
            
            double xDirect = heading + 90;
            if (xDirect < -180) xDirect = 360 - xDirect;
            else if (xDirect > 180) xDirect = -360 + xDirect;

            // Ovi wp1 i wp2 su ustvari vertexi ali je stajalo wp..
            wp1 = ver[verIndex];
            wp1 = moveGPS(wp1, Lx, xDirect);

            wp2 = ver[verIndex1];
            wp2 = moveGPS(wp2, Lx, xDirect);
           
            // Nadji parametre prave granice slika (kraj slika)
            findLineParams(wp1, wp2, out slopeWP, out consWP);

            addedPrev = 0;
            if (verAngle1 > 90)
            {
                // Trazi presek sa PRETHODNIM stranicama
                i = 0;
                angleSumRad = 0;

                while (i < ver.Length)
                {
                    angle = getVertexPrevAngle(ver, verIndex, i);
                    angleSumRad = angleSumRad + (Math.PI - angle * 3.14 / 180);

                    getTwoVertexPrev(ver, verIndex, i, out v1, out v2);

                    // Nadji parametre prave prethodne stranice poligona
                    findLineParams(v1, v2, out slopeV, out consV);

                    // Nadji presek  dve prave
                    inters = findInersection(slopeWP, consWP, slopeV, consV);

                    // Ako je ukupni ugao veci od 90 ne dodaji vise nego prekini
                    if (angleSumRad <= Math.PI / 2)
                    {
                        // Da li taj presek ne pripada stranici poligona => idi na prethodnu stranicu
                        if ((inters.lat > v1.lat && inters.lat > v2.lat) ||
                        (inters.lat < v1.lat && inters.lat < v2.lat))
                        {
                            // Znaci da ne pripada stranici pa dodajemo projekciju te stranice u duzinu
                            addedPrev += calcDistance(v1, v2) * Math.Cos(angleSumRad);
                            i++;
                        }
                        else
                        { 
                            addedPrev += calcDistance(v1, inters) * Math.Cos(angleSumRad);
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            dist = dist + addedPrev;


            addedNext = 0;
            if (verAngle2 > 90)
            {
                // Trazi presek sa NAREDNIM stranicama
                i = 0;
                angleSumRad = 0;
                while (i < ver.Length)
                {
                    angle = getVertexNextAngle(ver, verIndex, i);
                    angleSumRad = angleSumRad + (Math.PI - angle * 3.14 / 180);

                    getTwoVertexNext(ver, verIndex, i, out v1, out v2);

                    // Nadji parametre prave prethodne stranice poligona
                    findLineParams(v1, v2, out slopeV, out consV);

                    // Nadji presek dve prave
                    inters = findInersection(slopeWP, consWP, slopeV, consV);

                    // Ako je ukupni ugao veci od 90 ne dodaji vise nego prekini
                    if (angleSumRad <= Math.PI / 2)
                    {
                        // Da li taj presek ne pripada stranici poligona => idi na prethodnu stranicu
                        if ((inters.lat > v1.lat && inters.lat > v2.lat) ||
                        (inters.lat < v1.lat && inters.lat < v2.lat))
                        {
                            // Znaci da ne pripada stranici
                            addedNext += calcDistance(v1, v2) * Math.Cos(angleSumRad);
                            i++;
                        }
                        else
                        {
                            addedNext += calcDistance(v1, inters) * Math.Cos(angleSumRad);
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            dist = dist + addedNext;
            



            return dist;
        }

        private GPSCoordinates setFirstWPinStripe(GPSCoordinates[] ver, int verIndex, double heading, double shiftX, double shiftY, double addedDistPrev)
        {
            GPSCoordinates wp = new GPSCoordinates();
            double reverseHeading;

            if (calcVertexAngle(ver, verIndex) <= 90)
            {
                wp = calcFirstWP_GPS(ver[verIndex], shiftX, shiftY, heading);
            }
            else
            {
                // Uzmi u obzir i dodatni deo
                if (heading < 0) reverseHeading = 180 + heading;
                else reverseHeading = -180 + heading;
                wp = moveGPS(ver[verIndex], addedDistPrev, reverseHeading);
                wp = calcFirstWP_GPS(wp, shiftX, shiftY, heading);
            }

            return wp;
        }

        private int planFirstStripe(GPSCoordinates[] ver, out double vHeading)
        {
            int i = 0;
            int vIndex = 0;
            double len, maxLen = 0;
            double distAddedPrev = 0;
            double distAddedNext = 0;
            double distStripe = 0;

            // Nadji najduzu stranicu; maxIndex ce biti prva tacka, a druga je maxIndex + 1.
            for (i = 0; i < ver.Length; i++)
            {
                if (i != ver.Length - 1) len = calcDistance(ver[i], ver[i + 1]);
                else len = calcDistance(ver[i], ver[0]);

                if (len > maxLen)
                {
                    maxLen = len;
                    vIndex = i;
                }
            }

            // Izracunaj Heading po toj najduzoj stranici
            if (vIndex != ver.Length - 1) vHeading = calcHeading(ver[vIndex], ver[vIndex + 1]);
            else vHeading = calcHeading(ver[vIndex], ver[0]);

            // Izracunaj duzinu koja treba da se pokrije u ovom stripe-u
            distStripe = calcDistVertex(ver, vIndex, vHeading, out distAddedPrev, out distAddedNext);

            ////////////////////////////////////////////////////////////////////
            // OVDE JE DEFAULT DA ZA PRVU TACKU UZME ONU SA MANJIM INDEXOM!!!
            ////////////////////////////////////////////////////////////////////
            // Postavi prvu tacku ovog stripe-a
            Array.Resize(ref wpArray, wpArray.Length + 1);
            wpArray[wpArray.Length - 1] = setFirstWPinStripe(ver, vIndex, vHeading, Lx / 2, Ly / 2, distAddedPrev); // (ver[vIndex], Lx / 2, Ly / 2, vHeading);

            //int m = (int)(distStripe / dy) + 1;
            //deltaY = (distStripe - Ly) / (m - 1);

            int m = (int)((distStripe - (Ly - dy)) / dy) + 1;
            deltaY = (distStripe - Ly) / (m - 1);
                        
            // Postavi ostale tacke ovog stripe-a
            for (int j = 1; j < m; j++)
            {
                Array.Resize(ref wpArray, wpArray.Length + 1);
                wpArray[wpArray.Length - 1] = moveGPS(wpArray[wpArray.Length - 2], deltaY, vHeading);
            }

            // Upisi broj wp-ova u ovom stripe-u u wpNumPerStripe[]
            Array.Resize(ref wpNumPerStripe, wpNumPerStripe.Length + 1);
            wpNumPerStripe[wpNumPerStripe.Length - 1] = m;

            return vIndex;
        }

        private int planStripe(GPSCoordinates[] newVert, double vHeading)
        {
            double distStripe = 0;
            int vIndex = 0;
            double headTemp;
            double distAddedPrev = 0;
            double distAddedNext = 0;

            // Prema heading-u nadji po kojoj stranici radis
            for (int i = 0; i < newVert.Length; i++)
            {
                if (i != newVert.Length - 1) headTemp = calcHeading(newVert[i], newVert[i + 1]);
                else headTemp = calcHeading(newVert[i], newVert[0]);

                if (Math.Abs(headTemp - vHeading) < 1)
                {
                    vIndex = i;
                    break;
                }
            }
                       

            // Izracunaj duzinu koja treba da se pokrije u ovom stripe-u
            distStripe = calcDistVertex(newVert, vIndex, vHeading, out distAddedPrev, out distAddedNext);

            ////////////////////////////////////////////////////////////////////
            // OVDE JE DEFAULT DA ZA PRVU TACKU UZME ONU SA MANJIM INDEXOM!!!
            ////////////////////////////////////////////////////////////////////
            // Postavi prvu tacku ovog stripe-a
            Array.Resize(ref wpArray, wpArray.Length + 1);
            wpArray[wpArray.Length - 1] = setFirstWPinStripe(newVert, vIndex, vHeading, deltaX / 2, Ly / 2, distAddedPrev); // (ver[vIndex], Lx / 2, Ly / 2, vHeading);

            //int m = (int)(distStripe / dy) + 1;
            //deltaY = (distStripe - Ly) / (m - 1);

            int m = (int)((distStripe - (Ly - dy)) / dy) + 1;
            deltaY = (distStripe - Ly) / (m - 1);

            // Postavi ostale tacke ovog stripe-a
            for (int j = 1; j < m; j++)
            {
                Array.Resize(ref wpArray, wpArray.Length + 1);
                wpArray[wpArray.Length - 1] = moveGPS(wpArray[wpArray.Length - 2], deltaY, vHeading);
            }

            // Upisi broj wp-ova u ovom stripe-u u wpNumPerStripe[]
            Array.Resize(ref wpNumPerStripe, wpNumPerStripe.Length + 1);
            wpNumPerStripe[wpNumPerStripe.Length - 1] = m;

            return vIndex;
        }

        private void findLineParams(GPSCoordinates p1, GPSCoordinates p2, out double slope, out double cons)
        {
            // y = ax + b            
            // p.lat = slope*p.lon + cons

            // Ako su p2.lon == p1.lon onda malo mrdnem p2.lon da ne bi bilo deljenje sa 0, a to nece mnogo uticati
            if (p2.lon == p1.lon) p2.lon += 0.0000001; 

            slope = (p2.lat - p1.lat) / (p2.lon - p1.lon);
            cons = p1.lat - p1.lon * (p2.lat - p1.lat) / (p2.lon - p1.lon);
            
        }

        private double findVertexDistFromLine(GPSCoordinates v, double slope, double cons)
        {
            // y = ax + b            
            // p.lat = slope*p.lon + cons

            double dist = 0;

            double slopePerp = Math.Tan(Math.Atan(slope) + Math.PI / 2);
            double consPerp = v.lat - slopePerp * v.lon;

            GPSCoordinates inters = new GPSCoordinates();
            inters = findInersection(slope, cons, slopePerp, consPerp);

            dist = calcDistance(v, inters);

            return dist;
        }

        private GPSCoordinates findInersection(double slope1, double cons1, double slope2, double cons2)
        {
            GPSCoordinates res = new GPSCoordinates();

            res.lon = (cons2 - cons1) / (slope1 - slope2);
            res.lat = slope1 * (cons2 - cons1) / (slope1 - slope2) + cons1;

            return res;
        }

        // Trazi prethodnu tacku poligona (jer je niz pa ako je prethodna ustvari poslednja u nizu)
        private GPSCoordinates getVertexPrev(GPSCoordinates[] ver, int vIndex)
        {
            GPSCoordinates v = new GPSCoordinates();
            if (vIndex != 0) v = ver[vIndex - 1];
            else v = ver[ver.Length - 1];
            return v;
        }

        // Trazi narednu tacku poligona (jer je niz pa ako je naredna ustvari prva u nizu)
        private GPSCoordinates getVertexNext(GPSCoordinates[] ver, int vIndex)
        {
            GPSCoordinates v = new GPSCoordinates();
            if (vIndex != (ver.Length - 1)) v = ver[vIndex + 1];
            else v = ver[0];
            return v;
        }

        // Trazi prethodne dve tacke poligona pomerene za i (jer je niz pa ako je prethodna ustvari poslednja u nizu)
        // Kada je prevCnt = 0, vraca vIndex i vIndex - 1
        private void getTwoVertexPrev(GPSCoordinates[] ver, int vIndex, int prevCnt, out GPSCoordinates v1, out GPSCoordinates v2)
        {
            if ((vIndex - prevCnt) < 0) v1 = ver[ver.Length + vIndex - prevCnt];
            else v1 = ver[vIndex - prevCnt];

            if ((vIndex - prevCnt - 1) < 0) v2 = ver[ver.Length + vIndex - prevCnt - 1];
            else v2 = ver[vIndex - prevCnt - 1];
        }

        private double getVertexPrevAngle(GPSCoordinates[] ver, int vIndex, int prevCnt)
        {
            double angle = 0;

            if ((vIndex - prevCnt) < 0) angle = calcVertexAngle(ver, ver.Length + vIndex - prevCnt);
            else angle = calcVertexAngle(ver, vIndex - prevCnt);

            return angle;
        }

        // Trazi naredne dve tacke poligona pomerene za i (jer je niz pa ako je naredna ustvari prva u nizu)
        // Kada je prevCnt = 0, vraca vIndex + 1 i vIndex + 2
        private void getTwoVertexNext(GPSCoordinates[] ver, int vIndex, int nextCnt, out GPSCoordinates v1, out GPSCoordinates v2)
        {
            if ((vIndex + nextCnt + 1) > ver.Length - 1) v1 = ver[vIndex + nextCnt + 1 - ver.Length];
            else v1 = ver[vIndex + nextCnt + 1];

            if ((vIndex + nextCnt + 2) > ver.Length - 1) v2 = ver[vIndex + nextCnt + 2 - ver.Length];
            else v2 = ver[vIndex + nextCnt + 2];
        }

        private double getVertexNextAngle(GPSCoordinates[] ver, int vIndex, int nextCnt)
        {
            double angle = 0;

            if ((vIndex + nextCnt + 1) > ver.Length - 1) angle = calcVertexAngle(ver, vIndex + nextCnt + 1 - ver.Length);
            else angle = calcVertexAngle(ver, vIndex + nextCnt + 1);

            return angle;
        }

        private GPSCoordinates[] removeElements(GPSCoordinates[] inArray, int[] index)
        {
            GPSCoordinates[] newArray = new GPSCoordinates[0];
            int j;

            for (j = 0; j < inArray.Length; j++)
            {
                if (index.Contains(j) == false)
                {
                    Array.Resize(ref newArray, newArray.Length + 1);
                    newArray[newArray.Length - 1] = inArray[j];
                }
            }

            return newArray;
        }

        private GPSCoordinates[] findNewPolygon(GPSCoordinates[] ver, int vIndex, GPSCoordinates[] wp, double heading)
        {
            int i = 0;
            double slopeWP, slopeV;
            double consWP, consV;
            GPSCoordinates wp1 = new GPSCoordinates();
            GPSCoordinates wp2 = new GPSCoordinates();
            GPSCoordinates v1 = new GPSCoordinates();
            GPSCoordinates v2 = new GPSCoordinates();
            GPSCoordinates inters = new GPSCoordinates();

            int[] removeIndex = new int[0];

            GPSCoordinates[] returnPolygon = new GPSCoordinates[0];
            GPSCoordinates[] newPolygon = new GPSCoordinates[0];
            Array.Resize(ref newPolygon, ver.Length);
            newPolygon = ver;

            double xDirect = heading + 90;
            if (xDirect < -180) xDirect = 360 - xDirect;
            else if (xDirect > 180) xDirect = -360 + xDirect;

            // Uzmi poslednja dva WPa ako ima toliko, i pomeri ih na putanju narednog prolaza (stipe). 
            // Ako nema dva WPa, ne radi ovaj algoritam.
            if (wp.Length > 2)
            {
                // OVO RADI ZA POLIGONE U SMERU KAZALJKE NA SATU
                wp2 = wp[wp.Length - 1];
                wp2 = moveGPS(wp2, deltaX / 2, xDirect);

                wp1 = wp[wp.Length - 2];
                wp1 = moveGPS(wp1, deltaX / 2, xDirect);
            }
            else
            {
                // GRESKA
            }

            // Nadji parametre prave granice slika u narednom stripe-u
            findLineParams(wp1, wp2, out slopeWP, out consWP);

            // Trazi presek sa PRETHODNIM stranicama
            i = 0;
            while (i < ver.Length)
            {
                getTwoVertexPrev(ver, vIndex, i, out v1, out v2);

                // Nadji parametre prave prethodne stranice poligona
                findLineParams(v1, v2, out slopeV, out consV);

                // Nadji presek  dve prave
                inters = findInersection(slopeWP, consWP, slopeV, consV);

                // Da li taj presek ne pripada stranici poligona => idi na prethodnu stranicu
                if ((inters.lat > v1.lat && inters.lat > v2.lat) ||
                    (inters.lat < v1.lat && inters.lat < v2.lat))
                {
                    // Znaci da ne pripada stranici
                    Array.Resize(ref removeIndex, removeIndex.Length + 1);
                    if (vIndex - i - 1 < 0) removeIndex[removeIndex.Length - 1] = ver.Length + vIndex - i;
                    else removeIndex[removeIndex.Length - 1] = vIndex - i - 1;

                    i++;
                }
                else
                {
                    break;
                }
            }

            // vIndex tacka se prepravlja u novu tacku a ostale ce da se sklone
            newPolygon[vIndex] = inters;



            // Trazi presek sa NAREDNIM stranicama
            i = 0;
            while (i < ver.Length)
            {
                getTwoVertexNext(ver, vIndex, i, out v1, out v2);

                // Nadji parametre prave prethodne stranice poligona
                findLineParams(v1, v2, out slopeV, out consV);

                // Nadji presek dve prave
                inters = findInersection(slopeWP, consWP, slopeV, consV);

                // Da li taj presek ne pripada stranici poligona => idi na prethodnu stranicu
                if ((inters.lat > v1.lat && inters.lat > v2.lat) ||
                    (inters.lat < v1.lat && inters.lat < v2.lat))
                {
                    // Znaci da ne pripada stranici
                    Array.Resize(ref removeIndex, removeIndex.Length + 1);
                    if (vIndex + i + 2 > ver.Length - 1) removeIndex[removeIndex.Length - 1] = vIndex + i + 2 - ver.Length;
                    else removeIndex[removeIndex.Length - 1] = vIndex + i + 2;

                    i++;
                }
                else
                {
                    break;
                }
            }

            // vIndex tacka se prepravlja u novu tacku a ostale ce da se sklone
            if(vIndex != newPolygon.Length - 1) newPolygon[vIndex + 1] = inters;
            else newPolygon[0] = inters;


            returnPolygon = removeElements(newPolygon, removeIndex);

            return returnPolygon;

        }

        private double findFarthestVertexDistance(GPSCoordinates[] ver, int vIndex)
        {
            double slopeV, consV, dist = 0;
            double maxDist = 0;
            int i = 0;
            int vIndex1;

            if (vIndex != (ver.Length - 1)) vIndex1 = vIndex + 1;
            else vIndex1 = 0;

            // Nadji parametre prave trenutne stranice poligona
            findLineParams(ver[vIndex], ver[vIndex1], out slopeV, out consV);

            for (i = 0; i < ver.Length; i++)
            {
                if (i != vIndex && i != vIndex1) dist = findVertexDistFromLine(ver[i], slopeV, consV);

                if (dist > maxDist) maxDist = dist;
            }

            return maxDist;
        }

    }
}
