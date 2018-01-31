using ConsoleApp1.Models;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(await LoginRequest());
                HtmlNode linkNode = document.GetElementbyId("linkTC");
                string link = linkNode.Attributes["href"].Value;
                link = link.Replace("../..", "http://www.managetickets.com");

                document.LoadHtml(await MakeGetRequest(link));
                HtmlNode ticketTable = document.GetElementbyId("TicketTable");
                HtmlNodeCollection tickets = ticketTable.SelectNodes("tbody/tr");

                using (var db = new SADContext())
                {
                    foreach (HtmlNode tick in tickets)
                    {
                        HtmlNodeCollection coll = tick.SelectNodes("td");
                        db.OneCallTicket.Add(new OneCallTicket
                        {
                            BeginWorkDate = DateTime.Parse(coll[3].InnerHtml),
                            City = coll[5].InnerHtml,
                            OriginalCallDate = DateTime.Parse(coll[2].InnerHtml),
                            StreetAddress = coll[4].InnerHtml,
                            TicketNumber = coll[1].FirstChild.InnerHtml,
                            StateAbbreviation = coll[7].InnerHtml,
                            Status = coll[9].InnerHtml
                        });
                    }

                    db.SaveChanges();
                }
            }).GetAwaiter().GetResult();
        }

        private async static Task<string> LoginRequest()
        {
            string url = "http://www.managetickets.com/mologin/servlet/iSiteLoginSelected";
            WebRequest request = WebRequest.Create(url);
            
            //Format Request
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            string requestData = "db=mo&sessionID=null&disttrans=n&basetrans=n&trans_id=0&district_code=0&record_id=0&trans_state=&iSiteUserName=ia-uoim&iSitePassword=mechshop";

            byte[] byteArray = Encoding.UTF8.GetBytes(requestData);
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            //Return response
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private async static Task<string> MakeGetRequest(string url)
        {
            //Create Request
            WebRequest request = WebRequest.Create(url);

            //Return response
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
