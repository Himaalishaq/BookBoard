using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BookBoard.Pages
{
    [Authorize]
    public class CustomizeBoardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomizeBoardModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Board? Board { get; set; }

        [BindProperty]
        public CanvasTileInput Input { get; set; } = new CanvasTileInput();

        [BindProperty]
        public string? ImageUrlValue { get; set; }

        [BindProperty]
        public string? QuoteValue { get; set; }

        [BindProperty]
        public string? SymbolValue { get; set; }

        [BindProperty]
        public string? ColorValue { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await LoadBoardAsync(id);

            if (result != null)
            {
                return result;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var result = await LoadBoardAsync(id);

            if (result != null)
            {
                return result;
            }

            if (Board == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string content;

            if (Input.ItemType == "bookcover")
            {
                var selectedBook = Board.Books.FirstOrDefault(b => b.Id == Input.BookCoverId);

                if (selectedBook == null || string.IsNullOrWhiteSpace(selectedBook.CoverUrl))
                {
                    ModelState.AddModelError(string.Empty, "Choose a book with a cover image to feature.");
                    return Page();
                }

                content = selectedBook.CoverUrl;
            }
            else
            {
                content = Input.ItemType switch
                {
                    "image" => ImageUrlValue?.Trim() ?? string.Empty,
                    "text" => QuoteValue?.Trim() ?? string.Empty,
                    "icon" => SymbolValue?.Trim() ?? string.Empty,
                    "color" => ColorValue?.Trim() ?? string.Empty,
                    _ => string.Empty
                };
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError(string.Empty, "Please fill in the tile before adding it.");
                return Page();
            }

            if (content.Length > 700)
            {
                content = content.Substring(0, 700);
            }

            int nextSortOrder = 1;

            if (Board.VisualItems.Count > 0)
            {
                nextSortOrder = Board.VisualItems.Max(item => item.SortOrder) + 1;
            }

            var visualItem = new BoardVisualItem
            {
                BoardId = Board.Id,
                ItemType = Input.ItemType,
                Content = content,
                TileStyle = Input.TileStyle,
                SortOrder = nextSortOrder,
                CreatedAt = DateTime.Now
            };

            _context.BoardVisualItems.Add(visualItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("/CustomizeBoard", new { id = Board.Id });
        }

        public async Task<IActionResult> OnPostMoveAsync(int id, int itemId, string direction)
        {
            var result = await LoadBoardAsync(id);

            if (result != null)
            {
                return result;
            }

            if (Board == null)
            {
                return NotFound();
            }

            var ordered = Board.VisualItems.OrderBy(item => item.SortOrder).ToList();
            int index = ordered.FindIndex(item => item.Id == itemId);

            if (index == -1)
            {
                return NotFound();
            }

            int swapIndex = direction == "up" ? index - 1 : index + 1;

            if (swapIndex >= 0 && swapIndex < ordered.Count)
            {
                int temp = ordered[index].SortOrder;
                ordered[index].SortOrder = ordered[swapIndex].SortOrder;
                ordered[swapIndex].SortOrder = temp;

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/CustomizeBoard", new { id = Board.Id });
        }

        public async Task<IActionResult> OnPostReorderAsync(int id, [FromBody] ReorderRequest? request)
        {
            string? userId = _userManager.GetUserId(User);

            var board = await _context.Boards
                .Include(b => b.VisualItems)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null)
            {
                return NotFound();
            }

            if (request == null || request.OrderedIds == null || request.OrderedIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "No tile order was provided." });
            }

            var itemsById = board.VisualItems.ToDictionary(item => item.Id);

            int sortOrder = 1;

            foreach (int itemId in request.OrderedIds)
            {
                if (itemsById.TryGetValue(itemId, out var item))
                {
                    item.SortOrder = sortOrder;
                    sortOrder++;
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int itemId)
        {
            var result = await LoadBoardAsync(id);

            if (result != null)
            {
                return result;
            }

            if (Board == null)
            {
                return NotFound();
            }

            var item = await _context.BoardVisualItems
                .FirstOrDefaultAsync(item =>
                    item.Id == itemId &&
                    item.BoardId == Board.Id);

            if (item == null)
            {
                return NotFound();
            }

            _context.BoardVisualItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToPage("/CustomizeBoard", new { id = Board.Id });
        }

        private async Task<IActionResult?> LoadBoardAsync(int id)
        {
            string? userId = _userManager.GetUserId(User);

            Board = await _context.Boards
                .Include(board => board.VisualItems)
                .Include(board => board.Books)
                .FirstOrDefaultAsync(board =>
                    board.Id == id &&
                    board.UserId == userId);

            if (Board == null)
            {
                return NotFound();
            }

            Board.VisualItems = Board.VisualItems
                .OrderBy(item => item.SortOrder)
                .ToList();

            return null;
        }
    }

    public class CanvasTileInput
    {
        [Required]
        [StringLength(30)]
        public string ItemType { get; set; } = "image";

        [StringLength(50)]
        public string TileStyle { get; set; } = "normal";

        public int? BookCoverId { get; set; }
    }

    public class ReorderRequest
    {
        public List<int> OrderedIds { get; set; } = new List<int>();
    }
}