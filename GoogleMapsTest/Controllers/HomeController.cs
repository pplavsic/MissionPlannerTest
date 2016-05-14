using GoogleMapsTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GoogleMapsTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            LocaDatabase db = new LocaDatabase();
            db.SaveChanges();
            return View();
        }

    }
}