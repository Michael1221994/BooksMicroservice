using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ReviewService.Data
{
    public class ReviewDbContextFactory : IDesignTimeDbContextFactory<ReviewDbContext>
    {
        public ReviewDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ReviewDbContext>();

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

            return new ReviewDbContext(optionsBuilder.Options);
        }
    }
}
