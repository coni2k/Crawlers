using System.Collections.Generic;

namespace Crawlers.RealEstateCrawler
{
    public static class Source
    {
        public static IEnumerable<string> GetList()
        {
            var list = new List<string>
            {
                "https://www.sahibinden.com/ilan/emlak-konut-satilik-b.duzu-familyadan-genis-mutfak%2Cgenis-salon-ve-emsalsiz-fiyat-517985398/detay"
            };

            return list;
        }
    }
}
