using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    [Authorize]
    public class EditBoardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TagService _tagService;

        public EditBoardModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            TagService tagService)
        {
            _context = context;
            _userManager = userManager;
            _tagService = tagService;
        }

        [BindProperty]
        public Board Board { get; set; } = new Board();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var board = await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null)
            {
                return NotFound();
            }

            Board = board;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var boardToUpdate = await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (boardToUpdate == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                Board.Id = id;
                return Page();
            }

            string rawTags = Board.MoodTags;

            boardToUpdate.Title = Board.Title;
            boardToUpdate.Description = Board.Description;
            boardToUpdate.MoodTags = Board.MoodTags;
            boardToUpdate.IsPublic = Board.IsPublic;

            boardToUpdate.Theme = Board.Theme;
            boardToUpdate.BackgroundStyle = Board.BackgroundStyle;
            boardToUpdate.AccentColor = Board.AccentColor;
            boardToUpdate.IconSymbols = Board.IconSymbols;

            await _tagService.SyncBoardTagsAsync(boardToUpdate.Id, rawTags);
            await _context.SaveChangesAsync();

            return RedirectToPage("/BoardDetails", new { id = boardToUpdate.Id });
        }
    }
}