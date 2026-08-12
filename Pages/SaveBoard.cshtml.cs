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
    public class SaveBoardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SaveBoardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Board? Board { get; set; }

        public bool IsSaved { get; set; }

        public string ReturnUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int boardId, string? returnUrl)
        {
            ReturnUrl = returnUrl ?? Url.Page("/BoardDetails", new { id = boardId }) ?? "/";

            var result = await LoadBoardAsync(boardId);

            if (result != null)
            {
                return result;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int boardId, string? returnUrl)
        {
            var result = await LoadBoardAsync(boardId);

            if (result != null)
            {
                return result;
            }

            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (Board == null)
            {
                return NotFound();
            }

            if (Board.UserId == userId)
            {
                return RedirectSafely(returnUrl, Board.Id);
            }

            var existingSave = await _context.SavedBoards
                .FirstOrDefaultAsync(saved =>
                    saved.UserId == userId &&
                    saved.BoardId == Board.Id);

            if (existingSave == null)
            {
                var savedBoard = new SavedBoard
                {
                    UserId = userId,
                    BoardId = Board.Id,
                    SavedAt = DateTime.Now
                };

                _context.SavedBoards.Add(savedBoard);
            }
            else
            {
                _context.SavedBoards.Remove(existingSave);
            }

            await _context.SaveChangesAsync();

            return RedirectSafely(returnUrl, Board.Id);
        }

        private async Task<IActionResult?> LoadBoardAsync(int boardId)
        {
            string? userId = _userManager.GetUserId(User);

            Board = await _context.Boards
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .FirstOrDefaultAsync(board => board.Id == boardId);

            if (Board == null)
            {
                return NotFound();
            }

            bool canView = Board.IsPublic || Board.UserId == userId;

            if (!canView)
            {
                return Forbid();
            }

            IsSaved = !string.IsNullOrWhiteSpace(userId) &&
                await _context.SavedBoards.AnyAsync(saved =>
                    saved.UserId == userId &&
                    saved.BoardId == Board.Id);

            return null;
        }

        private IActionResult RedirectSafely(string? returnUrl, int boardId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/BoardDetails", new { id = boardId });
        }
    }
}