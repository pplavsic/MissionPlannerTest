using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GoogleMapsTest.Models;

namespace GoogleMapsTest.Controllers
{
    public class MissionsController : Controller
    {
        private LocaDatabase db = new LocaDatabase();
        private MissionPlanner mp = new MissionPlanner();

        // GET: Missions
        public ActionResult Index()
        {
            return View(db.Missions.ToList());
        }

        // GET: Missions/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Mission mission = db.Missions.Find(id);
            if (mission == null)
            {
                return HttpNotFound();
            }
            ViewBag.FlightPoints = db.FlightPoints.Where(f => f.MissionFK == mission.MissionPK).Select(s => new { Latitude = s.Latitude, Lognitude = s.Longitude, Height = s.Altitude, Action = s.PIAction.ToString() }).ToList();
            ViewBag.PolygonPoints = db.PolygonPoints.Where(f => f.MissionFK == mission.MissionPK).Select(s => new { Latitude = s.Latitude, Lognitude = s.Longitude }).ToList();
            return View(mission);
        }

        // GET: Missions/Create
        public ActionResult Create()
        {
            var Model = new Mission()
            {
                idle_velocity = 10,
                velocity_range = 15,
                action_on_finish = ActionOnFinish.Return_to_home,
                mission_exec_times = MissionExecNum.Once,
                yaw_mode = YawMode.Use_waypoints,
                trace_mode = Models.TraceMode.Point_to_point,
                action_on_rc_lost = ActionOnRCLost.Exit_waypoint_and_failsafe,
                gimbal_pitch_mode = GimbalPitchMode.Free
            };
            return View(Model);
        }

        // POST: Missions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MissionPK,Name,MissionStatus,velocity_range,idle_velocity,action_on_finish,mission_exec_times,yaw_mode,trace_mode,action_on_rc_lost,gimbal_pitch_mode")] Mission mission)
        {
            if (ModelState.IsValid)
            {
                db.Missions.Add(mission);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        // GET: Missions/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Mission mission = db.Missions.Find(id);
            if (mission == null)
            {
                return HttpNotFound();
            }
            return View(mission);
        }

        // POST: Missions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MissionPK,Name,MissionStatus,velocity_range,idle_velocity,action_on_finish,mission_exec_times,yaw_mode,trace_mode,action_on_rc_lost,gimbal_pitch_mode")] Mission mission)
        {
            if (ModelState.IsValid)
            {
                db.Entry(mission).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(mission);
        }

        // GET: Missions/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Mission mission = db.Missions.Find(id);
            if (mission == null)
            {
                return HttpNotFound();
            }
            return View(mission);
        }

        // POST: Missions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            Mission mission = db.Missions.Find(id);
            db.Missions.Remove(mission);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult PolygonMission()
        {
            ViewBag.MissionFK = new SelectList(db.Missions, "MissionPK", "Name");
            return View(db.Missions.ToList());
        }
        public ActionResult CreateMissionFromPoly(double[] Lat, double[] Lng, long MissionFK, float Alt)
        {
            try
            {
                if (db.PolygonPoints.Where(f => f.MissionFK == MissionFK).FirstOrDefault() != null)
                {
                    return Json(new { Msg = "Polygon points already exists." }, JsonRequestBehavior.AllowGet);
                }
                for (int i = 0; i < Lat.Length; i++)
                {
                    db.PolygonPoints.Add(new PolygonPoint() { Latitude = Lat[i], Longitude = Lng[i], MissionFK = MissionFK });
                }
                db.SaveChanges();
                
                FlightPoint[] fps;
                fps = mp.PlanMission(Lat, Lng, Alt, MissionFK);

                //fps = mp.reverseMission(fps);
                //fps = mp.mirrorMission(fps);

                db.FlightPoints.AddRange(fps);
                db.SaveChanges();                

                return Json(new { Msg = "Success", FlightPoints = fps }, JsonRequestBehavior.AllowGet);
            
            }
            catch(Exception g)
            {
                return Json(new { Msg = g.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult ReverseMission(long MissionFK)
        {
            try
            {
                if (db.PolygonPoints.Where(f => f.MissionFK == MissionFK).FirstOrDefault() == null)
                {
                    return Json(new { Msg = "Polygon points not exists." }, JsonRequestBehavior.AllowGet);
                }
                var old = db.FlightPoints.Where(f => f.MissionFK == MissionFK).ToArray();
                db.FlightPoints.RemoveRange(old);
                db.SaveChanges();
                FlightPoint[] fps = mp.reverseMission(old);
                foreach (var i in fps)
                {
                    i.FlightPointPK = 0;
                }
                db.FlightPoints.AddRange(fps);
                db.SaveChanges();
                return Json(new { Msg = "Success", FlightPoints = fps.Select(s => new FlightPoint { Latitude = s.Latitude, Longitude = s.Longitude, FlightPointPK = s.FlightPointPK, _ACTION_COMMAND_LIST = s._ACTION_COMMAND_LIST, _ACTION_COMMAND_PARAMS = s._ACTION_COMMAND_PARAMS }).ToArray() }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception g)
            {
                return Json(new { Msg = g.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult MirrorMission(long MissionFK)
        {
            try
            {
                if (db.PolygonPoints.Where(f => f.MissionFK == MissionFK).FirstOrDefault() == null)
                {
                    return Json(new { Msg = "Polygon points not exists." }, JsonRequestBehavior.AllowGet);
                }
                var old = db.FlightPoints.Where(f => f.MissionFK == MissionFK).ToArray();
                db.FlightPoints.RemoveRange(old);
                db.SaveChanges();
                FlightPoint[] fps = mp.mirrorMission(old);
                foreach (var i in fps)
                {
                    i.FlightPointPK = 0;
                }
                db.FlightPoints.AddRange(fps);
                db.SaveChanges();
                return Json(new { Msg = "Success", FlightPoints = fps.Select(s => new FlightPoint { Latitude = s.Latitude, Longitude = s.Longitude, FlightPointPK = s.FlightPointPK, _ACTION_COMMAND_LIST = s._ACTION_COMMAND_LIST, _ACTION_COMMAND_PARAMS = s._ACTION_COMMAND_PARAMS }).ToArray() }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception g)
            {
                return Json(new { Msg = g.Message }, JsonRequestBehavior.AllowGet);
            }

        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
