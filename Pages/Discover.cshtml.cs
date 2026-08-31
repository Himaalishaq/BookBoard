using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    public class DiscoverModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BoardRecommendationService _recommendations;

        public DiscoverModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            BoardRecommendationService recommendations)
        {
            _context = context;
            _userManager = userManager;
            _recommendations = recommendations;
        }

        public string SearchQuery { get; set; } = string.Empty;
        public string SelectedMood { get; set; } = string.Empty;
        public int? SimilarToBoardId { get; set; }
        public string SimilarBoardTitle { get; set; } = string.Empty;

        public string CurrentUserId { get; set; } = string.Empty;
        public HashSet<int> SavedBoardIds { get; set; } = new HashSet<int>();

        public List<Board> MatchingBoards { get; set; } = new List<Board>();
        public List<BoardBook> MatchingBooks { get; set; } = new List<BoardBook>();

        public List<Board> RecommendedBoards { get; set; } = new List<Board>();
        public List<BoardBook> RecommendedBooks { get; set; } = new List<BoardBook>();
        public List<Board> MoodBoards { get; set; } = new List<Board>();

        public List<MoodGroup> PopularMoods { get; set; } = new List<MoodGroup>();
        public List<string> SimilarMoods { get; set; } = new List<string>();
        public List<string> SuggestedTags { get; set; } = new List<string>();
        public List<BoardOption> BoardOptions { get; set; } = new List<BoardOption>();
        public List<BoardMatch> SimilarBoards { get; set; } = new List<BoardMatch>();

        public bool RecommendationsArePersonalized { get; set; }
        public List<string> TasteTags { get; set; } = new List<string>();

        public int PageNumber { get; set; } = 1;
        public int TotalBoardPages { get; set; }
        public int TotalMatchingBoards { get; set; }

        public bool HasSearched => !string.IsNullOrWhiteSpace(SearchQuery);
        public bool HasSelectedMood => !string.IsNullOrWhiteSpace(SelectedMood);

        public string? SelectedMoodSlug => HasSelectedMood
            ? TagService.ParseTags(SelectedMood).FirstOrDefault()
            : null;

        public async Task OnGetAsync(string? query, string? mood, int? similarToBoardId, int page = 1)
        {
            SearchQuery = query?.Trim() ?? string.Empty;
            SelectedMood = mood?.Trim() ?? string.Empty;
            SimilarToBoardId = similarToBoardId;
            PageNumber = page < 1 ? 1 : page;

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

            PopularMoods = await _recommendations.GetPopularMoodsAsync();

            if (HasSearched || HasSelectedMood)
            {
                var search = await _recommendations.SearchAsync(
                    HasSearched ? SearchQuery : null,
                    HasSelectedMood ? SelectedMood : null,
                    PageNumber);

                PageNumber = search.Page;
                TotalMatchingBoards = search.TotalBoards;
                TotalBoardPages = search.TotalPages;
                SimilarMoods = search.RelatedMoodSlugs;

                if (HasSearched)
                {
                    MatchingBoards = search.Boards;
                    MatchingBooks = search.Books;
                }
                else
                {
                    MoodBoards = search.Boards;
                }

                SuggestedTags = PopularMoods
                    .Select(group => group.Slug)
                    .Where(slug => !SimilarMoods.Contains(slug) && !IsCurrentFilter(slug))
                    .Take(6)
                    .ToList();
            }
            else
            {
                var feed = await _recommendations.GetPersonalizedAsync(userId);
                RecommendedBoards = feed.Boards;
                RecommendationsArePersonalized = feed.IsPersonalized;
                TasteTags = feed.TasteTags;
                RecommendedBooks = await _recommendations.GetRecentPublicBooksAsync();
            }

            if (SimilarToBoardId.HasValue)
            {
                var similar = await _recommendations.GetSimilarAsync(SimilarToBoardId.Value, userId);
                if (similar.Found && similar.CanView)
                {
                    SimilarBoardTitle = similar.BoardTitle;
                    SimilarBoards = similar.Matches;
                }
            }
        }

        private bool IsCurrentFilter(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return false;
            }

            if (SelectedMoodSlug == slug)
            {
                return true;
            }

            return SearchQuery.Contains(slug, StringComparison.OrdinalIgnoreCase) ||
                SearchQuery.Contains(TagService.ToDisplayName(slug), StringComparison.OrdinalIgnoreCase);
        }
    }
}
