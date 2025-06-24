using Microsoft.AspNetCore.Mvc;
using BookService.Models;
using BookService.Repositories;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http.HttpResults;
using BookService.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;



namespace BookService.Controllers;

[ApiController]
[Route("[controller]")]
public class BookController : ControllerBase
{
    private readonly ILogger<BookController> _logger; //needed to log to see if the result is from the cache or if it's from the list
    private readonly BookRepository _repository;
    private readonly RedisCacheService _cache;

    public BookController(BookRepository repository, RedisCacheService cache, ILogger<BookController> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;

    }

    [HttpGet("getAll")]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Ok(_repository.GetAll());
    }

    [HttpGet("getByID/{id:int:min(1)}", Name = "GetBookById")]
    public async Task<ActionResult<Book>> GetById(int id)
    {
        string cacheKey = $"Book:{id}";

        var cached = await _cache.GetCachedValue(cacheKey);

        if (cached != null)
        {
            _logger.LogInformation("Book {id} found in cache", id); //when there's a cache hit it logs it 
            var cachedBook = JsonSerializer.Deserialize<Book>(cached);// turning the json into a book object so that I can return it
            return Ok(cachedBook);

        }
        //after the 5mins I set. It'll be a cache miss so I'll look for it in the list

        var book = _repository.GetById(id);//look for it in the list

        if (book == null)
        {
            _logger.LogWarning("Book {id} not found in cache or repository", id);
            return NotFound();

        }

        var json= JsonSerializer.Serialize(book); //turn the book into json
        await _cache.SetCachedValue(cacheKey, json, TimeSpan.FromMinutes(5));//store it in the cache for later
        _logger.LogInformation("Book {id} found in repository", id);
        return Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult> Post(Book book)
    {
        _repository.Add(book);
        var json = JsonSerializer.Serialize(book);//after adding the book I save it in the cache for 5 minutes
        string cachekey = $"Book:{book.Id}";
        await _cache.SetCachedValue(cachekey, json, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Book {id} added to repository and cached for the next 5 minutes", book.Id);
        return CreatedAtRoute("GetBookById", new { id = book.Id }, book);

        //return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }
}

