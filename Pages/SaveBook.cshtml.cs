using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using BookBoard.Services;

namespace BookBoard.Pages
{
    [Authorize]
    public class SaveBookModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TagService _tagService;


        public SaveBookModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            TagService tagService)
        {
            _context = context;
            _userManager = userManager;
            _tagService = tagService;
        }

        public BoardBook? SourceBook { get; set; }

        public List<Board> UserBoards { get; set; } = new List<Board>();

        [BindProperty]
        public SaveBookInput Input { get; set; } = new SaveBookInput();

        public async Task<IActionResult> OnGetAsync(int bookId)
        {
            Input.SourceBookId = bookId;

            var result = await LoadPageDataAsync(bookId);

            if (result != null)
            {
                return result;
            }

            if (SourceBook != null)
            {
                Input.MoodTags = SourceBook.MoodTags;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await LoadPageDataAsync(Input.SourceBookId);

            if (result != null)
            {
                return result;
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? userId = _userManager.GetUserId(User);

            var targetBoard = await _context.Boards
                .FirstOrDefaultAsync(board => board.Id == Input.TargetBoardId && board.UserId == userId);

            if (targetBoard == null)
            {
                return NotFound();
            }

            if (SourceBook == null)
            {
                return NotFound();
            }

            bool alreadySaved = await _context.BoardBooks.AnyAsync(book =>
                book.BoardId == targetBoard.Id &&
                book.Title == SourceBook.Title &&
                book.Author == SourceBook.Author);

            if (alreadySaved)
            {
                ModelState.AddModelError(string.Empty, "This book is already saved to that board.");
                return Page();
            }

            var savedBook = new BoardBook
            {
                Title = SourceBook.Title,
                Author = SourceBook.Author,
                CoverUrl = SourceBook.CoverUrl,
                ShortDescription = SourceBook.ShortDescription,
                PublishedYear = SourceBook.PublishedYear,
                MoodTags = Input.MoodTags,
                Reflection = Input.Reflection,
                CreatedAt = DateTime.Now,
                BoardId = targetBoard.Id
            };

            _context.BoardBooks.Add(savedBook);
            await _context.SaveChangesAsync();

            await _tagService.SyncBookTagsAsync(savedBook.Id, Input.MoodTags);
            await _context.SaveChangesAsync();

            return RedirectToPage("/BoardDetails", new { id = targetBoard.Id });
        }

        private async Task<IActionResult?> LoadPageDataAsync(int sourceBookId)
        {
            string? userId = _userManager.GetUserId(User);

            SourceBook = await _context.BoardBooks
                .Include(book => book.Board)
                .FirstOrDefaultAsync(book => book.Id == sourceBookId);

            if (SourceBook == null)
            {
                return NotFound();
            }

            if (SourceBook.Board == null)
            {
                return NotFound();
            }

            bool canViewSourceBook = SourceBook.Board.IsPublic || SourceBook.Board.UserId == userId;

            if (!canViewSourceBook)
            {
                return Forbid();
            }

            UserBoards = await _context.Boards
                .Where(board => board.UserId == userId)
                .OrderByDescending(board => board.CreatedAt)
                .ToListAsync();

            return null;
        }
    }

    public class SaveBookInput
    {
        public int SourceBookId { get; set; }

        [Required]
        public int TargetBoardId { get; set; }

        [StringLength(200)]
        public string MoodTags { get; set; } = string.Empty;

        [StringLength(500)]
        public string Reflection { get; set; } = string.Empty;
    }
}