using System;

namespace Crawlers.BTCCrawler
{
    public class FixerExchangeRate
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        public Items Rates { get; set; }

        public class Items
        {
            public float Try { get; set; }
        }
    }
}
