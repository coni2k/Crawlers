namespace Crawler.CrawlerConsoleApp
{
    static class Utils
    {
        public static string TrimEtc(this string value)
        {
            return value.Replace("&nbsp;", "").Trim();
        }
    }
}
