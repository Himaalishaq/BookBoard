using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class SavedBoard
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int BoardId { get; set; }

        public Board? Board { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}