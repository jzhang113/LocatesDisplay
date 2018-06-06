using HtmlAgilityPack;
using LocatesParser.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LocatesParser
{
    class Program
    {
        private static HttpClient client;
        private static HttpClientHandler handler;
        private static CookieContainer cookies;

        private static string siteUrl = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";
        private static readonly DateTime BEGIN_DATE = DateTime.Today.Subtract(new TimeSpan(0, 0, 0, 0));
        private static readonly int DAYS_PREV = 7;

        static void Main(string[] args)
        {
#if DEBUG
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.Development.json");
#else
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json");
#endif
            string path = Directory.GetCurrentDirectory();
            Console.WriteLine($"Writing to: {path}");
            StreamWriter sw = new StreamWriter(path + "/log.txt");

            IConfigurationRoot config = builder.Build();
            string connString = config["connectionString"];
            Console.WriteLine(connString);
            sw.WriteLine(connString);

            cookies = new CookieContainer();
            handler = new HttpClientHandler { CookieContainer = cookies };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows 5.1;)");

            Task.Run(async () =>
            {
                // Log in to the site
                HtmlDocument document = new HtmlDocument();
                HttpContent login = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("iSiteUserName", config["username"]),
                    new KeyValuePair<string, string>("iSitePassword", config["password"])
                });
                document.Load(await MakePostRequest(siteUrl, login));

                // Have to load this first so that the next GET request succeeds (design pls)
                HtmlNode linkNode = document.GetElementbyId("linkTC");
                string link = linkNode.Attributes["href"].Value;
                link = link.Replace("../..", "http://www.managetickets.com");
                document.Load(await MakeGetRequest(link));

                // Load the current active tickets
                DateTime endDate = BEGIN_DATE.Add(new TimeSpan(1, 0, 0, 0));
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
                
                Console.WriteLine($"{tickets.Count} tickets found");
                sw.WriteLine($"{tickets.Count} tickets found");
                int processed = 0;

                // Get the elements from each ticket
                using (var db = new SADContext(connString))
                {
                    foreach (HtmlNode tick in tickets)
                    {
                        HtmlNodeCollection coll = tick.SelectNodes("td");

                        string detailLink = coll[1].FirstChild.GetAttributeValue("href", "");
                        detailLink = "http://www.managetickets.com/morecApp/" + detailLink;
                        string ticketNumber = coll[1].FirstChild.InnerHtml.Truncate(20);
                        string status = coll[9].InnerHtml.Truncate(50).ToTitleCase();

                        Console.WriteLine($"processing ticket {++processed}: {ticketNumber}");
                        sw.Write($"processing ticket {processed}: {ticketNumber} ");

                        // Check if the ticket is already in the database
                        OneCallTicket dbTicket = db.Find<OneCallTicket>(ticketNumber);

                        // If not, add it to the database
                        if (dbTicket == null)
                        {
                            sw.WriteLine("added");
                            OneCallTicket oneCallEntry = await ParsePage(coll, detailLink, ticketNumber, status);
                            db.Add(oneCallEntry);
                        }
                        else
                        {
                            // update the status if it is different
                            if (dbTicket.Status != status)
                            {
                                sw.WriteLine("updated");
                                OneCallTicket oneCallEntry = await ParsePage(coll, detailLink, ticketNumber, status);

                                var entry = db.Entry(oneCallEntry);
                                entry.Property(e => e.Status).IsModified = true;
                            }
                            else
                            {
                                sw.WriteLine("skipped");
                            }
                        }
                    }

                    int updateCount = db.SaveChanges();
                    Console.WriteLine($"successfully updated {updateCount} records");
                    sw.WriteLine($"successfully updated {updateCount} records");
                }

                sw.Flush();
                sw.Close();
            }).GetAwaiter().GetResult();
        }

        private static async Task<OneCallTicket> ParsePage(HtmlNodeCollection coll, string detailLink, string ticketNumber, string status)
        {
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
                Console.WriteLine($"Error: Ticket {ticketNumber} did not return full html");
            }

            System.Diagnostics.Debug.Assert(contactPhone.Length <= 10);

            OneCallTicket oneCallEntry = new OneCallTicket
            {
                TicketNumber = ticketNumber,
                TicketType = ticketType.Groups[1].Value.Truncate(20).ToTitleCase(),
                TicketKey = key,
                Status = status,
                OriginalCallDate = DateTime.Parse(coll[2].InnerHtml),
                BeginWorkDate = DateTime.Parse(coll[3].InnerHtml),
                StreetAddress = coll[4].InnerHtml.Truncate(50).ToTitleCase(),
                City = coll[5].InnerHtml.Truncate(20).ToTitleCase(),
                ExcavatorName = excavatorName?.Groups[1].Value.Truncate(20).ToTitleCase(),
                OnsightContactPerson = contactName.Truncate(20).ToTitleCase(),
                OnsightContactPhone = contactPhone,
                WorkExtent = extentWork?.Groups[1].Value.Truncate(1000).ToTitleCase(),
                Remark = remarks?.Groups[1].Value.Truncate(1000).ToTitleCase()
            };

            return oneCallEntry;
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
        }

        private async static Task<Stream> MakeGetRequest(string url)
        {
            var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadAsStreamAsync();
        }
    }
}