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

        public Book? GetById(int id) => _context.Books.Find(id);

        public void Add(Book book)
        {
            _context.Books.Add(book);
            _context.SaveChanges();    
        } 
    }
}