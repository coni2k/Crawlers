namespace Crawlers.BTCCrawler
{
    public class BTCTurkTicker
    {
        public BTCTurkPair[] data { get; set; }
        public bool success { get; set; }
        public object message { get; set; }
        public int code { get; set; }
    }

    public class BTCTurkPair
    {
        public string pair { get; set; }
        public string pairNormalized { get; set; }
        public float timestamp { get; set; }
        public float last { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float bid { get; set; }
        public float ask { get; set; }
        public float open { get; set; }
        public float volume { get; set; }
        public float average { get; set; }
        public float daily { get; set; }
        public float dailyPercent { get; set; }
        public string denominatorSymbol { get; set; }
        public string numeratorSymbol { get; set; }
        public int order { get; set; }
    }
}
