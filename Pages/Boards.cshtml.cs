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

        public string CurrentUserId { get; set; } = string.Empty;

        public HashSet<int> SavedBoardIds { get; set; } = new HashSet<int>();

        public async Task OnGetAsync()
        {
            string? userId = _userManager.GetUserId(User);

            CurrentUserId = userId ?? string.Empty;

            Boards = await _context.Boards
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .Where(board => board.UserId == userId)
                .OrderByDescending(board => board.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                SavedBoardIds = await _context.SavedBoards
                    .Where(saved => saved.UserId == userId)
                    .Select(saved => saved.BoardId)
                    .ToHashSetAsync();
            }
        }
    }
}