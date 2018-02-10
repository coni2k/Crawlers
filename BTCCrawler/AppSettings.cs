using System.Configuration;

namespace Crawlers.BTCCrawler
{
    public static class AppSettings
    {
        /// <summary>In seconds</summary>
        public static int CrawlCycle => int.Parse(ConfigurationManager.AppSettings["CrawlCycle"]);

        public static float ExchangeRate => float.Parse(ConfigurationManager.AppSettings["ExchangeRate"]);

        public static bool AlertEmail_Enabled => bool.Parse(ConfigurationManager.AppSettings["AlertEmail_Enabled"]);
        
        /// <summary>Only if "difference" will be bigger than this amount (in EUR), alert email will be send</summary>
        public static int AlertEmail_DifferenceLimit => int.Parse(ConfigurationManager.AppSettings["AlertEmail_DifferenceLimit"]);

        public static string AlertEmail_FromEmailAddress => ConfigurationManager.AppSettings["AlertEmail_FromEmailAddress"];

        public static string AlertEmail_ToEmailAddress => ConfigurationManager.AppSettings["AlertEmail_ToEmailAddress"];
    }
}
