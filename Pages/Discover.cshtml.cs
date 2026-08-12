using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    public class DiscoverModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DiscoverModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string SearchQuery { get; set; } = string.Empty;
        public string SelectedMood { get; set; } = string.Empty;
        public int? SimilarToBoardId { get; set; }
        public string SimilarBoardTitle { get; set; } = string.Empty;

        public string CurrentUserId { get; set; } = string.Empty;
        public HashSet<int> SavedBoardIds { get; set; } = new HashSet<int>();

        public List<Board> MatchingBoards { get; set; } = new();
        public List<BoardBook> MatchingBooks { get; set; } = new();

        public List<Board> RecommendedBoards { get; set; } = new();
        public List<BoardBook> RecommendedBooks { get; set; } = new();
        public List<Board> MoodBoards { get; set; } = new();

        public List<MoodGroup> PopularMoods { get; set; } = new();
        public List<string> SimilarMoods { get; set; } = new();
        public List<string> SuggestedTags { get; set; } = new();
        public List<BoardOption> BoardOptions { get; set; } = new();
        public List<BoardMatch> SimilarBoards { get; set; } = new();

        public bool HasSearched => !string.IsNullOrWhiteSpace(SearchQuery);
        public bool HasSelectedMood => !string.IsNullOrWhiteSpace(SelectedMood);

        public async Task OnGetAsync(string? query, string? mood, int? similarToBoardId)
        {
            SearchQuery = query?.Trim() ?? string.Empty;
            SelectedMood = mood?.Trim() ?? string.Empty;
            SimilarToBoardId = similarToBoardId;

            string? userId = _userManager.GetUserId(User);
            CurrentUserId = userId ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                SavedBoardIds = await _context.SavedBoards
                    .Where(saved => saved.UserId == userId)
                    .Select(saved => saved.BoardId)
                    .ToHashSetAsync();

                BoardOptions = await _context.Boards
                    .Where(board => board.UserId == userId)
                    .OrderBy(board => board.Title)
                    .Select(board => new BoardOption
                    {
                        Id = board.Id,
                        Title = board.Title
                    })
                    .ToListAsync();
            }

            var allPublicBoards = await _context.Boards
                .Where(board => board.IsPublic)
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .OrderByDescending(board => board.CreatedAt)
                .ToListAsync();

            var allPublicBooks = allPublicBoards
                .SelectMany(board => board.Books.Select(book =>
                {
                    book.Board = board;
                    return book;
                }))
                .OrderByDescending(book => book.CreatedAt)
                .ToList();

            RecommendedBoards = allPublicBoards
                .Take(24)
                .ToList();

            RecommendedBooks = allPublicBooks
                .Take(12)
                .ToList();

            PopularMoods = BuildPopularMoods(allPublicBoards);

            if (HasSelectedMood)
            {
                MoodBoards = FilterBoardsByMood(allPublicBoards, SelectedMood)
                    .Take(12)
                    .ToList();
            }

            if (HasSearched)
            {
                string loweredQuery = SearchQuery.ToLower();

                MatchingBoards = allPublicBoards
                    .Where(board => BoardContainsText(board, loweredQuery))
                    .ToList();

                MatchingBooks = allPublicBooks
                    .Where(book => BookContainsText(book, loweredQuery))
                    .ToList();

                SimilarMoods = BuildRelatedMoods(MatchingBoards, MatchingBooks, loweredQuery);

                SuggestedTags = PopularMoods
                    .Select(group => group.Mood)
                    .Where(tag => !SimilarMoods.Contains(tag) && !loweredQuery.Contains(tag))
                    .Take(6)
                    .ToList();
            }

            if (SimilarToBoardId.HasValue)
            {
                var selectedBoard = await _context.Boards
                    .Include(board => board.Books)
                    .FirstOrDefaultAsync(board =>
                        board.Id == SimilarToBoardId.Value &&
                        board.UserId == userId);

                if (selectedBoard != null)
                {
                    SimilarBoardTitle = selectedBoard.Title;
                    SimilarBoards = FindSimilarBoards(selectedBoard, allPublicBoards);
                }
            }
        }

        private static List<Board> FilterBoardsByMood(List<Board> boards, string mood)
        {
            string loweredMood = mood.ToLower();

            return boards
                .Where(board => BoardContainsText(board, loweredMood))
                .OrderByDescending(board => board.Books.Count)
                .ThenByDescending(board => board.CreatedAt)
                .ToList();
        }

        private static bool BoardContainsText(Board board, string text)
        {
            return TextContains(board.Title, text)
                || TextContains(board.Description, text)
                || TextContains(board.MoodTags, text)
                || TextContains(board.Theme, text)
                || TextContains(board.BackgroundStyle, text)
                || board.Books.Any(book => BookContainsText(book, text));
        }

        private static bool BookContainsText(BoardBook book, string text)
        {
            return TextContains(book.Title, text)
                || TextContains(book.Author, text)
                || TextContains(book.MoodTags, text)
                || TextContains(book.Reflection, text)
                || TextContains(book.ShortDescription, text)
                || TextContains(book.Board?.Title, text)
                || TextContains(book.Board?.Description, text)
                || TextContains(book.Board?.MoodTags, text);
        }

        private static bool TextContains(string? value, string text)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.ToLower().Contains(text);
        }

        private static List<MoodGroup> BuildPopularMoods(List<Board> boards)
        {
            var allTags = new List<string>();

            foreach (var board in boards)
            {
                allTags.AddRange(ParseTags(board.MoodTags));

                foreach (var book in board.Books)
                {
                    allTags.AddRange(ParseTags(book.MoodTags));
                }
            }

            return allTags
                .GroupBy(tag => tag)
                .Select(group => new MoodGroup
                {
                    Mood = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Mood)
                .Take(10)
                .ToList();
        }

        private static List<BoardMatch> FindSimilarBoards(Board selectedBoard, List<Board> publicBoards)
        {
            var selectedTags = ParseTags(selectedBoard.MoodTags)
                .Concat(selectedBoard.Books.SelectMany(book => ParseTags(book.MoodTags)))
                .Distinct()
                .ToList();

            if (selectedTags.Count == 0)
            {
                return new List<BoardMatch>();
            }

            return publicBoards
                .Where(board => board.Id != selectedBoard.Id)
                .Select(board =>
                {
                    var boardTags = ParseTags(board.MoodTags)
                        .Concat(board.Books.SelectMany(book => ParseTags(book.MoodTags)))
                        .Distinct()
                        .ToList();

                    var matchedTags = boardTags
                        .Where(tag => selectedTags.Contains(tag))
                        .ToList();

                    return new BoardMatch
                    {
                        Board = board,
                        MatchedTags = matchedTags,
                        Score = matchedTags.Count
                    };
                })
                .Where(match => match.Score > 0)
                .OrderByDescending(match => match.Score)
                .ThenByDescending(match => match.Board.Books.Count)
                .Take(6)
                .ToList();
        }

        private static List<string> BuildRelatedMoods(List<Board> matchingBoards, List<BoardBook> matchingBooks, string loweredQuery)
        {
            var tagCounts = new Dictionary<string, int>();

            void AddTags(IEnumerable<string> tags)
            {
                foreach (var tag in tags)
                {
                    if (tag.Length < 2 || loweredQuery.Contains(tag))
                    {
                        continue;
                    }

                    tagCounts[tag] = tagCounts.TryGetValue(tag, out int count) ? count + 1 : 1;
                }
            }

            foreach (var board in matchingBoards)
            {
                AddTags(ParseTags(board.MoodTags));

                foreach (var book in board.Books)
                {
                    AddTags(ParseTags(book.MoodTags));
                }
            }

            foreach (var book in matchingBooks)
            {
                AddTags(ParseTags(book.MoodTags));

                if (book.Board != null)
                {
                    AddTags(ParseTags(book.Board.MoodTags));
                }
            }

            return tagCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Select(pair => pair.Key)
                .Take(8)
                .ToList();
        }

        private static List<string> ParseTags(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return new List<string>();
            }

            return tags
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.ToLower())
                .Distinct()
                .ToList();
        }
    }

    public class MoodGroup
    {
        public string Mood { get; set; } = string.Empty;
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
        public List<string> MatchedTags { get; set; } = new();
        public int Score { get; set; }
    }
}