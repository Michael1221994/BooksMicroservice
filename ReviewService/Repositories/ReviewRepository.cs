using ReviewService.Models;

namespace ReviewService.Repositories
{
    public class ReviewRepository
    {
        private static readonly List<Review> _reviews = new List<Review>();
        public IEnumerable<Review> GetAll() => _reviews;
        public IEnumerable<Review> GetByBookId(int bookId) => _reviews.Where(r => r.BookId == bookId);
        public void Add(Review review)
        {
            review.Id=_reviews.Count+1;
            review.CreatedAt=DateTime.UtcNow;
            _reviews.Add(review);
        }
    }
}