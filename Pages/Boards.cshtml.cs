using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages
{
    [Authorize]
    public class BoardsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BoardsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Board> Boards { get; set; } = new List<Board>();

        public async Task OnGetAsync()
        {
            string? userId = _userManager.GetUserId(User);

            Boards = await _context.Boards
                .Include(board => board.Books)
                .Where(board => board.UserId == userId)
                .OrderByDescending(board => board.CreatedAt)
                .ToListAsync();
        }
    }
}