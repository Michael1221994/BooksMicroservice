using Microsoft.AspNetCore.Mvc;
using ReviewService.Models;
using ReviewService.Repositories;
using ReviewService.Services;
using ReviewService.DTOs;
using System.Text.Json;

namespace ReviewService.Controllers
{
    [ApiVersion("2.0")]
    [ApiController]
    [Route("v{version:apiVersion}/review")]
    public class ReviewV2Controller : ControllerBase
    {
        private readonly ReviewRepository _repository;
        private readonly RedisCacheService _cache;
        private readonly ILogger<ReviewV2Controller> _logger;

        public ReviewV2Controller(ReviewRepository repository, RedisCacheService cache, ILogger<ReviewV2Controller> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("getByID/{bookId:int:min(1)}", Name = "GetReviewByBookIdV2")]//http://localhost:5212/api/v2/Review/getByID/2
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewByBookIdV2(int bookId)
        {
            string cacheKey = $"ReviewV2:{bookId}";
            var cached = await _cache.GetCachedValue(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("Review V2 {bookId} found in cache", bookId);
                var cachedReview = JsonSerializer.Deserialize<IEnumerable<Review>>(cached);
                return Ok(cachedReview);
            }

            var reviews = _repository.GetByBookId(bookId);
            if (reviews == null)
            {
                _logger.LogWarning("Review V2 {bookId} not found", bookId);
                return NotFound();
            }

            var json = JsonSerializer.Serialize(reviews);
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(10)); // longer cache in v2
            return Ok(reviews);
        }

        [HttpGet("getAll")]//http://localhost:5212/api/v2/Review/getAll
        public ActionResult<IEnumerable<Review>> GetAllV2()
        {
            var reviews = _repository.GetAll();
            return Ok(new { count = reviews.Count(), data = reviews }); // changed format
        }

        [HttpPost]
        public async Task<ActionResult> AddReviewV2([FromBody] CreateReviewDto reviewDto)
        {
            if (reviewDto == null ||
                reviewDto.BookId <= 0 ||
                string.IsNullOrWhiteSpace(reviewDto.ReviewerName) ||
                string.IsNullOrWhiteSpace(reviewDto.Comment) ||
                reviewDto.Rating < 1 || reviewDto.Rating > 5)
            {
                return BadRequest("Invalid review data in v2.");
            }

            var review = new Review
            {
                BookId = reviewDto.BookId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                ReviewerName = reviewDto.ReviewerName
            };

            _repository.Add(review);

            var allReviewsForBook = _repository.GetByBookId(review.BookId);
            string cacheKey = $"ReviewV2:{review.BookId}";
            await _cache.SetCachedValue(cacheKey, JsonSerializer.Serialize(allReviewsForBook), TimeSpan.FromMinutes(10));

            return CreatedAtAction(nameof(GetReviewByBookIdV2), new { bookId = review.BookId }, review);
        }

        [HttpGet("onlyInV2")]//http://localhost:5212/api/v2/Review/onlyInV2
        public ActionResult<string> OnlyInV2()
        {
            return Ok("This endpoint only exists in Review API v2");
        }
    }
}
