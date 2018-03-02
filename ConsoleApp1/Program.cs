using HtmlAgilityPack;
using LocatesParser.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace LocatesParser
{
    static class Program
    {
        private static CookieContainer cookies = new CookieContainer();
        private static string siteUrl = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";
        private static readonly int DAYS_PREV = 2;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                // Log in to the site
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(await MakePostRequest(siteUrl, "db=mo&sessionID=null&disttrans=n&basetrans=n&trans_id=0&district_code=0&record_id=0&trans_state=&iSiteUserName=ia-uoim&iSitePassword=mechshop"));

                // Have to load this first so that the next GET request succeeds (design pls)
                HtmlNode linkNode = document.GetElementbyId("linkTC");
                string link = linkNode.Attributes["href"].Value;
                link = link.Replace("../..", "http://www.managetickets.com");
                document.LoadHtml(await MakeGetRequest(link));

                // Load the current active tickets
                DateTime endDate = DateTime.Today.Add(new TimeSpan(1, 0, 0, 0));
                DateTime startDate = endDate.Subtract(new TimeSpan(DAYS_PREV, 0, 0, 0));
                document.LoadHtml(await MakePostRequest("http://www.managetickets.com/morecApp/servlet/ViewTickets", "auditEndDate=" + endDate.ToShortDateString() + "&auditStartDate=" + startDate.ToShortDateString() + "&CurrentDisplay=All&District=IA-9559"));

                // Get the collections of tickets
                HtmlNode ticketTable = document.GetElementbyId("TicketTable");
                HtmlNodeCollection tickets = ticketTable.SelectNodes("tbody/tr");

                // Quit processing if the search failed
                if (tickets == null)
                    return;

                // Get the elements from each ticket
                using (var db = new SADContext())
                {
                    foreach (HtmlNode tick in tickets)
                    {
                        HtmlNodeCollection coll = tick.SelectNodes("td");
                        string detailLink = coll[1].FirstChild.GetAttributeValue("href", "");
                        detailLink = "http://www.managetickets.com/morecApp/" + detailLink;

                        HtmlDocument newPage = new HtmlDocument();
                        newPage.LoadHtml(await MakeGetRequest(detailLink));
                        HtmlNode content = newPage.GetElementbyId("content");
                        HtmlNodeCollection keyNode = content.SelectNodes("//form/input[@name='key']");
                        string key = keyNode[0].GetAttributeValue("value", "");

                        HtmlNodeCollection sections = content.SelectNodes("//div[@class='pure-g']");

                        Match duration = Regex.Match(sections[0].InnerHtml, @"<span class=.+>Duration:<\/span>\s*<span class=.+>(\d+) (\w+)<\/span>");
                        Match ticketType = Regex.Match(sections[0].InnerHtml, @"<span class=.+>&nbsp;<\/span>\s*<span class=.+>(.*)<\/span>");

                        DateTime beginTime = DateTime.Parse(coll[3].InnerHtml);
                        DateTime endTime = GetEndDate(beginTime, duration.Groups[1].Value, duration.Groups[2].Value);

                        Match extentWork = null;
                        Match remarks = null;

                        // This section might not get found
                        if (sections.Count > 4)
                        {
                            extentWork = Regex.Match(sections[4].InnerHtml, @"<td class=.+>.*<\/td>\s*<td class=.+>([\S\s]*)<\/span><\/td>");
                            remarks = Regex.Match(sections[4].InnerHtml, @"<span class=.+>Remarks:<\/span>\s*<span class=.+>(.*)<\/span>");
                        }
                        else
                        {
                            Console.WriteLine("Error: Ticket " + coll[1].FirstChild.InnerHtml + " did not return full html");
                        }

                        OneCallTicket oneCallEntry = new OneCallTicket
                        {
                            TicketNumber = coll[1].FirstChild.InnerHtml.Truncate(20),
                            TicketType = ticketType.Groups[1].Value.Truncate(20),
                            TicketKey = key,
                            Status = coll[9].InnerHtml.Truncate(20),
                            OriginalCallDate = DateTime.Parse(coll[2].InnerHtml),
                            BeginWorkDate = beginTime,
                            FinishWorkDate = endTime,
                            StreetAddress = coll[4].InnerHtml.Truncate(20),
                            City = coll[5].InnerHtml.Truncate(20),
                            WorkExtent = extentWork?.Groups[1].Value.Truncate(1000),
                            Remark = remarks?.Groups[1].Value.Truncate(1000)
                        };

                        // Check if the ticket is already in the database
                        OneCallTicket dbTicket = db.Find<OneCallTicket>(oneCallEntry.TicketNumber, oneCallEntry.TicketKey);                       

                        // If not, add it to the database
                        if (dbTicket == null)
                        {
                            db.Add(oneCallEntry);
                        }
                        else
                        {
                            // Otherwise, update the entry if the status has changed
                            if (dbTicket.Status != oneCallEntry.Status)
                            {
                                var entry = db.Entry(oneCallEntry);
                                entry.Property(e => e.Status).IsModified = true;
                            }
                        }
                    }


                    int updateCount = db.SaveChanges();
                    Console.WriteLine("successfully updated " + updateCount + " records");
                }
            }).GetAwaiter().GetResult();
        }

        private static DateTime GetEndDate(DateTime beginDate, string timeValue, string timeUnits)
        {
            bool parsed = Int32.TryParse(timeValue, out int value);
            if (!parsed)
            {
                Console.WriteLine("Error: bad duration value");
                return beginDate;
            }

            timeUnits = timeUnits.ToUpper();
            DateTime endDate = beginDate;

            switch (timeUnits)
            {
                case "HOUR":
                case "HOURS":
                    endDate = endDate.AddHours(value);
                    break;
                case "DAY":
                case "DAYS":
                    endDate = endDate.AddDays(value);
                    break;
                case "WEEK":
                case "WEEKS":
                    endDate = endDate.AddDays(7 * value);
                    break;
                case "MONTH":
                case "MONTHS":
                    endDate = endDate.AddMonths(value);
                    break;
                default:
                    Console.WriteLine("Error: unknown duration type");
                    break;
            }

            return endDate;
        }

        private async static Task<string> MakePostRequest(string url, string data)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            
            // Save cookies
            request.CookieContainer = cookies;

            // Format request
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            byte[] byteArray = Encoding.UTF8.GetBytes(data);
            request.ContentLength = byteArray.Length;

            // Attach data
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // Return response
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private async static Task<string> MakeGetRequest(string url)
        {
            // Create request
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

            // Send cookies with request
            request.CookieContainer = cookies;

            // Return response
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (!String.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                return value.Substring(0, maxLength);
            }

            return value;
        }
    }
}