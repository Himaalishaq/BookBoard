using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    [Authorize]
    public class DeleteBoardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteBoardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Board? Board { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            Board = await _context.Boards
                .Include(b => b.Books)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (Board == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var board = await _context.Boards
                .Include(b => b.Books)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null)
            {
                return NotFound();
            }

            _context.BoardBooks.RemoveRange(board.Books);
            _context.Boards.Remove(board);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Boards");
        }
    }
}