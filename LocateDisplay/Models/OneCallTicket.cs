using System;
using System.ComponentModel;

namespace LocateDisplay.Models
{
    public partial class OneCallTicket
    {
        [DisplayName("Begin Work Date")]
        public DateTime BeginWorkDate { get; set; }

        public string City { get; set; }

        [DisplayName("Excavator Name")]
        public string ExcavatorName { get; set; }

        [DisplayName("Onsite Contact Person")]
        public string OnsightContactPerson { get; set; }

        [DisplayName("Onsite Contact Phone")]
        public string OnsightContactPhone { get; set; }

        [DisplayName("Original Call Date")]
        public DateTime OriginalCallDate { get; set; }

        [DisplayName("Remarks")]
        public string Remark { get; set; }

        public string Status { get; set; }

        [DisplayName("Street Address")]
        public string StreetAddress { get; set; }

        [DisplayName("Ticket Key")]
        public string TicketKey { get; set; }

        [DisplayName("Ticket Number")]
        public string TicketNumber { get; set; }

        [DisplayName("Ticket Type")]
        public string TicketType { get; set; }

        [DisplayName("Location")]
        public string WorkExtent { get; set; }
    }
}
