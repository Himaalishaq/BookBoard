namespace BookBoard.Models
{
    public class MoodGroup
    {
        public string Mood { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class BoardOption
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
    }

    public class BoardMatch
    {
        public Board Board { get; set; } = new Board();

        public List<string> MatchedTags { get; set; } = new List<string>();

        public int Score { get; set; }
    }

    public class BoardFeed
    {
        public List<Board> Boards { get; set; } = new List<Board>();

        public bool IsPersonalized { get; set; }

        public List<string> TasteTags { get; set; } = new List<string>();
    }

    public class BoardSearchPage
    {
        public List<Board> Boards { get; set; } = new List<Board>();

        public List<BoardBook> Books { get; set; } = new List<BoardBook>();

        public List<string> RelatedMoodSlugs { get; set; } = new List<string>();

        public int TotalBoards { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalBoards / (double)PageSize);
    }

    public class SimilarBoardsResult
    {
        public bool Found { get; set; }

        public bool CanView { get; set; }

        public int BoardId { get; set; }

        public string BoardTitle { get; set; } = string.Empty;

        public List<BoardMatch> Matches { get; set; } = new List<BoardMatch>();
    }
}
