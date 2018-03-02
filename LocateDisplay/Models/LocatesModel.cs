using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LocateDisplay.Models
{
    public class LocatesModel
    {
        private static CookieContainer cookies = new CookieContainer();
        private static string siteUrl = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";

        public static TicketViewModel GetTickets(DateTime startDate, DateTime endDate)
        {
            TicketViewModel viewModel = new TicketViewModel();
            List<OneCallTicket> list = new List<OneCallTicket>();

            Task.Run(async () =>
            {
                // Log in to the site
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(await MakePostRequest(siteUrl, "db=mo&sessionID=null&disttrans=n&basetrans=n&trans_id=0&district_code=0&record_id=0&trans_state=&iSiteUserName=ia-uoim&iSitePassword=mechshop"));

                // Have to load this first so that the next GET request succeeds
                HtmlNode linkNode = document.GetElementbyId("linkTC");
                string link = linkNode.Attributes["href"].Value;
                link = link.Replace("../..", "http://www.managetickets.com");
                document.LoadHtml(await MakeGetRequest(link));

                // Load the current active tickets
                document.LoadHtml(await MakePostRequest("http://www.managetickets.com/morecApp/servlet/ViewTickets", "auditEndDate=" + endDate.ToShortDateString() + "&auditStartDate=" + startDate.ToShortDateString() + "&CurrentDisplay=All&District=IA-9559"));

                // Get the collections of tickets
                HtmlNode ticketTable = document.GetElementbyId("TicketTable");
                HtmlNodeCollection tickets = ticketTable.SelectNodes("tbody/tr");

                // Quit processing if the search failed
                if (tickets == null)
                    return;

                // Get the elements from each ticket
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
                    string ticketLink = "http://ia.itic.occinc.com/" + key;

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
                        Console.WriteLine("Error: Did not return full html");
                    }

                    OneCallTicket oneCallEntry = new OneCallTicket
                    {
                        TicketNumber = coll[1].FirstChild.InnerHtml,
                        TicketType = ticketType.Groups[1].Value,
                        TicketKey = key,
                        Status = coll[9].InnerHtml,
                        OriginalCallDate = DateTime.Parse(coll[2].InnerHtml),
                        BeginWorkDate = beginTime,
                        FinishWorkDate = endTime,
                        StreetAddress = coll[4].InnerHtml,
                        City = coll[5].InnerHtml,
                        WorkExtent = extentWork?.Groups[1].Value,
                        Remark = remarks?.Groups[1].Value
                    };

                    list.Add(oneCallEntry);
                }
            }).GetAwaiter().GetResult();

            viewModel.TicketList = list;
            return viewModel;
        }

        private static DateTime GetEndDate(DateTime beginDate, string timeValue, string timeUnits)
        {
            int value = Int32.Parse(timeValue);
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
    }
}