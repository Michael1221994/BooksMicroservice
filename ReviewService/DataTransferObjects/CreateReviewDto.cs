using System.ComponentModel.DataAnnotations;

namespace ReviewService.Dtos
{
    public class CreateReviewDto
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public string ReviewerName { get; set; } = string.Empty;
    }
}
