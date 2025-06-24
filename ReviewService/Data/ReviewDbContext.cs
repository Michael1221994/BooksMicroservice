using Microsoft.EntityFrameworkCore;
using ReviewService.Models;

namespace ReviewService.Data
{
    public class ReviewDbContext : DbContext
    {
        public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options) { }

        public DbSet<Review> Reviews { get; set; }
    }
}
