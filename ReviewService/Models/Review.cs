namespace ReviewService.Models
{
    public class Review
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]//auto increment
        public int Id { get; set; }
        [Required]
        public int BookId { get; set; }
        [Required]
        public string ReviewerName { get; set; } = string.Empty;
        [Required]
        public int Rating { get; set; }
        [Required]
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //createdt
    }
}