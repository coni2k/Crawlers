using System;

namespace Crawler.CrawlerConsoleApp
{
    public static class Utils
    {
        public static int GetMonth(string monthText)
        {
            switch (monthText)
            {
                case "Ocak": return 1;
                case "Şubat": return 2;
                case "Mart": return 3;
                case "Nisan": return 4;
                case "Mayıs": return 5;
                case "Haziran": return 6;
                case "Temmuz": return 7;
                case "Ağustos": return 8;
                case "Eylül": return 9;
                case "Ekim": return 10;
                case "Kasım": return 11;
                case "Aralık": return 12;
                default:
                    {
                        throw new ArgumentException($"Invalid value: {monthText}", nameof(monthText));
                    }
            }
        }

        public static string TrimEtc(this string value)
        {
            return value.Replace("&nbsp;", "").Trim();
        }
    }
}
