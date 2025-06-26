using Microsoft.AspNetCore.Mvc;
using BookService.Models;
using BookService.Repositories;
using BookService.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using BookService.Dtos;

namespace BookService.Controllers
{
    [ApiVersion("2.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/Book")] 
    public class BookV2Controller : ControllerBase
    {
        private readonly ILogger<BookController> _logger;
        private readonly BookRepository _repository;
        private readonly RedisCacheService _cache;

        public BookV2Controller(BookRepository repository, RedisCacheService cache, ILogger<BookController> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("getAll")] //http://localhost:5294/api/v2/Book/getAll
        public ActionResult<IEnumerable<Book>> GetAllV2()
        {
            var books = _repository.GetAll();
            var sorted = books.OrderBy(b => b.Title); // Changed logic: sorted by Title
            return Ok(sorted);
        }

        [HttpGet("getByName/{Name:}", Name = "GetBookByNameV2")]//http://localhost:5294/api/v2/Book/getByName/Animal%20Farm
        public async Task<ActionResult<Book>> GetByNameV2(string Name)
        {
            string cacheKey = $"BookV2:{Name}";

            var cached = await _cache.GetCachedValue(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Book {id} found in v2 cache", Name);
                var cachedBook = JsonSerializer.Deserialize<Book>(cached);
                return Ok(cachedBook);
            }

            var book = _repository.GetByName(Name);
            if (book == null)
            {
                _logger.LogWarning("Book {id} not found in cache or repo", Name);
                return NotFound();
            }

            var json = JsonSerializer.Serialize(book);
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(10)); // Different cache duration
            _logger.LogInformation("Book {id} found in v2 repo and cached", Name);

            return Ok(book);
        }

        [HttpPost]
        public async Task<ActionResult> AddBookV2(CreateBookDto bookDto)
        {
            if (string.IsNullOrWhiteSpace(bookDto.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(bookDto.Author))
                return BadRequest("Author is required.");
            if (string.IsNullOrWhiteSpace(bookDto.ISBN))
                return BadRequest("ISBN is required.");

            var book = new Book
            {
                Title = bookDto.Title.Trim(),
                Author = bookDto.Author.Trim(),
                ISBN = bookDto.ISBN.Trim().ToUpper() // Minor change in how ISBN is stored
            };

            _repository.Add(book);
            var json = JsonSerializer.Serialize(book);
            string cacheKey = $"BookV2:{book.Id}";
            await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(10));

            _logger.LogInformation("Book {id} added via v2", book.Id);
            return Ok(book);
        }


        [HttpGet("addBook")]//http://localhost:5294/api/v2/Book/addBook
        public  ActionResult V2()
        {
            return Ok("This endpoint exists only in API version 2.");
        }
    }
}
