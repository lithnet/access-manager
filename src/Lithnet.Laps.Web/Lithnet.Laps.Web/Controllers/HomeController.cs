using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return this.RedirectToAction("Get", "Lap");
        }

        public ActionResult AuthNError()
        {
            return this.View();
        }
    }
}