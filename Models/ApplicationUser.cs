using Microsoft.AspNetCore.Identity;

namespace BookBoard.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;

        public List<SavedBoard> SavedBoards { get; set; } = new List<SavedBoard>();
    }
}