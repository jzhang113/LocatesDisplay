using System;
using Microsoft.AspNetCore.Mvc;
using LocateDisplay.Models;

namespace LocateDisplay.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var l = LocateModel.GetTickets(DateTime.Today - new TimeSpan(1, 0, 0, 0), DateTime.Today + new TimeSpan(1, 0, 0, 0));
            return View(l);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }
    }
}
