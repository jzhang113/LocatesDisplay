using HtmlAgilityPack;
using LocatesParser.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LocatesParser
{
    class Program
    {
        private static HttpClient client;
        private static HttpClientHandler handler;
        private static CookieContainer cookies;

        private static string siteUrl = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";
        private static readonly int DAYS_PREV = 2;

        static void Main(string[] args)
        {
            cookies = new CookieContainer();
            handler = new HttpClientHandler { CookieContainer = cookies };
            client = new HttpClient(handler);

            Task.Run(async () =>
            {
                // Log in to the site
                HtmlDocument document = new HtmlDocument();
                HttpContent login = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("iSiteUserName", "ia-uoim"),
                    new KeyValuePair<string, string>("iSitePassword", "mechshop")
                });
                document.Load(await MakePostRequest(siteUrl, login));

                // Have to load this first so that the next GET request succeeds (design pls)
                HtmlNode linkNode = document.GetElementbyId("linkTC");
                string link = linkNode.Attributes["href"].Value;
                link = link.Replace("../..", "http://www.managetickets.com");
                document.Load(await MakeGetRequest(link));

                // Load the current active tickets
                DateTime endDate = DateTime.Today.Add(new TimeSpan(1, 0, 0, 0));
                DateTime startDate = endDate.Subtract(new TimeSpan(DAYS_PREV, 0, 0, 0));
                HttpContent search = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("auditEndDate", endDate.ToShortDateString()),
                    new KeyValuePair<string, string>("auditStartDate", startDate.ToShortDateString()),
                    new KeyValuePair<string, string>("CurrentDisplay", "All"),
                    new KeyValuePair<string, string>("District", "IA-9559")
                });
                document.Load(await MakePostRequest("http://www.managetickets.com/morecApp/servlet/ViewTickets", search));

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
                        newPage.Load(await MakeGetRequest(detailLink));
                        HtmlNode content = newPage.GetElementbyId("content");
                        HtmlNodeCollection keyNode = content.SelectNodes("//form/input[@name='key']");
                        string key = keyNode[0].GetAttributeValue("value", "");

                        HtmlNodeCollection sections = content.SelectNodes("//div[@class='pure-g']");

                        Match ticketType = Regex.Match(sections[0].InnerHtml, @"<span class=.+>&nbsp;<\/span>\s*<span class=.+>(.*)<\/span>");

                        Match excavatorName = Regex.Match(sections[2].InnerHtml, @"<span class=.+>Excavator Name:<\/span>\s*<span class=.+>(.*)<\/span>");
                        Match onsiteInfo = Regex.Match(sections[2].InnerHtml, @"<span class=.+>Onsite Contact:<\/span>\s*<span class=.+>(.*)<\/span>[\S\s]*<span class=.+>Phone:<\/span>\s*<span class=.+>(.*)<\/span>");
                        string contactName = onsiteInfo.Groups[1].Value;
                        string contactPhone = onsiteInfo.Groups[2].Value.Replace("-", "");

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

                        System.Diagnostics.Debug.Assert(contactPhone.Length <= 10);

                        OneCallTicket oneCallEntry = new OneCallTicket
                        {
                            TicketNumber = coll[1].FirstChild.InnerHtml.Truncate(20),
                            TicketType = ticketType.Groups[1].Value.Truncate(20),
                            TicketKey = key,
                            Status = coll[9].InnerHtml.Truncate(20),
                            OriginalCallDate = DateTime.Parse(coll[2].InnerHtml),
                            BeginWorkDate = DateTime.Parse(coll[3].InnerHtml),
                            StreetAddress = coll[4].InnerHtml.Truncate(20),
                            City = coll[5].InnerHtml.Truncate(20),
                            ExcavatorName = excavatorName?.Groups[1].Value.Truncate(20),
                            OnsightContactPerson = contactName.Truncate(20),
                            OnsightContactPhone = contactPhone,
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

        private async static Task<Stream> MakePostRequest(string url, HttpContent content)
        {
            var result = await client.PostAsync(url, content);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadAsStreamAsync();
            /*
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
            */
        }

        private async static Task<Stream> MakeGetRequest(string url)
        {
            var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadAsStreamAsync();
        }
    }
}