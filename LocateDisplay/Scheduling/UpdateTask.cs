using System;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using LocateDisplay.Models;
using Microsoft.Extensions.Logging;

namespace LocateDisplay.Scheduling
{
    public class UpdateTask : IScheduledTask
    {
        public TimeSpan Interval => new TimeSpan(0, 15, 0);

        private static HttpClient client;
        private static HttpClientHandler handler;
        private static CookieContainer cookies;

        private IConfiguration _configuration;
        private readonly ILogger<UpdateTask> _logger;

        private static string siteUrl = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";
        private static readonly DateTime BEGIN_DATE = DateTime.Today.Subtract(new TimeSpan(0, 0, 0, 0));
        private static readonly int DAYS_PREV = 7;

        public UpdateTask(IConfiguration configuration, ILogger<UpdateTask> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            string connString = _configuration["AppSettings:ConnectionString"];

            _logger.LogInformation($"Updating locates at {DateTime.Now}");

            cookies = new CookieContainer();
            handler = new HttpClientHandler { CookieContainer = cookies };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows 5.1;)");

            // Log in to the site
            HtmlDocument document = new HtmlDocument();
            HttpContent login = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("iSiteUserName", _configuration["AppSettings:Username"]),
                new KeyValuePair<string, string>("iSitePassword", _configuration["AppSettings:Password"])
            });
            document.Load(await MakePostRequest(siteUrl, login));

            _logger.LogDebug("Connecting to site");

            // Have to load this first so that the next GET request succeeds
            HtmlNode linkNode = document.GetElementbyId("linkTC");
            if (linkNode == null)
            {
                _logger.LogWarning("Log in to site failed - update is cancelled");
                return;
            }

            string link = linkNode.Attributes["href"].Value;
            link = link.Replace("../..", "http://www.managetickets.com");
            document.Load(await MakeGetRequest(link));

            _logger.LogDebug("Searching for tickets");

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

            _logger.LogInformation($"{tickets.Count} tickets found");
            int processed = 0;

            // Get the elements from each ticket
            var db = new SADContext(connString);
            try
            {
                foreach (HtmlNode tick in tickets)
                {
                    HtmlNodeCollection coll = tick.SelectNodes("td");

                    string detailLink = coll[1].FirstChild.GetAttributeValue("href", "");
                    detailLink = "http://www.managetickets.com/morecApp/" + detailLink;
                    string ticketNumber = coll[1].FirstChild.InnerHtml.Truncate(20);
                    string status = coll[9].InnerHtml.Truncate(50).ToTitleCase();

                    string info = $"processing ticket {++processed}: {ticketNumber} - ";

                    // Check if the ticket is already in the database
                    OneCallTicket dbTicket = db.Find<OneCallTicket>(ticketNumber);

                    // If not, add it to the database
                    if (dbTicket == null)
                    {
                        _logger.LogDebug(info + "new ticket");

                        OneCallTicket oneCallEntry = await ParsePage(coll, detailLink, ticketNumber, status);
                        db.Add(oneCallEntry);
                    }
                    else
                    {
                        // update the status if it is different
                        if (dbTicket.Status != status)
                        {
                            _logger.LogDebug(info + "updating status");
                            OneCallTicket oneCallEntry = await ParsePage(coll, detailLink, ticketNumber, status);

                            var entry = db.Entry(oneCallEntry);
                            entry.Property(e => e.Status).IsModified = true;
                        }

                        _logger.LogDebug(info + "skipping");
                    }
                }

                int updateCount = db.SaveChanges();
                _logger.LogDebug($"successfully updated {updateCount} records");

                StreamWriter sw = null;
                try
                {
                    string path = Directory.GetCurrentDirectory() + "/last-update.txt";
                    sw = new StreamWriter(path);
                    sw.WriteLine(DateTime.Now);
                    sw.Flush();
                    sw.Close();
                }
                catch (IOException)
                {
                    _logger.LogWarning("Unable to write to last-update.txt");
                }
                finally
                {
                    if (sw != null)
                        sw.Dispose();
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to update database - inner error: {e.Message}");
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
        }


        private async Task<OneCallTicket> ParsePage(HtmlNodeCollection coll, string detailLink, string ticketNumber, string status)
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
                _logger.LogWarning($"Ticket {ticketNumber} did not return full html");
            }

            if (contactPhone.Length > 10)
            {
                _logger.LogWarning($"Unexpected length of contact phone: {contactPhone}");
                contactPhone = contactPhone.Truncate(10);
            }

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

        private DateTime GetEndDate(DateTime beginDate, string timeValue, string timeUnits)
        {
            bool parsed = Int32.TryParse(timeValue, out int value);
            if (!parsed)
            {
                _logger.LogWarning($"Bad duration value: {timeValue}");
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
                    _logger.LogWarning($"Unknown duration type: {timeUnits}");
                    break;
            }

            return endDate;
        }

        private async Task<Stream> MakePostRequest(string url, HttpContent content)
        {
            var result = await client.PostAsync(url, content);

            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                _logger.LogWarning($"Failed to make a post request to {url}");
                throw;
            }

            return await result.Content.ReadAsStreamAsync();
        }

        private async Task<Stream> MakeGetRequest(string url)
        {
            var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                _logger.LogWarning($"Failed to make a get request to {url}");
                throw;
            }

            return await result.Content.ReadAsStreamAsync();
        }
    }
}