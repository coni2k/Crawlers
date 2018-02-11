using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Crawlers.BTCCrawler
{
    class Program
    {
        static HttpClient client = new HttpClient();
        const string BTCTurkApiUrl = "https://www.btcturk.com/api/ticker";
        const string GDaxApiUrl = "https://api.gdax.com/products/BTC-EUR/ticker";

        static void Main(string[] args)
        {
            // Display appInfo
            var appInfo = $"BTCCrawler - Version {Assembly.GetExecutingAssembly().GetName().Version}";

            Trace.WriteLine("");
            Trace.WriteLine(appInfo);
            Trace.WriteLine("");
            Trace.WriteLine(new string('-', appInfo.Length));
            Trace.WriteLine("");

            // Init
            client.DefaultRequestHeaders.Add("User-Agent", "BTCCrawler");

            // Run the cycle
            while (true)
            {
                Crawl();

                Thread.Sleep(AppSettings.CrawlCycle * 1000);
            }
        }

        static void Crawl()
        {
            // Get buy price, exchange rate, sell price and calculate the diff
            var buyEURPrice = GetBuyEURPrice().GetAwaiter().GetResult();
            var exchangeRate = AppSettings.ExchangeRate;
            var sellTRYPrice = GetSellTRYPrice().GetAwaiter().GetResult();
            var sellEURPrice = sellTRYPrice / exchangeRate;
            var difference = sellEURPrice - buyEURPrice;

            var differenceText = $"{DateTime.UtcNow} | Buy: {buyEURPrice:0.0} | Rate: {exchangeRate:0.000} | Sell: {sellEURPrice:0.0} | Diff: { difference:+0.0;-0.0;0} EUR";
            Trace.WriteLine(differenceText);

            // Enough difference, send an alert email
            if (AppSettings.AlertEmail_Enabled && difference > AppSettings.AlertEmail_DifferenceLimit)
            {
                SendAlertEmail(differenceText);
            }

            // Save to db log
            using (var dbContext = new BTCCrawlerContext())
            {
                var log = new CrawlerLog()
                {
                    BuyPrice = buyEURPrice,
                    ExchangeRate = exchangeRate,
                    SellPrice = sellEURPrice,
                    Difference = difference,
                    CreatedOn = DateTime.UtcNow
                };

                dbContext.CrawlerLogSet.Add(log);
                dbContext.SaveChanges();
            }
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

        static void SendAlertEmail(string text)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(AppSettings.AlertEmail_FromEmailAddress)
            };

            mailMessage.To.Add(new MailAddress(AppSettings.AlertEmail_ToEmailAddress));
            mailMessage.Subject = "BTCCrawler - Price alert!";

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
