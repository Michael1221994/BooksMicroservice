using Microsoft.AspNetCore.Mvc;
using ReviewService.Models;
using ReviewService.Repositories;
using ReviewService.Services;
using System.Text.Json;
using ReviewService.Data; // for ReviewDbContext
using ReviewService.DTOs; // for CreateReviewDto
using Microsoft.AspNetCore.Authorization;


namespace ReviewService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/review")]
    [Authorize]
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
        //[Authorize]
        public async Task<ActionResult> AddReview([FromBody] CreateReviewDto reviewDto)
        {

            if (reviewDto == null ||
                reviewDto.BookId <= 0 ||
                string.IsNullOrWhiteSpace(reviewDto.ReviewerName) ||
                string.IsNullOrWhiteSpace(reviewDto.Comment) ||
                reviewDto.Rating < 1 || reviewDto.Rating > 5)
            {
                return BadRequest("Invalid review data.");
            }

            var review = new Review
            {
                BookId = reviewDto.BookId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                ReviewerName = reviewDto.ReviewerName
            };

            _repository.Add(review);

            //  all reviews for the same book and the new I just added
            var allReviewsForBook = _repository.GetByBookId(review.BookId);

            // Cache the updated list
            var json = JsonSerializer.Serialize(allReviewsForBook);
            string cacheKey = $"Review:{review.BookId}";
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(5));

            // Log and return response
            _logger.LogInformation("Review {id} added to repository and updated in cache for 5 minutes", review.Id);
            return CreatedAtAction(nameof(GetReviewByBookId), new { bookId = review.BookId }, review);
        }

        [HttpGet("averageRating/{bookId:int:min(1)}")]
        public ActionResult<double> GetAverageRating(int bookId)
        {
            var avg = _repository.GetAverageRatingByBookId(bookId);
            return Ok(avg);
        }

        [HttpGet("reviewCount/{bookId:int:min(1)}")]
        public ActionResult<int> GetReviewCount(int bookId)
        {
            var count = _repository.GetReviewCountByBookId(bookId);
            return Ok(count);
        }


    }
}