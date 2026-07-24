using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookBoard.Pages
{
    [Authorize]
    public class CreateBoardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TagService _tagService;

        public CreateBoardModel(
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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string rawTags = Board.MoodTags;

            Board.CreatedAt = DateTime.Now;
            Board.UserId = _userManager.GetUserId(User);

            _context.Boards.Add(Board);
            await _context.SaveChangesAsync();

            await _tagService.SyncBoardTagsAsync(Board.Id, rawTags);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Boards");
        }
    }
}