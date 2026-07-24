using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookBoard.Services;



namespace BookBoard.Pages
{
    [Authorize]
    public class AddBookModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OpenLibraryService _openLibraryService;
        private readonly TagService _tagService;

        public AddBookModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            OpenLibraryService openLibraryService,
            TagService tagService)
        {
            _context = context;
            _userManager = userManager;
            _openLibraryService = openLibraryService;
            _tagService = tagService;
        }


        [BindProperty]
        public BoardBook BoardBook { get; set; } = new BoardBook();

        public string BoardTitle { get; set; } = string.Empty;

        public string SearchQuery { get; set; } = string.Empty;

        public List<BookSearchResult> SearchResults { get; set; } = new List<BookSearchResult>();

        public async Task<IActionResult> OnGetAsync(int boardId, string? searchQuery)
        {
            var userId = _userManager.GetUserId(User);

            var board = await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == boardId && b.UserId == userId);

            if (board == null)
            {
                return NotFound();
            }

            BoardTitle = board.Title;
            BoardBook.BoardId = board.Id;

            SearchQuery = searchQuery?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults = await _openLibraryService.SearchBooksAsync(SearchQuery);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            var board = await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == BoardBook.BoardId && b.UserId == userId);

            if (board == null)
            {
                return NotFound();
            }

            BoardTitle = board.Title;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            BoardBook.CreatedAt = DateTime.Now;

            string rawTags = BoardBook.MoodTags;

            BoardBook.CreatedAt = DateTime.Now;

            _context.BoardBooks.Add(BoardBook);
            await _context.SaveChangesAsync();

            await _tagService.SyncBookTagsAsync(BoardBook.Id, rawTags);
            await _context.SaveChangesAsync();

            return RedirectToPage("/BoardDetails", new { id = BoardBook.BoardId });
        }
    }
}