using System;

namespace ConsoleApp1.Models
{
    public partial class OneCallTicket
    {
        public DateTime BeginWorkDate { get; set; }
        public string City { get; set; }
        public DateTime OriginalCallDate { get; set; }
        public string StreetAddress { get; set; }
        public string TicketNumber { get; set; }
        public string StateAbbreviation { get; set; }
        public string Status { get; set; }
    }
}
