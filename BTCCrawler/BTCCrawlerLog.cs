using System;

namespace Crawlers.BTCCrawler
{
    public class CrawlerLog
    {
        public int Id { get; set; }
        public float BuyPrice { get; set; }
        public float ExchangeRate { get; set; }
        public float SellPrice { get; set; }
        public float Difference { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
