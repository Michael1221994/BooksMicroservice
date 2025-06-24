using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookService.Models
{
    public class Book
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]// Tells EF Core to auto-generate the id's for me
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Author { get; set; } = string.Empty;
        [Required]
        public string ISBN { get; set; } = string.Empty;
    }
}