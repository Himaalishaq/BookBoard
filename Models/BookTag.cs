namespace BookBoard.Models
{
    public class BookTag
    {
        public int BoardBookId { get; set; }

        public BoardBook? BoardBook { get; set; }

        public int TagId { get; set; }

        public Tag? Tag { get; set; }
    }
}