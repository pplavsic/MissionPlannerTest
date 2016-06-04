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
    public class FlightPointsController : Controller
    {
        private LocaDatabase db = new LocaDatabase();

        // GET: FlightPoints
        public ActionResult Index()
        {
            var flightPoints = db.FlightPoints.Include(f => f.Mission);
            return View(flightPoints.ToList());
        }

        // GET: FlightPoints/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FlightPoint flightPoint = db.FlightPoints.Find(id);
            if (flightPoint == null)
            {
                return HttpNotFound();
            }
            return View(flightPoint);
        }

        // GET: FlightPoints/Create
        public ActionResult Create()
        {
            ViewBag.MissionFK = new SelectList(db.Missions, "MissionPK", "Name");
            var Model = new FlightPoint()
            {
                Altitude = 30,
                Target_yaw = 0,
                Damping_distance = 0,
                Target_gimbal_pitch = 0,
                Turn_mode = TurnMode.Clockwise,
                Has_action = HasAction.No_Action,
                Action_time_limit = 5,
                ACTION_action_repeat = 1,
                _ACTION_COMMAND_LIST = "1",
                _ACTION_COMMAND_PARAMS = "1"
                 
            };
            return View(Model);
        }

        // POST: FlightPoints/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FlightPointPK,MissionFK,Latitude,Longitude,Altitude,Damping_distance,Target_yaw,Target_gimbal_pitch,Turn_mode,Has_action,Action_time_limit,ACTION_action_repeat,_ACTION_COMMAND_LIST,_ACTION_COMMAND_PARAMS,PIAction")] FlightPoint flightPoint)
        {
            if (ModelState.IsValid)
            {
                db.FlightPoints.Add(flightPoint);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MissionFK = new SelectList(db.Missions, "MissionPK", "Name", flightPoint.MissionFK);
            return View(flightPoint);
        }

        // GET: FlightPoints/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FlightPoint flightPoint = db.FlightPoints.Find(id);
            if (flightPoint == null)
            {
                return HttpNotFound();
            }
            ViewBag.MissionFK = new SelectList(db.Missions, "MissionPK", "Name", flightPoint.MissionFK);
            return View(flightPoint);
        }

        // POST: FlightPoints/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FlightPointPK,MissionFK,Latitude,Longitude,Altitude,Damping_distance,Target_yaw,Target_gimbal_pitch,Turn_mode,Has_action,Action_time_limit,ACTION_action_repeat,_ACTION_COMMAND_LIST,_ACTION_COMMAND_PARAMS,PIAction")] FlightPoint flightPoint)
        {
            if (ModelState.IsValid)
            {
                db.Entry(flightPoint).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MissionFK = new SelectList(db.Missions, "MissionPK", "Name", flightPoint.MissionFK);
            return View(flightPoint);
        }

        // GET: FlightPoints/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FlightPoint flightPoint = db.FlightPoints.Find(id);
            if (flightPoint == null)
            {
                return HttpNotFound();
            }
            return View(flightPoint);
        }

        // POST: FlightPoints/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            FlightPoint flightPoint = db.FlightPoints.Find(id);
            db.FlightPoints.Remove(flightPoint);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult GetLastMissionPoint(long MissionPK)
        {
            var point = db.FlightPoints.Where(f => f.MissionFK == MissionPK).OrderByDescending(o => o.FlightPointPK).FirstOrDefault();
            if (point != null)
            { return Json(new { Latitude = point.Latitude, Longitude = point.Longitude, Altitude = point.Altitude, Action = (int)point.PIAction }, JsonRequestBehavior.AllowGet); }
            else { return null; }

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
