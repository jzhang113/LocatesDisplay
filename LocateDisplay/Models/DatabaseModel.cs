using System;
using System.Collections.Generic;
using System.Linq;

namespace LocateDisplay.Models
{
    public class DatabaseModel
    {
        public static TicketViewModel GetTicket(DateTime beginQueryDate)
        {
            TicketViewModel viewModel = new TicketViewModel();
            List<OneCallTicket> list = new List<OneCallTicket>();

            using (var db = new SADContext())
            {
                var entries = from entry in db.OneCallTicket
                              where entry.OriginalCallDate > beginQueryDate
                              select entry;

                foreach (OneCallTicket ticket in entries)
                {
                    list.Add(ticket);
                }
            }

            viewModel.TicketList = list;
            return viewModel;
        }
    }
}
