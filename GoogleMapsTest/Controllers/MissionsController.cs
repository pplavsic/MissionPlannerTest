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
        public ActionResult CreateMissionFromPoly(double[] Lat, double[] Lng, long MissionFK)
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

                //TODO: Implement FlightPoint creation!!

                return Json(new { Msg = "Success" }, JsonRequestBehavior.AllowGet);
            
            }
            catch(Exception g)
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
