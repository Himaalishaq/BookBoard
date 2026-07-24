using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class BoardBook
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [StringLength(300)]
        public string CoverUrl { get; set; } = string.Empty;

        [StringLength(600)]
        public string ShortDescription { get; set; } = string.Empty;

        public int? PublishedYear { get; set; }

        // Kept for the form input and simple display.
        // The real tag system is BookTags -> Tag.
        [StringLength(200)]
        public string MoodTags { get; set; } = string.Empty;

        [StringLength(500)]
        public string Reflection { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int BoardId { get; set; }

        public Board? Board { get; set; }

        public List<BookTag> BookTags { get; set; } = new List<BookTag>();
    }
}