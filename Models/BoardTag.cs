namespace BookBoard.Models
{
    public class BoardTag
    {
        public int BoardId { get; set; }

        public Board? Board { get; set; }

        public int TagId { get; set; }

        public Tag? Tag { get; set; }
    }
}