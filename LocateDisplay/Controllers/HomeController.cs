using System;
using Microsoft.AspNetCore.Mvc;
using LocateDisplay.Models;
using System.Web;

namespace LocateDisplay.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index(string days, string order, string asc)
        {
            if (!int.TryParse(days, out int prevDate))
                prevDate = 14;

            if (order == null)
                order = "TicketNumber";

            if (!bool.TryParse(asc, out bool sortAsc))
                sortAsc = true;

            var tickets = DatabaseModel.GetTicket(DateTime.Today.AddDays(-prevDate), order, sortAsc);
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
