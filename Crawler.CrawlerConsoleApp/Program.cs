using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Crawler.CrawlerConsoleApp
{
    class Program
    {
        private static int GetMonth(string monthText)
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
                        Trace.WriteLine(monthText);
                        return 1;
                        //throw new ArgumentException("Invalid value: " + monthText, nameof(monthText));
                    }
            }
        }

        static void Main(string[] args)
        {
            Trace.WriteLine($"Begin: {DateTime.Now}");
            Trace.WriteLine("");

            Execute().GetAwaiter().GetResult();

            Trace.WriteLine("");
            Trace.WriteLine($"End: {DateTime.Now}");
            Trace.WriteLine("--------------------------------------------------");

            Console.ReadLine();
        }

        static async Task Execute()
        {
            using (var dbContext = new CrawlerContext())
            {
                var list = Source.GetList().Take(1);

                foreach (var item in list)
                {
                    try
                    {
                        await ProcessAdvertisement(item, dbContext);
                    }
                    catch (WebException exception)
                    {
                        Trace.WriteLine(exception);

                        var response = (HttpWebResponse)exception.Response;
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            Trace.WriteLine("");
                            Trace.WriteLine("Blocked, aborting...");
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine(exception);
                    }

                    Trace.WriteLine("-");
                }

                await dbContext.SaveChangesAsync();
            }
        }

        static async Task ProcessAdvertisement(string advertisementUrl, CrawlerContext dbContext)
        {
            // Advertisement no
            var advertisementNoText = advertisementUrl.Substring(advertisementUrl.LastIndexOf("-", StringComparison.InvariantCulture) + 1,
                advertisementUrl.LastIndexOf("/", StringComparison.InvariantCulture) - (advertisementUrl.LastIndexOf("-", StringComparison.InvariantCulture) + 1));

            var advertisementNo = int.Parse(advertisementNoText);

            // Look for an existing advertisement
            var advertisement = dbContext.AdvertisementSet.SingleOrDefault(adv => adv.AdvertisementNo == advertisementNo);

            // New advertisement?
            var newAdvertisement = advertisement == null;
            if (newAdvertisement)
            {
                advertisement = new Advertisement
                {
                    AdvertisementNo = advertisementNo,
                    AdvertisementUrl = advertisementUrl
                };

                dbContext.AdvertisementSet.Add(advertisement);
            }

            // Already deleted?
            if (!newAdvertisement && advertisement.Deleted)
            {
                Trace.WriteLine($"Already deleted: {advertisementNo}");
                return;
            }

            // Already crawled?
            if (!newAdvertisement && advertisement.AdvertisementDate.HasValue)
            {
                // Todo Recrawl
                Trace.WriteLine($"Already crawled: {advertisementNo}");
                return;
            }

            // Previous crawl failed, but not old enough?
            if (!newAdvertisement && DateTime.UtcNow.Subtract(advertisement.ModifiedOn).Minutes < 60)
            {
                Trace.WriteLine($"Not crawled, but not old enough: {advertisementNo}");
                return;
            }

            // Process html
            //var html = await RequestHtml(advertisement);
            var html = ReadHtmlFile(advertisement.AdvertisementNo);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Main node
            var infoNode = doc.DocumentNode
                .Descendants()
                .FirstOrDefault(node => node.Attributes["class"] != null && node.Attributes["class"]
                .Value
                .TrimEtc() == "classifiedInfo");

            // Crawl failed?
            if (infoNode == null)
            {
                Trace.WriteLine($"Crawl failed: {advertisement.AdvertisementNo}");
                return;
            }

            // Price
            var price = infoNode.Descendants("h3")
                .Single()
                .FirstChild
                .InnerText
                .Replace(".", "")
                .Replace(" TL", "")
                .TrimEtc();

            // Location
            var locationNodes = infoNode.Descendants("h2").Single().Descendants("a").ToList();
            var province = locationNodes[0].InnerText.TrimEtc();
            var district = locationNodes[1].InnerText.TrimEtc();
            var neighborhood = locationNodes[2].InnerText.TrimEtc();

            // Info list
            var infoListLabels = infoNode.Descendants("ul").Single().Descendants("strong").ToList();
            var infoListValues = infoNode.Descendants("ul").Single().Descendants("span").ToList();

            // Advertisement date
            var advertisementDateText = infoListValues[1].InnerText.TrimEtc();
            var advertisementDay = int.Parse(advertisementDateText.Substring(0,
                advertisementDateText.IndexOf(" ", StringComparison.InvariantCulture)));
            var advertisementMonth = GetMonth(advertisementDateText.Substring(
                advertisementDateText.IndexOf(" ", StringComparison.InvariantCulture) + 1,
                advertisementDateText.LastIndexOf(" ", StringComparison.InvariantCulture) -
                (advertisementDateText.IndexOf(" ", StringComparison.InvariantCulture) + 1)));
            var advertisementYear = int.Parse(advertisementDateText.Substring(
                    advertisementDateText.LastIndexOf(" ", StringComparison.InvariantCulture) + 1));
            var advertisementDate = new DateTime(advertisementYear, advertisementMonth, advertisementDay);

            var advertisementType = infoListValues[2].InnerText.TrimEtc();
            var squareMeters = infoListValues[3].InnerText.TrimEtc();
            var numberOfRooms = infoListValues[4].InnerText.TrimEtc();
            var buildingAge = infoListValues[5].InnerText.TrimEtc();
            var floor = infoListValues[6].InnerText.TrimEtc();
            var numberOfFloors = infoListValues[7].InnerText.TrimEtc();
            var heatingSystem = infoListValues[8].InnerText.TrimEtc();
            var numberOfToilets = infoListValues[9].InnerText.TrimEtc();
            var furnished = infoListValues[10].InnerText.TrimEtc();
            var currentState = infoListValues[11].InnerText.TrimEtc();
            var inComplex = infoListValues[12].InnerText.TrimEtc();
            var subscriptionCosts = infoListValues[13].InnerText.TrimEtc();

            // Only some of the advertisements has "Complex Name"
            var hasComplexName = infoListLabels[14].InnerText == "Site Adı";
            var complexName = hasComplexName ? infoListValues[14].InnerText.TrimEtc() : string.Empty;

            var suitableForLoadIndex = hasComplexName ? 15 : 14;
            var advertisementOwnerIndex = hasComplexName ? 16 : 15;
            var swappableIndex = hasComplexName ? 17 : 16;

            var suitableForLoan = infoListValues[suitableForLoadIndex].InnerText.TrimEtc();
            var advertisementOwner = infoListValues[advertisementOwnerIndex].InnerText.TrimEtc();
            var swappable = infoListValues[swappableIndex].InnerText.TrimEtc();

            Trace.WriteLine($"advertisementNo: {advertisementNo}");
            Trace.WriteLine($"advertisementDate: {advertisementDate}");
            Trace.WriteLine($"province: {province}");
            Trace.WriteLine($"district: {district}");
            Trace.WriteLine($"neighborhood: {neighborhood}");
            Trace.WriteLine($"price: {price}");
            Trace.WriteLine($"advertisementType: {advertisementType}");
            Trace.WriteLine($"squareMeters: {squareMeters}");
            Trace.WriteLine($"numberOfRooms: {numberOfRooms}");
            Trace.WriteLine($"buildingAge: {buildingAge}");
            Trace.WriteLine($"floor: {floor}");
            Trace.WriteLine($"totalFloors: {numberOfFloors}");
            Trace.WriteLine($"heatingSystem: {heatingSystem}");
            Trace.WriteLine($"numberOfFloors: {numberOfFloors}");
            Trace.WriteLine($"numberOfToilets: {numberOfToilets}");
            Trace.WriteLine($"furnished: {furnished}");
            Trace.WriteLine($"currentState: {currentState}");
            Trace.WriteLine($"inComplex: {inComplex}");
            Trace.WriteLine($"subscriptionCosts: {subscriptionCosts}");
            Trace.WriteLine($"complexName: {complexName}");
            Trace.WriteLine($"suitableForLoan: {suitableForLoan}");
            Trace.WriteLine($"advertisementOwner: {advertisementOwner}");
            Trace.WriteLine($"swappable: {swappable}");

            advertisement.Province = province;
            advertisement.District = district;
            advertisement.Neighborhood = neighborhood;
            advertisement.Price = decimal.Parse(price);
            advertisement.AdvertisementDate = advertisementDate;
            advertisement.AdvertisementType = advertisementType;
            advertisement.SquareMeters = int.Parse(squareMeters);
            advertisement.NumberOfRooms = numberOfRooms;
            advertisement.BuildingAge = buildingAge;
            advertisement.Floor = floor;
            advertisement.NumberOfFloors = numberOfFloors;
            advertisement.HeatingSystem = heatingSystem;
            advertisement.NumberOfToilets = numberOfToilets;
            advertisement.Furnished = furnished;
            advertisement.CurrentState = currentState;
            advertisement.InComplex = inComplex;
            advertisement.SubscriptionCosts = subscriptionCosts;
            advertisement.ComplexName = complexName;
            advertisement.SuitableForLoad = suitableForLoan;
            advertisement.AdvertisementOwner = advertisementOwner;
            advertisement.Swappable = swappable;
            advertisement.ModifiedOn = DateTime.UtcNow;
        }

        static async Task<string> RequestHtml(Advertisement advertisement)
        {
            // Cooldown - 1001 didn't work!
            Thread.Sleep(5001);

            var request = (HttpWebRequest)WebRequest.Create(advertisement.AdvertisementUrl);

            // Chrome
            // Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
            // Cache-Control: no-cache
            // Cookie: vid=251; cdid=z3ms7al5YWZFH1xk5a12af49; MS1=https://www.google.nl/; __gfp_64b=ymQlACAE5aclwIuPnfNlCBhFmeqgsm82HOkUR7grwdT.f7; showPremiumBanner=false; __gads=ID=f79eadd924f8acef:T=1511173963:S=ALNI_Mb9yqYqoBUsP3rpm0f34PG4Fw0QIA; userLastSearchSplashClosed=true; wm-ueug=%22fdb1fbeb-586b-3f87-61be-5515c85e3251%22; wm-fgug=1; wm-ASRep-213909=1; emlakt=yeni; _sm_au_c=iDV0jQMKLFkFskf70f; xsrf-token=9a380393e210652f9b7d1cfd6fd20a8552211238; language=tr; gcd=20171129211839; MDR=20171129; lastVisit=20171129; userType=yeni_bireysel; shuid=cPX4A1VsoSIEky3xvEoM45A; dopingPurchase=false; getPurchase=false; st=a2c9cc949113d430a9e794c7e1aecd9db6bcc337f208f93a6f61947a90e24a0d889516fc0611039bac76f1ac446688f2ccdc2788166542f12; dtLatC=5; rxVisitor=15118947149954AKF11TH45A7T4R0SNN0FTM6UNHQ3DGA; dtCookie=2$E4C5A9BE318043B3434969B7424CC9FA|RUM+Default+Application|1; dtSa=-; rxvt=1512160444472|1512155424841; dtPC=2$158603607_63h-vDRMGKAMNIJLFIBBCICBGALCBOHFHFJKMLN; bannerClosed=false; geoipCity=noord-holland; geoipIsp=tele2_nederland; SPSI=b3f06eddb056422b0dbc722292f21e6d; sbtsck=jav; nwsh=std; _dc_gtm_UA-235070-1=1; spcsrf=bb9304dbd61e0acd7dc4433abacfb217; PRLST=kQ; UTGv2=h4a07ed5196caa15b65919ce1cfe8c0eaf38; segIds=; _ga=GA1.2.386445408.1511173963; _gid=GA1.2.1165316201.1512155230
            // Host: www.sahibinden.com
            // Pragma: no-cache
            // User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Headers["Cache-Control"] = "no-cache";
            request.Headers["Cookie"] = "vid=251; cdid=z3ms7al5YWZFH1xk5a12af49; MS1=https://www.google.nl/; __gfp_64b=ymQlACAE5aclwIuPnfNlCBhFmeqgsm82HOkUR7grwdT.f7; showPremiumBanner=false; __gads=ID=f79eadd924f8acef:T=1511173963:S=ALNI_Mb9yqYqoBUsP3rpm0f34PG4Fw0QIA; userLastSearchSplashClosed=true; wm-ueug=%22fdb1fbeb-586b-3f87-61be-5515c85e3251%22; wm-fgug=1; wm-ASRep-213909=1; emlakt=yeni; _sm_au_c=iDV0jQMKLFkFskf70f; xsrf-token=9a380393e210652f9b7d1cfd6fd20a8552211238; language=tr; gcd=20171129211839; MDR=20171129; lastVisit=20171129; userType=yeni_bireysel; shuid=cPX4A1VsoSIEky3xvEoM45A; dopingPurchase=false; getPurchase=false; st=a2c9cc949113d430a9e794c7e1aecd9db6bcc337f208f93a6f61947a90e24a0d889516fc0611039bac76f1ac446688f2ccdc2788166542f12; dtLatC=5; rxVisitor=15118947149954AKF11TH45A7T4R0SNN0FTM6UNHQ3DGA; dtCookie=2$E4C5A9BE318043B3434969B7424CC9FA|RUM+Default+Application|1; dtSa=-; rxvt=1512160444472|1512155424841; dtPC=2$158603607_63h-vDRMGKAMNIJLFIBBCICBGALCBOHFHFJKMLN; bannerClosed=false; geoipCity=noord-holland; geoipIsp=tele2_nederland; SPSI=b3f06eddb056422b0dbc722292f21e6d; sbtsck=jav; nwsh=std; _dc_gtm_UA-235070-1=1; spcsrf=bb9304dbd61e0acd7dc4433abacfb217; PRLST=kQ; UTGv2=h4a07ed5196caa15b65919ce1cfe8c0eaf38; segIds=; _ga=GA1.2.386445408.1511173963; _gid=GA1.2.1165316201.1512155230";
            request.Host = "www.sahibinden.com";
            request.Headers["Pragma"] = "no-cache";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36";

            // Firefox
            // Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
            // Cache-Control: max-age=0
            // Cookie: MS1=https://www.sahibinden.com/satilik-daire/istanbul/insaat-firmasindan?date=3days&price_min=50001&a24_min=85&pagingSize=50&a812=40728&a812=40605&a812=40604&a812=40603&a812=40602&sorting=price_asc&viewType=Classic&a811=62370&a811=62371&a811=62372&a811=62373&a811=97315&a811=40861&a811=97314&a811=97317&a811=97316&a811=40986&a811=40601&a811=97311&a811=40600&a811=97310&a811=97313&a811=97312&a811=50278&a811=40593&a811=97308&a811=40592&a811=97309&a811=40597&a811=40596&a811=62364&a811=40595&a811=62365&a811=40594&a811=62366&a811=62367&a811=62368&a811=40599&a811=62369&a811=40598&a811=236072&a811=40708&a811=52042&price_max=135000; MDR=20171106; vid=829; cdid=0Yj6KV6MylOAgmES5a1b02fa; nwsh=std; showPremiumBanner=false; __gfp_64b=pqPGTrQ11A6Q8tm59l.85DLItji9IHdIkMMHpgAVWef.b7; _ga=GA1.2.1120270113.1511719678; __gads=ID=f8db686f0512feb0:T=1511719679:S=ALNI_MbMvA_snREKy4mDvSJmlfqsDpUAEA; st=a8020d5f9c037425cbe2cb2145d494e4e21fb411906c0e8b4fb3ceb0909982b35e05dd8022e5daa8746c15666af73d8db02aef163a601dfea; SPSI=f2cadc070803086215a1134fb9517333; UTGv2=h4cce8c482f0976ec420c1a3d787a6ccdc36; sbtsck=jav; PRLST=rW; segIds=; _gid=GA1.2.1291889508.1512234546; rxVisitor=151223482253163V1GKBV7THM1OLONVC5TV4FS7DTTQT9; dtPC=2$247750040_559h-vDRJWZFMHZRMBYWIRUNIHUCTYMTJDGTIGJC; rxvt=1512249564766|1512247750046; dtSa=true%7CKD116%7C-1%7CPage%3A%20detay%7C-%7C1512247764602%7C247750040_559%7Chttps%3A%2F%2Fwww.sahibinden.com%2Filan%2Femlak-konut-satilik-arslan-insaat-tan-2-plus1-yuksek-giris-daire-439053178%2Fdetay%7C%C4%B0n%C5%9Faat%20Firmas%C4%B1ndan%202%2B1%5Ec%20142%20m2%20Sat%C4%B1l%C4%B1k%20Daire%20115.000%20TL%27ye%20sahibinden.com%27da%20-%20439053178%7C1512247758313%7C; dtLatC=83; spcsrf=8312b3e58c1814c47bfbbaea97778933; geoipCity=noord-holland; geoipIsp=tele2_nederland; _dc_gtm_UA-235070-1=1; dtCookie=2$8VU1C0RID5SUEHNGD0K7QV8T867SB2F5|RUM+Default+Application|1
            // Host: www.sahibinden.com
            // User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //request.Headers["Cache-Control"] = "max-age=0";
            //request.Headers["Cookie"] = "MS1=https://www.sahibinden.com/satilik-daire/istanbul/insaat-firmasindan?date=3days&price_min=50001&a24_min=85&pagingSize=50&a812=40728&a812=40605&a812=40604&a812=40603&a812=40602&sorting=price_asc&viewType=Classic&a811=62370&a811=62371&a811=62372&a811=62373&a811=97315&a811=40861&a811=97314&a811=97317&a811=97316&a811=40986&a811=40601&a811=97311&a811=40600&a811=97310&a811=97313&a811=97312&a811=50278&a811=40593&a811=97308&a811=40592&a811=97309&a811=40597&a811=40596&a811=62364&a811=40595&a811=62365&a811=40594&a811=62366&a811=62367&a811=62368&a811=40599&a811=62369&a811=40598&a811=236072&a811=40708&a811=52042&price_max=135000; MDR=20171106; vid=829; cdid=0Yj6KV6MylOAgmES5a1b02fa; nwsh=std; showPremiumBanner=false; __gfp_64b=pqPGTrQ11A6Q8tm59l.85DLItji9IHdIkMMHpgAVWef.b7; _ga=GA1.2.1120270113.1511719678; __gads=ID=f8db686f0512feb0:T=1511719679:S=ALNI_MbMvA_snREKy4mDvSJmlfqsDpUAEA; st=a8020d5f9c037425cbe2cb2145d494e4e21fb411906c0e8b4fb3ceb0909982b35e05dd8022e5daa8746c15666af73d8db02aef163a601dfea; SPSI=f2cadc070803086215a1134fb9517333; UTGv2=h4cce8c482f0976ec420c1a3d787a6ccdc36; sbtsck=jav; PRLST=rW; segIds=; _gid=GA1.2.1291889508.1512234546; rxVisitor=151223482253163V1GKBV7THM1OLONVC5TV4FS7DTTQT9; dtPC=2$247750040_559h-vDRJWZFMHZRMBYWIRUNIHUCTYMTJDGTIGJC; rxvt=1512249564766|1512247750046; dtSa=true%7CKD116%7C-1%7CPage%3A%20detay%7C-%7C1512247764602%7C247750040_559%7Chttps%3A%2F%2Fwww.sahibinden.com%2Filan%2Femlak-konut-satilik-arslan-insaat-tan-2-plus1-yuksek-giris-daire-439053178%2Fdetay%7C%C4%B0n%C5%9Faat%20Firmas%C4%B1ndan%202%2B1%5Ec%20142%20m2%20Sat%C4%B1l%C4%B1k%20Daire%20115.000%20TL%27ye%20sahibinden.com%27da%20-%20439053178%7C1512247758313%7C; dtLatC=83; spcsrf=8312b3e58c1814c47bfbbaea97778933; geoipCity=noord-holland; geoipIsp=tele2_nederland; _dc_gtm_UA-235070-1=1; dtCookie=2$8VU1C0RID5SUEHNGD0K7QV8T867SB2F5|RUM+Default+Application|1";
            //request.Host = "www.sahibinden.com";
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0";

            var html = string.Empty;
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.MovedPermanently)
                {
                    advertisement.Deleted = true;
                }
                else
                {
                    var data = response.GetResponseStream();

                    if (data == null)
                    {
                        advertisement.Deleted = true;
                    }
                    else
                    {
                        // Timeout: 10 seconds
                        data.ReadTimeout = 10000;

                        using (var sr = new StreamReader(data))
                        {
                            try
                            {
                                html = sr.ReadToEnd();
                            }
                            catch
                            {
                                advertisement.Deleted = true;
                            }
                        }
                    }
                }
            }

            // Write html to file
            var filePath = GetFilePath(advertisement.AdvertisementNo);
            File.WriteAllText(filePath, html);

            return html;
        }

        static string GetFilePath(int advertisementNo)
        {
            return $@".\Advertisements\{advertisementNo}.html";
        }

        static string ReadHtmlFile(int advertisementNo)
        {
            var filePath = GetFilePath(advertisementNo);

            return File.Exists(filePath)
                ? File.ReadAllText(filePath, Encoding.UTF8)
                : string.Empty;
        }
    }
}
