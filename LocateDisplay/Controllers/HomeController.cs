using System;
using Microsoft.AspNetCore.Mvc;
using LocateDisplay.Models;

namespace LocateDisplay.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var tickets = DatabaseModel.GetTicket(DateTime.Today.AddDays(-14));
            return View(tickets);
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
