using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookBoard.Services;

namespace BookBoard.Pages
{
    [Authorize]
    public class EditBookModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly TagService _tagService;

        public EditBookModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            TagService tagService)
        {
            _context = context;
            _userManager = userManager;
            _tagService = tagService;
}

        [BindProperty]
        public BoardBook BoardBook { get; set; } = new BoardBook();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var book = await _context.BoardBooks
                .Include(b => b.Board)
                .FirstOrDefaultAsync(b => b.Id == id && b.Board != null && b.Board.UserId == userId);

            if (book == null)
            {
                return NotFound();
            }

            BoardBook = book;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var bookToUpdate = await _context.BoardBooks
                .Include(b => b.Board)
                .FirstOrDefaultAsync(b => b.Id == id && b.Board != null && b.Board.UserId == userId);

            if (bookToUpdate == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }
            string rawTags = BoardBook.MoodTags;

            bookToUpdate.Title = BoardBook.Title;
            bookToUpdate.Author = BoardBook.Author;
            bookToUpdate.CoverUrl = BoardBook.CoverUrl;
            bookToUpdate.ShortDescription = BoardBook.ShortDescription;
            bookToUpdate.PublishedYear = BoardBook.PublishedYear;
            bookToUpdate.MoodTags = BoardBook.MoodTags;
            bookToUpdate.Reflection = BoardBook.Reflection;

            await _tagService.SyncBookTagsAsync(bookToUpdate.Id, rawTags);
            await _context.SaveChangesAsync();

            return RedirectToPage("/BoardDetails", new { id = bookToUpdate.BoardId });


        }
    }
}