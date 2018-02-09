using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Crawlers.BTCCrawler
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static string BTCTurkApiUrl = "https://www.btcturk.com/api/ticker";
        static string FixerApiUrl = "https://api.fixer.io/latest?symbols=TRY";
        static string GDaxApiUrl = "https://api.gdax.com/products/BTC-EUR/ticker";
        static int cycle = 60 * 1000;
        static int alertLimit = 250;

        static void Main(string[] args)
        {
            // Init
            client.DefaultRequestHeaders.Add("User-Agent", "BTCCrawler");

            while (true)
            {
                Crawl();

                Thread.Sleep(cycle);
            }
        }

        static void Crawl()
        {
            Trace.WriteLine($"Begin: {DateTime.UtcNow}");
            Trace.WriteLine("");

            var buyPrice = GetBuyEURPrice().GetAwaiter().GetResult();
            var exchangeRate = GetExchangeRate().GetAwaiter().GetResult();
            var sellPrice = GetSellTRYPrice().GetAwaiter().GetResult();
            var difference = (sellPrice / exchangeRate) - buyPrice;

            Trace.WriteLine($"{difference} EUR");

            // Alert!
            if (difference > alertLimit)
            {
                SendAlertEmail(difference);
            }

            // Save to db log
            using (var dbContext = new BTCCrawlerContext())
            {
                var log = new CrawlerLog()
                {
                    BuyPrice = buyPrice,
                    ExchangeRate = exchangeRate,
                    SellPrice = sellPrice,
                    Difference = difference,
                    CreatedOn = DateTime.UtcNow
                };

                dbContext.CrawlerLogSet.Add(log);
                dbContext.SaveChanges();
            }

            Trace.WriteLine("");
            Trace.WriteLine($"End: {DateTime.UtcNow}");
            Trace.WriteLine("--------------------------------------------------");
        }

        static async Task<float> GetBuyEURPrice()
        {
            var response = await client.GetAsync(GDaxApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var ticker = await response.Content.ReadAsAsync<GDaxTicker>();
                return ticker.Price;
            }

            return 0;
        }

        static async Task<float> GetExchangeRate()
        {
            var response = await client.GetAsync(FixerApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var exchangeRate = await response.Content.ReadAsAsync<FixerExchangeRate>();
                return exchangeRate.Rates.Try;
            }

            return 0;
        }

        static async Task<float> GetSellTRYPrice()
        {
            var response = await client.GetAsync(BTCTurkApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var ticker = await response.Content.ReadAsAsync<IEnumerable<BTCTurkTicker>>();
                return ticker.First().Last;
            }

            return 0;
        }

        static void SendAlertEmail(float difference)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(AppSettings.FromEmailAddress)
            };

            mailMessage.To.Add(new MailAddress(AppSettings.ToEmailAddress));
            mailMessage.Subject = "BTCCrawler - Price alert!";

            var text = $"{difference} EUR";
            var html = text;

            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

            using (var smtpClient = new SmtpClient())
            {
                try
                {
                    smtpClient.Send(mailMessage);
                }
                catch (SmtpException ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }
    }
}
