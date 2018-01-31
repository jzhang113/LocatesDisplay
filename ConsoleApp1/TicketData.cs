using System;

namespace ConsoleApp1
{
    class TicketData
    {
        public long ID { get; set; }
        public DateTime OrigCall { get; set; }
        public DateTime BeginTime { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string District { get; set; }
        public string Status { get; set; }
    }
}
