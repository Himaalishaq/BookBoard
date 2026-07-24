using BookBoard.Data;
using BookBoard.Models;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Services
{
    public class TagService
    {
        private readonly ApplicationDbContext _context;

        public TagService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SyncBoardTagsAsync(int boardId, string rawTags)
        {
            var board = await _context.Boards
                .Include(board => board.BoardTags)
                .FirstOrDefaultAsync(board => board.Id == boardId);

            if (board == null)
            {
                return;
            }

            var parsedTags = ParseTags(rawTags);

            board.MoodTags = string.Join(", ", parsedTags.Select(tag => ToDisplayName(tag)));

            _context.BoardTags.RemoveRange(board.BoardTags);

            foreach (string slug in parsedTags)
            {
                var tag = await GetOrCreateTagAsync(slug);

                board.BoardTags.Add(new BoardTag
                {
                    BoardId = board.Id,
                    TagId = tag.Id
                });
            }
        }

        public async Task SyncBookTagsAsync(int bookId, string rawTags)
        {
            var book = await _context.BoardBooks
                .Include(book => book.BookTags)
                .FirstOrDefaultAsync(book => book.Id == bookId);

            if (book == null)
            {
                return;
            }

            var parsedTags = ParseTags(rawTags);

            book.MoodTags = string.Join(", ", parsedTags.Select(tag => ToDisplayName(tag)));

            _context.BookTags.RemoveRange(book.BookTags);

            foreach (string slug in parsedTags)
            {
                var tag = await GetOrCreateTagAsync(slug);

                book.BookTags.Add(new BookTag
                {
                    BoardBookId = book.Id,
                    TagId = tag.Id
                });
            }
        }

        public static List<string> GetBoardTagSlugs(Board board)
        {
            var relationalTags = board.BoardTags
                .Where(boardTag => boardTag.Tag != null)
                .Select(boardTag => boardTag.Tag!.Slug)
                .ToList();

            if (relationalTags.Count > 0)
            {
                return relationalTags;
            }

            return ParseTags(board.MoodTags);
        }

        public static List<string> GetBookTagSlugs(BoardBook book)
        {
            var relationalTags = book.BookTags
                .Where(bookTag => bookTag.Tag != null)
                .Select(bookTag => bookTag.Tag!.Slug)
                .ToList();

            if (relationalTags.Count > 0)
            {
                return relationalTags;
            }

            return ParseTags(book.MoodTags);
        }

        public static List<string> ParseTags(string? rawTags)
        {
            if (string.IsNullOrWhiteSpace(rawTags))
            {
                return new List<string>();
            }

            return rawTags
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeToSlug)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct()
                .ToList();
        }

        public static string ToDisplayName(string slug)
        {
            return string.Join(" ",
                slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
        }

        private async Task<Tag> GetOrCreateTagAsync(string slug)
        {
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(tag => tag.Slug == slug);

            if (existingTag != null)
            {
                return existingTag;
            }

            var tag = new Tag
            {
                Slug = slug,
                Name = ToDisplayName(slug)
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return tag;
        }

        private static string NormalizeToSlug(string tag)
        {
            tag = tag.Trim().ToLower();

            while (tag.Contains("  "))
            {
                tag = tag.Replace("  ", " ");
            }

            tag = tag.Replace(" ", "-");

            return tag;
        }
    }
}