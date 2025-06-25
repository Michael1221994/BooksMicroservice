using BookService.Data;
using BookService.Models;

namespace BookService.Repositories
{
    public class BookRepository
    {
        private readonly BookDbContext _context;  

        public BookRepository(BookDbContext context)
        {
        _context = context;
        }
        public IEnumerable<Book> GetAll() => _context.Books.ToList();

        public Book? GetByName(string Name) => _context.Books.FirstOrDefault(b => b.Title == Name);

        public void Add(Book book)
        {
            _context.Books.Add(book);
            _context.SaveChanges();    
        } 
    }
}