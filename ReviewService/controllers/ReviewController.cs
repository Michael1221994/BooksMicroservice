using Microsoft.AspNetCore.Mvc;
using ReviewService.Models;
using ReviewService.Repositories;
using ReviewService.Services;
using System.Text.Json;

namespace ReviewService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewRepository _repository;
        private readonly RedisCacheService _cache;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ReviewRepository repository, RedisCacheService cache, ILogger<ReviewController> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }



        [HttpGet("getByID/{bookId:int:min(1)}", Name = "GetReviewByBookId")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewByBookId(int bookId)
        {
            string cacheKey = $"Review:{bookId}";
            var cached = await _cache.GetCachedValue(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Review {bookId} found in cache", bookId);
                var cachedReview = JsonSerializer.Deserialize<IEnumerable<Review>>(cached);
                return Ok(cachedReview);
            }
            var reviews = _repository.GetByBookId(bookId);
            if (reviews == null)
            {
                _logger.LogWarning("Review {bookId} not found in cache or repository", bookId);
                return NotFound();
            }
            var json = JsonSerializer.Serialize(reviews);
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Review {bookId} found in repository", bookId);
            return Ok(reviews);

        }

        [HttpGet("getAll")]
        public ActionResult<IEnumerable<Review>> GetAll()
        {
            return Ok(_repository.GetAll());
        }
        

         [HttpPost]
        public async Task<ActionResult> AddReview(Review review)
        {
           var allReviewsForBook = _repository.GetByBookId(review.BookId); //  get all reviews for the same book
           var json = JsonSerializer.Serialize(allReviewsForBook);          //  serialize all of them
           string cachekey = $"Review:{review.BookId}";                     //  cache by bookId
            await _cache.SetCachedValue(cachekey, json, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Review {id} added to repository and in cache for the next 5min", review.Id);
            return CreatedAtAction(nameof(GetReviewByBookId), new { bookId = review.BookId }, review);
        }
    }
}