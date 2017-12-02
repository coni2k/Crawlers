using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Crawler.CrawlerConsoleApp
{
    public class CrawlerContext : DbContext
    {
        public virtual DbSet<Advertisement> AdvertisementSet { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Conventions
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
