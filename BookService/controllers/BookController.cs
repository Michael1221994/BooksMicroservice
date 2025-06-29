using Microsoft.AspNetCore.Mvc;
using BookService.Models;
using BookService.Repositories;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http.HttpResults;
using BookService.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using BookService.Dtos;
using ReviewService.Grpc;
using Microsoft.AspNetCore.Authorization;



namespace BookService.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/book")]
    //[Authorize]
    public class BookController : ControllerBase
    {
        private readonly ILogger<BookController> _logger; //needed to log to see if the result is from the cache or if it's from the list
        private readonly BookRepository _repository;
        private readonly RedisCacheService _cache;
        private readonly ReviewGrpcService.ReviewGrpcServiceClient _reviewClient;


        public BookController(BookRepository repository, RedisCacheService cache, ILogger<BookController> logger, ReviewGrpcService.ReviewGrpcServiceClient reviewClient)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
            _reviewClient = reviewClient;

        }

        //[Authorize]
        [HttpGet("getReviewStats/{bookId}")]
        public async Task<IActionResult> GetReviewStats(int bookId)
        {
            try
            {
                var averageRatingResponse = await _reviewClient.GetAverageRatingAsync(
                    new BookIdRequest { BookId = bookId }
                );

                var reviewCountResponse = await _reviewClient.GetReviewCountAsync(
                    new BookIdRequest { BookId = bookId }
                );

                return Ok(new
                {
                    BookId = bookId,
                    AverageRating = averageRatingResponse.AverageRating,
                    ReviewCount = reviewCountResponse.Count
                });
            }
            catch (Exception ex)
            {
                // Log error if needed
                return StatusCode(500, $"Error retrieving review stats: {ex.Message}");
            }
        }


        //[Authorize]
        [HttpGet("getAll")]
        public ActionResult<IEnumerable<Book>> GetAll()
        {
            return Ok(_repository.GetAll());
        }

        //[Authorize]
        [HttpGet("getByName/{Name:}", Name = "GetBookByName")]
        public async Task<ActionResult<Book>> GetByName(string Name)
        {
            string cacheKey = $"Book:{Name}";

            var cached = await _cache.GetCachedValue(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("Book {id} found in cache", Name); //when there's a cache hit it logs it 
                var cachedBook = JsonSerializer.Deserialize<Book>(cached);// turning the json into a book object so that I can return it
                return Ok(cachedBook);

            }
            //after the 5mins I set. It'll be a cache miss so I'll look for it in the list

            var book = _repository.GetByName(Name);//look for it in the list

            if (book == null)
            {
                _logger.LogWarning("Book {id} not found in cache or repository", Name);
                return NotFound();

            }

            var json = JsonSerializer.Serialize(book); //turn the book into json
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(5));//store it in the cache for later
            _logger.LogInformation("Book {id} found in repository", Name);
            return Ok(book);
        }

        //[Authorize]
        [HttpPost]
        public async Task<ActionResult> AddBook(CreateBookDto bookDto)
        {
            if (string.IsNullOrWhiteSpace(bookDto.Title))
                return BadRequest("Title is required.");

            if (string.IsNullOrWhiteSpace(bookDto.Author))
                return BadRequest("Author is required.");

            if (string.IsNullOrWhiteSpace(bookDto.ISBN))
                return BadRequest("ISBN is required.");

            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                ISBN = bookDto.ISBN
            };

            _repository.Add(book);
            var json = JsonSerializer.Serialize(book);//after adding the book I save it in the cache for 5 minutes
            string cachekey = $"Book:{book.Id}";
            await _cache.SetCachedValue(cachekey, json, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Book {id} added to repository and cached for the next 5 minutes", book.Id);
            //return CreatedAtRoute("GetBookByName", new { id = book.Id }, book);
            return Ok(book);
            //return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
        }

        [HttpGet("test-review/{bookId}")]
        public async Task<IActionResult> GetReviewStats(int bookId, [FromServices] ReviewGrpcService.ReviewGrpcServiceClient client)
        {
            try
            {
                var avgRating = await client.GetAverageRatingAsync(new BookIdRequest { BookId = bookId });
                var reviewCount = await client.GetReviewCountAsync(new BookIdRequest { BookId = bookId });

                return Ok(new
                {
                    BookId = bookId,
                    avgRating.AverageRating,
                    ReviewCount = reviewCount.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving review stats: {ex.Message}");
            }
        }

    }


}

