using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser? CurrentUser { get; set; }

        public string CurrentUserId { get; set; } = string.Empty;

        public List<Board> UserBoards { get; set; } = new List<Board>();

        public List<Board> SavedBoards { get; set; } = new List<Board>();

        public HashSet<int> SavedBoardIds { get; set; } = new HashSet<int>();

        public List<BoardBook> SavedBooks { get; set; } = new List<BoardBook>();

        public List<ProfileMoodStat> FavoriteMoods { get; set; } = new List<ProfileMoodStat>();

        public List<ProfileActivityItem> RecentActivity { get; set; } = new List<ProfileActivityItem>();

        public string TasteProfileTitle { get; set; } = "Mood Curator";

        public string TasteProfileDescription { get; set; } =
            "You are building a reading identity through the moods, reflections, and boards you create.";

        public int PublicBoardCount { get; set; }

        public int PrivateBoardCount { get; set; }

        public async Task OnGetAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            CurrentUserId = userId;
            CurrentUser = await _userManager.GetUserAsync(User);

            UserBoards = await _context.Boards
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .Where(board => board.UserId == userId)
                .OrderByDescending(board => board.CreatedAt)
                .ToListAsync();

            var savedBoardRows = await _context.SavedBoards
                .Include(saved => saved.Board)
                    .ThenInclude(board => board!.Books)
                .Include(saved => saved.Board)
                    .ThenInclude(board => board!.VisualItems)
                .Where(saved => saved.UserId == userId && saved.Board != null)
                .OrderByDescending(saved => saved.SavedAt)
                .ToListAsync();

            SavedBoards = savedBoardRows
                .Where(saved => saved.Board != null)
                .Select(saved => saved.Board!)
                .ToList();

            SavedBoardIds = SavedBoards
                .Select(board => board.Id)
                .ToHashSet();

            SavedBooks = UserBoards
                .SelectMany(board => board.Books.Select(book =>
                {
                    book.Board = board;
                    return book;
                }))
                .OrderByDescending(book => book.CreatedAt)
                .ToList();

            PublicBoardCount = UserBoards.Count(board => board.IsPublic);
            PrivateBoardCount = UserBoards.Count(board => !board.IsPublic);

            BuildFavoriteMoods();
            BuildTasteProfile();
            BuildRecentActivity();
        }

        private void BuildFavoriteMoods()
        {
            var allTags = new List<string>();

            foreach (var board in UserBoards)
            {
                allTags.AddRange(ParseTags(board.MoodTags));

                foreach (var book in board.Books)
                {
                    allTags.AddRange(ParseTags(book.MoodTags));
                }
            }

            FavoriteMoods = allTags
                .GroupBy(tag => tag)
                .Select(group => new ProfileMoodStat
                {
                    Mood = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(mood => mood.Count)
                .ThenBy(mood => mood.Mood)
                .Take(8)
                .ToList();
        }

        private void BuildTasteProfile()
        {
            var topMoods = FavoriteMoods
                .Select(mood => mood.Mood)
                .ToList();

            bool HasMood(params string[] moods)
            {
                return topMoods.Any(topMood =>
                    moods.Any(mood => topMood.Contains(mood)));
            }

            if (HasMood("spiritual", "religious", "faith", "healing", "awakening"))
            {
                TasteProfileTitle = "Spiritual Growth Reader";
                TasteProfileDescription =
                    "You seem drawn to books about meaning, reflection, healing, faith, and personal transformation.";
            }
            else if (HasMood("reflective", "emotional", "sad", "hopeful", "comfort"))
            {
                TasteProfileTitle = "Reflective Comfort Reader";
                TasteProfileDescription =
                    "You gravitate toward books that feel emotional, thoughtful, comforting, and personal.";
            }
            else if (HasMood("dark academia", "academic", "gloomy", "mysterious", "classic"))
            {
                TasteProfileTitle = "Atmospheric Academia Reader";
                TasteProfileDescription =
                    "You enjoy books with atmosphere, mystery, intellectual energy, and darker cozy aesthetics.";
            }
            else if (HasMood("fantasy", "escape", "adventure", "magical"))
            {
                TasteProfileTitle = "Fantasy Escape Reader";
                TasteProfileDescription =
                    "You seem to love immersive worlds, imagination, adventure, and books that feel like an escape.";
            }
            else if (HasMood("cozy", "warm", "soft", "romance", "summer"))
            {
                TasteProfileTitle = "Cozy Mood Reader";
                TasteProfileDescription =
                    "You are building a soft, warm, aesthetic reading taste centered around comfort and mood.";
            }
            else
            {
                TasteProfileTitle = "Mood Curator";
                TasteProfileDescription =
                    "Your reading taste is still forming. Add more books and mood tags to unlock a stronger profile.";
            }
        }

        private void BuildRecentActivity()
        {
            var boardActivities = UserBoards
                .Select(board => new ProfileActivityItem
                {
                    Type = "Board",
                    Title = board.Title,
                    Description = "Created a new reading board.",
                    CreatedAt = board.CreatedAt,
                    BoardId = board.Id
                });

            var bookActivities = SavedBooks
                .Select(book => new ProfileActivityItem
                {
                    Type = "Book",
                    Title = book.Title,
                    Description = book.Board != null
                        ? $"Saved to {book.Board.Title}."
                        : "Saved a book.",
                    CreatedAt = book.CreatedAt,
                    BoardId = book.BoardId
                });

            RecentActivity = boardActivities
                .Concat(bookActivities)
                .OrderByDescending(activity => activity.CreatedAt)
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
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct()
                .ToList();
        }
    }

    public class ProfileMoodStat
    {
        public string Mood { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class ProfileActivityItem
    {
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int BoardId { get; set; }
    }
}