using System;

namespace Crawlers.BTCCrawler
{
    public class GDaxTicker
    {
        public int TradeId { get; set; }
        public float Price { get; set; }
        public float Size { get; set; }
        public float Bid { get; set; }
        public float Ask { get; set; }
        public float Volume { get; set; }
        public DateTime Time { get; set; }
    }
}
