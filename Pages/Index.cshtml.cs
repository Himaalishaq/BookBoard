using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Pages;

public class IndexModel : PageModel
{
    private const int FeaturedCount = 7;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Board? HeroBoard { get; set; }

    public List<Board> FeaturedBoards { get; set; } = new List<Board>();

    public CanvaCollageModel SampleCollage { get; set; } = CanvaCollageModel.Sample();

    public string CurrentUserId { get; set; } = string.Empty;

    public HashSet<int> SavedBoardIds { get; set; } = new HashSet<int>();

    public async Task OnGetAsync()
    {
        string? userId = _userManager.GetUserId(User);
        CurrentUserId = userId ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            SavedBoardIds = await _context.SavedBoards
                .Where(saved => saved.UserId == userId)
                .Select(saved => saved.BoardId)
                .ToHashSetAsync();
        }

        var publicBoards = await _context.Boards
            .Where(board => board.IsPublic)
            .Include(board => board.Books)
            .Include(board => board.VisualItems)
            .Include(board => board.User)
            .AsNoTracking()
            .ToListAsync();

        var ranked = publicBoards
            .OrderByDescending(board => board.VisualItems.Count)
            .ThenByDescending(board => board.Books.Count)
            .ThenByDescending(board => board.CreatedAt)
            .Take(FeaturedCount)
            .ToList();

        HeroBoard = ranked.FirstOrDefault();
        FeaturedBoards = ranked.Skip(HeroBoard == null ? 0 : 1).ToList();
    }
}
