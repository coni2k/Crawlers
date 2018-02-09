using System.Data.Entity;

namespace Crawlers.BTCCrawler
{
    public class BTCCrawlerContext : DbContext
    {
        public virtual DbSet<CrawlerLog> CrawlerLogSet { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CrawlerLog>().ToTable($"{nameof(BTCCrawler)}_{nameof(CrawlerLog)}");
        }
    }
}
