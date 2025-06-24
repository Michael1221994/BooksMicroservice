using System.ComponentModel.DataAnnotations;

namespace BookService.Dtos
{
    public class CreateBookDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Author { get; set; } = string.Empty;
        [Required]
        public string ISBN { get; set; } = string.Empty;
    }
}
