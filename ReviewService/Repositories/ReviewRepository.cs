using ReviewService.Models;
using ReviewService.Data; // for ReviewDbContext
using ReviewService.DTOs; // for CreateReviewDto

namespace ReviewService.Repositories
{
    public class ReviewRepository
    {
        private readonly ReviewDbContext _context;
        public ReviewRepository(ReviewDbContext context) => _context = context;

        public IEnumerable<Review> GetAll() => _context.Reviews.ToList();

        public IEnumerable<Review> GetByBookId(int bookId) => _context.Reviews.Where(r => r.BookId == bookId).ToList();

        public void Add(Review review)
        {
            _context.Reviews.Add(review);
            _context.SaveChanges();
        }
           
        public double GetAverageRatingByBookId(int bookId)
        {
            var reviews = _context.Reviews.Where(r => r.BookId == bookId).ToList();
            if (!reviews.Any()) return 0;
            return reviews.Average(r => r.Rating);
        }

        public int GetReviewCountByBookId(int bookId)
        {
            return _context.Reviews.Count(r => r.BookId == bookId);
        }

    }
}