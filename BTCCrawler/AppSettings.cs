using System.Configuration;

namespace Crawlers.BTCCrawler
{
    public static class AppSettings
    {
        public static string FromEmailAddress => ConfigurationManager.AppSettings["FromEmailAddress"];
        public static string ToEmailAddress => ConfigurationManager.AppSettings["ToEmailAddress"];
    }
}
