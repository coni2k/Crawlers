using System.Data.Entity;

namespace Crawlers.RealEstateCrawler
{
    public class RealEstateCrawlerContext : DbContext
    {
        public virtual DbSet<Advertisement> AdvertisementSet { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Advertisement>().ToTable($"{nameof(RealEstateCrawler)}_{nameof(Advertisement)}");
        }
    }
}
