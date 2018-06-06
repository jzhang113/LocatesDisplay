using System;
using Microsoft.AspNetCore.Mvc;
using LocateDisplay.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Diagnostics;

namespace LocateDisplay.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration;
        private bool _parseSuccess = false;
        private DateTime _lastUpdate;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _configuration = configuration;

            StreamReader sr = null;
            try
            {
                string path = Directory.GetCurrentDirectory() + "/last-update.txt";
                sr = new StreamReader(path);
                _parseSuccess = DateTime.TryParse(sr.ReadLine(), out _lastUpdate);
                sr.Close();
            }
            catch (IOException)
            {
                logger.LogWarning("Unable to find or read last-update.txt");
            }
            finally
            {
                if (sr != null)
                    sr.Dispose();
            }
        }

        [HttpGet]
        public IActionResult Index(string q, string days, string order, string desc)
        {
            string connectionString = _configuration["AppSettings:ConnectionString"];

            if (q != null)
            {
                var tickets = DatabaseModel.GetTicketById(connectionString, q);
                return View(tickets);
            }
            else
            {
                if (!int.TryParse(days, out int prevDate))
                    prevDate = 14;

                if (order == null)
                    order = "OriginalCallDate";

                if (!bool.TryParse(desc, out bool sortDesc))
                    sortDesc = true;

                TicketViewModel tickets = DatabaseModel.GetTicket(connectionString, DateTime.Today.AddDays(-prevDate), order, sortDesc);

                if (_parseSuccess)
                {
                    tickets.UpdateSuccess = true;
                    tickets.LastUpdate = DateTime.Now - _lastUpdate;
                }
                else
                {
                    tickets.UpdateSuccess = false;
                }

                return View(tickets);
            }
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
