using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    public class BoardDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BoardDetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Board? Board { get; set; }

        public bool IsOwner { get; set; }

        public bool IsBoardSaved { get; set; }

        public int SavedCount { get; set; }

        public List<BookRecommendation> Recommendations { get; set; } = new List<BookRecommendation>();

        public List<MoodStat> BoardDna { get; set; } = new List<MoodStat>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Board = await _context.Boards
                .Include(board => board.VisualItems)
                .Include(board => board.BoardTags)
                    .ThenInclude(boardTag => boardTag.Tag)
                .Include(board => board.Books)
                    .ThenInclude(book => book.BookTags)
                        .ThenInclude(bookTag => bookTag.Tag)
                .Include(board => board.User)
                .FirstOrDefaultAsync(board => board.Id == id);

            if (Board == null)
            {
                return NotFound();
            }

            string? userId = _userManager.GetUserId(User);

            IsOwner = !string.IsNullOrWhiteSpace(userId) && Board.UserId == userId;

            if (!Board.IsPublic && !IsOwner)
            {
                return Forbid();
            }

            SavedCount = await _context.SavedBoards
                .CountAsync(saved => saved.BoardId == Board.Id);

            IsBoardSaved = !string.IsNullOrWhiteSpace(userId) &&
                await _context.SavedBoards.AnyAsync(saved =>
                    saved.UserId == userId &&
                    saved.BoardId == Board.Id);

            LoadBoardDna(Board);
            await LoadRecommendationsAsync(Board);

            return Page();
        }

        private void LoadBoardDna(Board board)
        {
            var allTags = new List<string>();

            allTags.AddRange(TagService.GetBoardTagSlugs(board));

            foreach (var book in board.Books)
            {
                allTags.AddRange(TagService.GetBookTagSlugs(book));
            }

            if (allTags.Count == 0)
            {
                BoardDna = new List<MoodStat>();
                return;
            }

            int totalTags = allTags.Count;

            BoardDna = allTags
                .GroupBy(tag => tag)
                .Select(group => new MoodStat
                {
                    Mood = TagService.ToDisplayName(group.Key),
                    Count = group.Count(),
                    Percentage = (int)Math.Round((group.Count() / (double)totalTags) * 100)
                })
                .OrderByDescending(stat => stat.Count)
                .ThenBy(stat => stat.Mood)
                .Take(6)
                .ToList();
        }

        private async Task LoadRecommendationsAsync(Board board)
        {
            var boardTags = TagService.GetBoardTagSlugs(board);

            var bookTagsFromThisBoard = board.Books
                .SelectMany(book => TagService.GetBookTagSlugs(book))
                .ToList();

            var targetTags = boardTags
                .Concat(bookTagsFromThisBoard)
                .Distinct()
                .ToList();

            if (targetTags.Count == 0)
            {
                Recommendations = new List<BookRecommendation>();
                return;
            }

            var otherBooks = await _context.BoardBooks
                .Include(book => book.BookTags)
                    .ThenInclude(bookTag => bookTag.Tag)
                .Include(book => book.Board)
                    .ThenInclude(board => board!.BoardTags)
                        .ThenInclude(boardTag => boardTag.Tag)
                .Where(book =>
                    book.BoardId != board.Id &&
                    book.Board != null &&
                    book.Board.IsPublic)
                .ToListAsync();

            Recommendations = otherBooks
                .Select(book =>
                {
                    var bookTags = TagService.GetBookTagSlugs(book)
                        .Concat(book.Board != null
                            ? TagService.GetBoardTagSlugs(book.Board)
                            : new List<string>())
                        .Distinct()
                        .ToList();

                    var matchedTags = bookTags
                        .Where(tag => targetTags.Contains(tag))
                        .Select(TagService.ToDisplayName)
                        .ToList();

                    return new BookRecommendation
                    {
                        Book = book,
                        MatchedTags = matchedTags,
                        Score = matchedTags.Count
                    };
                })
                .Where(result => result.Score > 0)
                .OrderByDescending(result => result.Score)
                .ThenByDescending(result => result.Book.CreatedAt)
                .Take(6)
                .ToList();
        }
    }

    public class BookRecommendation
    {
        public BoardBook Book { get; set; } = new BoardBook();

        public List<string> MatchedTags { get; set; } = new List<string>();

        public int Score { get; set; }
    }

    public class MoodStat
    {
        public string Mood { get; set; } = string.Empty;

        public int Count { get; set; }

        public int Percentage { get; set; }
    }
}