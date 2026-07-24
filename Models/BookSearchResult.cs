namespace BookBoard.Models
{
    public class BookSearchResult
    {
        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string CoverUrl { get; set; } = string.Empty;

        public string ShortDescription { get; set; } = string.Empty;

        public int? PublishedYear { get; set; }
    }
}