using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class Board
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        // Kept for the form input and simple display.
        // The real tag system is BoardTags -> Tag.
        [StringLength(200)]
        public string MoodTags { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public List<BoardBook> Books { get; set; } = new List<BoardBook>();

        public List<BoardTag> BoardTags { get; set; } = new List<BoardTag>();
    }
}