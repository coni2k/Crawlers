using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crawlers.RealEstateCrawler
{
    public class Advertisement
    {
        public int Id { get; set; }
        [Index]
        public int AdvertisementNo { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; }
        public decimal Price { get; set; }
        public DateTime? AdvertisementDate { get; set; }
        public string AdvertisementType { get; set; }
        public int SquareMeters { get; set; }
        public string NumberOfRooms { get; set; }
        public string BuildingAge { get; set; }
        public string Floor { get; set; }
        public string NumberOfFloors { get; set; }
        public string HeatingSystem { get; set; }
        public string NumberOfToilets { get; set; }
        public string Furnished { get; set; }
        public string CurrentState { get; set; }
        public string InComplex { get; set; }
        public string ComplexName { get; set; }
        public string SubscriptionCosts { get;set; }
        public string SuitableForLoad { get; set; }
        public string AdvertisementOwner { get; set; }
        public string Swappable { get; set; }
        public string AdvertisementUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool Deleted { get; set; }

        public Advertisement()
        {
            CreatedOn = DateTime.UtcNow;
            ModifiedOn = DateTime.UtcNow;
        }
    }
}
