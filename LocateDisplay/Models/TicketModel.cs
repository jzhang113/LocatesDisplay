using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocateDisplay.Models
{
    public class TicketModel
    {
        public DateTime BeginWorkDate { get; set; }
        public string City { get; set; }
        public DateTime FinishWorkDate { get; set; }
        public DateTime OriginalCallDate { get; set; }
        public string Remark { get; set; }
        public string Status { get; set; }
        public string StreetAddress { get; set; }
        public string TicketKey { get; set; }
        public string TicketNumber { get; set; }
        public string TicketType { get; set; }
        public string WorkExtent { get; set; }

        public string WorkExtentShort { get; set; }
        public string RemarkShort { get; set; }
    }
}
