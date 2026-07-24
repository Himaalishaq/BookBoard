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
    public class DeleteBookModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteBookModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public BoardBook? BoardBook { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            BoardBook = await _context.BoardBooks
                .Include(b => b.Board)
                .FirstOrDefaultAsync(b => b.Id == id && b.Board != null && b.Board.UserId == userId);

            if (BoardBook == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = _userManager.GetUserId(User);

            var book = await _context.BoardBooks
                .Include(b => b.Board)
                .FirstOrDefaultAsync(b => b.Id == id && b.Board != null && b.Board.UserId == userId);

            if (book == null)
            {
                return NotFound();
            }

            int boardId = book.BoardId;

            _context.BoardBooks.Remove(book);
            await _context.SaveChangesAsync();

            return RedirectToPage("/BoardDetails", new { id = boardId });
        }
    }
}