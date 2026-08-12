using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class Board
    {
        public int Id { get; set; }

        public List<BoardVisualItem> VisualItems { get; set; } = new List<BoardVisualItem>();

        [Required]
        [StringLength(80)]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(200)]
        public string MoodTags { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public List<BoardBook> Books { get; set; } = new List<BoardBook>();

        public List<BoardTag> BoardTags { get; set; } = new List<BoardTag>();

        public List<SavedBoard> SavedByUsers { get; set; } = new List<SavedBoard>();

        [StringLength(50)]
        public string Theme { get; set; } = "cozy";

        [StringLength(30)]
        public string AccentColor { get; set; } = "brown";

        [StringLength(100)]
        public string IconSymbols { get; set; } = "📚, ☕, ✨";

        [StringLength(50)]
        public string BackgroundStyle { get; set; } = "soft-glow";
    }
}