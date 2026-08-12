using System.ComponentModel.DataAnnotations;

namespace BookBoard.Models
{
    public class BoardVisualItem
    {
        public int Id { get; set; }

        public int BoardId { get; set; }

        public Board? Board { get; set; }

        [Required]
        [StringLength(30)]
        public string ItemType { get; set; } = "text";
        // image, text, icon, color

        [Required]
        [StringLength(700)]
        public string Content { get; set; } = string.Empty;
        // image URL, quote text, emoji, or color code

        [StringLength(50)]
        public string TileStyle { get; set; } = "normal";
        // normal, tall, wide, soft, glow, dark, light

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}