using System;
using System.Collections.Generic;
using System.Linq;

namespace LocateDisplay.Models
{
    public class DatabaseModel
    {
        public static TicketViewModel GetTicket(DateTime beginQueryDate, string sortColumn, bool sortAscending)
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

            Func<OneCallTicket, dynamic> keySelector = GetSelector(sortColumn);
            if (sortAscending)
                viewModel.TicketList = list.OrderBy(keySelector);
            else
                viewModel.TicketList = list.OrderByDescending(keySelector);
            
            return viewModel;
        }

        private static Func<OneCallTicket, dynamic> GetSelector(string column)
        {
            switch (column)
            {
                case "City":
                    return x => x.City;
                case "ExcavatorName":
                    return x => x.ExcavatorName;
                case "OnsightContactPerson":
                    return x => x.OnsightContactPerson;
                case "OnsightContactPhone":
                    return x => x.OnsightContactPhone;
                case "OriginalCallDate":
                    return x => x.OriginalCallDate;
                case "Remarks":
                    return x => x.Remark;
                case "Status":
                    return x => x.Status;
                case "StreetAddress":
                    return x => x.StreetAddress;
                case "TicketKey":
                    return x => x.TicketKey;
                case "TicketNumber":
                    return x => x.TicketNumber;
                case "TicketType":
                    return x => x.TicketType;
                case "WorkExtent":
                    return x => x.WorkExtent;
                default:
                    return x => x.TicketNumber;
            }
        }
    }
}
