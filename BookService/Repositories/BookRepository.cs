using BookService.Models;

namespace BookService.Repositories
{
    public class BookRepository
    {
        private readonly List<Book> _books = new();
        public IEnumerable<Book> GetAll() => _books;

        public Book? GetById(int id) => _books.FirstOrDefault(b => b.Id == id);
        
        public void Add(Book book) => _books.Add(book);
    }
}