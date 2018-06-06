using System;
using System.Collections.Generic;

namespace LocateDisplay.Models
{
    public class TicketViewModel
    {
        public IEnumerable<OneCallTicket> TicketList { get; set; }
        public string SortColumn { get; set; }
        public bool SortDescending { get; set; }
        public bool UpdateSuccess { get; set; }
        public TimeSpan LastUpdate { get; set; }
    }
}
