using System;
using System.Collections.Generic;
using System.Linq;

namespace LocateDisplay.Models
{
    public class DatabaseModel
    {
        public static TicketViewModel GetTicket(string connectionString, DateTime beginQueryDate, string sortColumn, bool sortDescending)
        {
            TicketViewModel viewModel = new TicketViewModel();
            List<OneCallTicket> list = new List<OneCallTicket>();

            using (var db = new SADContext(connectionString))
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
            if (sortDescending)
                viewModel.TicketList = list.OrderByDescending(keySelector);
            else
                viewModel.TicketList = list.OrderBy(keySelector);

            viewModel.SortColumn = sortColumn;
            viewModel.SortDescending = sortDescending;
            return viewModel;
        }

        public static TicketViewModel GetTicketById(string connectionString, string id)
        {
            TicketViewModel viewModel = new TicketViewModel();
            List<OneCallTicket> list = new List<OneCallTicket>();

            using (var db = new SADContext(connectionString))
            {
                var entries = from entry in db.OneCallTicket
                              where entry.TicketNumber.Contains(id)
                              select entry;

                foreach (OneCallTicket ticket in entries)
                {
                    list.Add(ticket);
                }
            }

            viewModel.TicketList = list;
            viewModel.SortColumn = "TicketNumber";
            viewModel.SortDescending = true;
            return viewModel;
        }

        private static Func<OneCallTicket, dynamic> GetSelector(string column)
        {
            switch (column)
            {
                case "City":
                    return x => x.City;
                case "BeginWorkDate":
                    return x => x.BeginWorkDate;
                case "ExcavatorName":
                    return x => x.ExcavatorName;
                case "OnsiteContactPerson":
                    return x => x.OnsightContactPerson;
                case "OnsiteContactPhone":
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
