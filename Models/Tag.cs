using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        public string Slug { get; set; } = string.Empty;

        public List<BoardTag> BoardTags { get; set; } = new List<BoardTag>();

        public List<BookTag> BookTags { get; set; } = new List<BookTag>();
    }
}