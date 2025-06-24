using BookService.Models;

namespace BookService.Repositories
{
    public class BookRepository
    {
        private static readonly List<Book> _books = new(){new Book{Id = 1, Title = "Book 1", Author = "Author 1"}, new Book{Id = 2, Title = "Book 2", Author = "Author 2"}};
        public IEnumerable<Book> GetAll() => _books;

        public Book? GetById(int id) => _books.FirstOrDefault(b => b.Id == id);
        
        public void Add(Book book) => _books.Add(book);
    }
}