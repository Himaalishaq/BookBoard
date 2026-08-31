using BookBoard.Data;
using BookBoard.Models;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Services
{
    public class BoardRecommendationService
    {
        public const int DefaultPageSize = 12;
        public const int DefaultFeedSize = 24;
        public const int DefaultSimilarCount = 6;
        public const int DefaultBookCount = 12;

        private readonly ApplicationDbContext _context;

        public BoardRecommendationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BoardFeed> GetPersonalizedAsync(
            string? userId,
            int take = DefaultFeedSize,
            CancellationToken cancellationToken = default)
        {
            var excludeIds = new List<int>();
            var tasteSlugs = new List<string>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                excludeIds = await _context.Boards
                    .Where(board => board.UserId == userId)
                    .Select(board => board.Id)
                    .ToListAsync(cancellationToken);

                tasteSlugs = await CollectUserTasteSlugsAsync(userId, excludeIds, cancellationToken);
            }

            if (tasteSlugs.Count == 0)
            {
                return new BoardFeed
                {
                    Boards = await LoadRecentPublicBoardsAsync(excludeIds, take, cancellationToken),
                    IsPersonalized = false
                };
            }

            var matches = await ScorePublicBoardsByTagsAsync(
                new HashSet<string>(tasteSlugs),
                excludeIds,
                take,
                cancellationToken);

            var boards = matches
                .Select(match => match.Board)
                .ToList();

            if (boards.Count < take)
            {
                var alreadyReturned = boards.Select(board => board.Id).ToHashSet();
                alreadyReturned.UnionWith(excludeIds);

                var filler = await LoadRecentPublicBoardsAsync(
                    alreadyReturned,
                    take - boards.Count,
                    cancellationToken);

                boards.AddRange(filler);
            }

            return new BoardFeed
            {
                Boards = boards,
                IsPersonalized = matches.Count > 0,
                TasteTags = tasteSlugs
                    .Distinct()
                    .OrderBy(slug => slug)
                    .Take(5)
                    .Select(TagService.ToDisplayName)
                    .ToList()
            };
        }

        public async Task<SimilarBoardsResult> GetSimilarAsync(
            int boardId,
            string? viewerUserId,
            int take = DefaultSimilarCount,
            CancellationToken cancellationToken = default)
        {
            var board = await _context.Boards
                .Include(item => item.BoardTags)
                    .ThenInclude(boardTag => boardTag.Tag)
                .Include(item => item.Books)
                    .ThenInclude(book => book.BookTags)
                        .ThenInclude(bookTag => bookTag.Tag)
                .FirstOrDefaultAsync(item => item.Id == boardId, cancellationToken);

            if (board == null)
            {
                return new SimilarBoardsResult();
            }

            bool canView = board.IsPublic ||
                (!string.IsNullOrWhiteSpace(viewerUserId) && board.UserId == viewerUserId);

            if (!canView)
            {
                return new SimilarBoardsResult
                {
                    Found = true,
                    CanView = false,
                    BoardId = board.Id,
                    BoardTitle = board.Title
                };
            }

            var targetSlugs = TagService.GetAllTagSlugs(board);

            var matches = targetSlugs.Count == 0
                ? new List<BoardMatch>()
                : await ScorePublicBoardsByTagsAsync(
                    new HashSet<string>(targetSlugs),
                    new[] { board.Id },
                    take,
                    cancellationToken);

            return new SimilarBoardsResult
            {
                Found = true,
                CanView = true,
                BoardId = board.Id,
                BoardTitle = board.Title,
                Matches = matches
            };
        }

        public async Task<BoardSearchPage> SearchAsync(
            string? query,
            string? mood,
            int page = 1,
            int pageSize = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = DefaultPageSize;
            }

            string trimmedQuery = query?.Trim() ?? string.Empty;
            string trimmedMood = mood?.Trim() ?? string.Empty;

            var boardsQuery = FilterPublicBoards(trimmedQuery, trimmedMood);

            int totalBoards = await boardsQuery.CountAsync(cancellationToken);

            int totalPages = (int)Math.Ceiling(totalBoards / (double)pageSize);
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var boards = await boardsQuery
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .OrderByDescending(board => board.Books.Count)
                .ThenByDescending(board => board.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var books = string.IsNullOrWhiteSpace(trimmedQuery)
                ? new List<BoardBook>()
                : await SearchPublicBooksAsync(trimmedQuery, DefaultBookCount, cancellationToken);

            return new BoardSearchPage
            {
                Boards = boards,
                Books = books,
                RelatedMoodSlugs = await GetRelatedMoodSlugsAsync(
                    trimmedQuery,
                    trimmedMood,
                    cancellationToken),
                TotalBoards = totalBoards,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<MoodGroup>> GetPopularMoodsAsync(
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            var boardTagCounts = await _context.BoardTags
                .Where(boardTag => boardTag.Board != null && boardTag.Board.IsPublic)
                .Select(boardTag => boardTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            var bookTagCounts = await _context.BookTags
                .Where(bookTag =>
                    bookTag.BoardBook != null &&
                    bookTag.BoardBook.Board != null &&
                    bookTag.BoardBook.Board.IsPublic)
                .Select(bookTag => bookTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            return boardTagCounts
                .Concat(bookTagCounts)
                .Where(slug => !string.IsNullOrWhiteSpace(slug))
                .GroupBy(slug => slug)
                .Select(group => new MoodGroup
                {
                    Slug = group.Key,
                    Mood = TagService.ToDisplayName(group.Key),
                    Count = group.Count()
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Mood)
                .Take(take)
                .ToList();
        }

        public async Task<List<BoardBook>> GetRecentPublicBooksAsync(
            int take = DefaultBookCount,
            CancellationToken cancellationToken = default)
        {
            return await _context.BoardBooks
                .Include(book => book.Board)
                .Where(book => book.Board != null && book.Board.IsPublic)
                .OrderByDescending(book => book.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public static BoardMatch? ScoreBoardAgainstTags(Board candidate, IReadOnlyCollection<string> targetSlugs)
        {
            if (candidate == null || targetSlugs == null || targetSlugs.Count == 0)
            {
                return null;
            }

            var targetSet = targetSlugs as HashSet<string> ?? new HashSet<string>(targetSlugs);
            var matchedSlugs = TagService.GetAllTagSlugs(candidate)
                .Where(slug => targetSet.Contains(slug))
                .Distinct()
                .ToList();

            if (matchedSlugs.Count == 0)
            {
                return null;
            }

            return new BoardMatch
            {
                Board = candidate,
                Score = matchedSlugs.Count,
                MatchedTags = matchedSlugs
                    .Select(TagService.ToDisplayName)
                    .ToList()
            };
        }

        private async Task<List<string>> CollectUserTasteSlugsAsync(
            string userId,
            List<int> createdBoardIds,
            CancellationToken cancellationToken)
        {
            var savedBoardIds = await _context.SavedBoards
                .Where(saved => saved.UserId == userId)
                .Select(saved => saved.BoardId)
                .ToListAsync(cancellationToken);

            var seedIds = createdBoardIds
                .Concat(savedBoardIds)
                .Distinct()
                .ToList();

            if (seedIds.Count == 0)
            {
                return new List<string>();
            }

            var boardSlugs = await _context.BoardTags
                .Where(boardTag => seedIds.Contains(boardTag.BoardId))
                .Select(boardTag => boardTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            var bookSlugs = await _context.BookTags
                .Where(bookTag => bookTag.BoardBook != null && seedIds.Contains(bookTag.BoardBook.BoardId))
                .Select(bookTag => bookTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            var slugs = boardSlugs
                .Concat(bookSlugs)
                .Where(slug => !string.IsNullOrWhiteSpace(slug))
                .Distinct()
                .ToList();

            if (slugs.Count > 0)
            {
                return slugs;
            }

            var moodTags = await _context.Boards
                .Where(board => seedIds.Contains(board.Id))
                .Select(board => board.MoodTags)
                .ToListAsync(cancellationToken);

            var bookMoodTags = await _context.BoardBooks
                .Where(book => seedIds.Contains(book.BoardId))
                .Select(book => book.MoodTags)
                .ToListAsync(cancellationToken);

            return moodTags
                .Concat(bookMoodTags)
                .SelectMany(TagService.ParseTags)
                .Distinct()
                .ToList();
        }

        private async Task<List<BoardMatch>> ScorePublicBoardsByTagsAsync(
            HashSet<string> targetSlugs,
            IEnumerable<int> excludeBoardIds,
            int take,
            CancellationToken cancellationToken)
        {
            if (targetSlugs.Count == 0 || take <= 0)
            {
                return new List<BoardMatch>();
            }

            var exclude = excludeBoardIds.ToHashSet();

            var boardHits = await _context.BoardTags
                .Where(boardTag =>
                    boardTag.Board != null &&
                    boardTag.Board.IsPublic &&
                    targetSlugs.Contains(boardTag.Tag!.Slug) &&
                    !exclude.Contains(boardTag.BoardId))
                .Select(boardTag => new TagHit
                {
                    BoardId = boardTag.BoardId,
                    Slug = boardTag.Tag!.Slug
                })
                .ToListAsync(cancellationToken);

            var bookHits = await _context.BookTags
                .Where(bookTag =>
                    bookTag.BoardBook != null &&
                    bookTag.BoardBook.Board != null &&
                    bookTag.BoardBook.Board.IsPublic &&
                    targetSlugs.Contains(bookTag.Tag!.Slug) &&
                    !exclude.Contains(bookTag.BoardBook.BoardId))
                .Select(bookTag => new TagHit
                {
                    BoardId = bookTag.BoardBook!.BoardId,
                    Slug = bookTag.Tag!.Slug
                })
                .ToListAsync(cancellationToken);

            var ranked = boardHits
                .Concat(bookHits)
                .GroupBy(hit => hit.BoardId)
                .Select(group => new
                {
                    BoardId = group.Key,
                    MatchedSlugs = group.Select(hit => hit.Slug).Distinct().ToList()
                })
                .OrderByDescending(item => item.MatchedSlugs.Count)
                .ToList();

            if (ranked.Count < take)
            {
                var alreadyFound = ranked.Select(item => item.BoardId).ToHashSet();
                alreadyFound.UnionWith(exclude);

                var fallbackIds = await FindBoardsByMoodTagFallbackAsync(
                    targetSlugs,
                    alreadyFound,
                    take - ranked.Count,
                    cancellationToken);

                foreach (int boardId in fallbackIds)
                {
                    ranked.Add(new
                    {
                        BoardId = boardId,
                        MatchedSlugs = new List<string>()
                    });
                }
            }

            var topIds = ranked
                .Take(Math.Max(take * 2, take))
                .Select(item => item.BoardId)
                .ToList();

            if (topIds.Count == 0)
            {
                return new List<BoardMatch>();
            }

            var boards = await _context.Boards
                .Include(board => board.Books)
                    .ThenInclude(book => book.BookTags)
                        .ThenInclude(bookTag => bookTag.Tag)
                .Include(board => board.BoardTags)
                    .ThenInclude(boardTag => boardTag.Tag)
                .Include(board => board.VisualItems)
                .Where(board => topIds.Contains(board.Id))
                .ToListAsync(cancellationToken);

            var boardsById = boards.ToDictionary(board => board.Id);

            var matches = new List<BoardMatch>();

            foreach (var item in ranked)
            {
                if (!boardsById.TryGetValue(item.BoardId, out var board))
                {
                    continue;
                }

                var match = ScoreBoardAgainstTags(board, targetSlugs);
                if (match == null)
                {
                    continue;
                }

                matches.Add(match);
            }

            return matches
                .OrderByDescending(match => match.Score)
                .ThenByDescending(match => match.Board.Books.Count)
                .ThenByDescending(match => match.Board.CreatedAt)
                .Take(take)
                .ToList();
        }

        private async Task<List<int>> FindBoardsByMoodTagFallbackAsync(
            HashSet<string> targetSlugs,
            HashSet<int> excludeIds,
            int take,
            CancellationToken cancellationToken)
        {
            if (take <= 0)
            {
                return new List<int>();
            }

            var patterns = targetSlugs
                .SelectMany(slug => new[]
                {
                    SanitizeLike(TagService.ToDisplayName(slug)),
                    SanitizeLike(slug)
                })
                .Where(pattern => pattern.Length > 0)
                .Distinct()
                .ToList();

            if (patterns.Count == 0)
            {
                return new List<int>();
            }

            var candidates = await _context.Boards
                .Where(board => board.IsPublic && !excludeIds.Contains(board.Id))
                .Select(board => new
                {
                    board.Id,
                    board.MoodTags,
                    BookMoodTags = board.Books.Select(book => book.MoodTags).ToList()
                })
                .ToListAsync(cancellationToken);

            return candidates
                .Where(board =>
                    patterns.Any(pattern =>
                        ContainsIgnoreCase(board.MoodTags, pattern) ||
                        board.BookMoodTags.Any(tags => ContainsIgnoreCase(tags, pattern))))
                .Select(board => board.Id)
                .Take(take)
                .ToList();
        }

        private IQueryable<Board> FilterPublicBoards(string query, string mood)
        {
            IQueryable<Board> boards = _context.Boards.Where(board => board.IsPublic);

            if (!string.IsNullOrWhiteSpace(mood))
            {
                string moodSlug = TagService.ParseTags(mood).FirstOrDefault() ?? string.Empty;
                string moodPattern = SanitizeLike(mood);

                boards = boards.Where(board =>
                    (!string.IsNullOrWhiteSpace(moodSlug) &&
                        (board.BoardTags.Any(boardTag => boardTag.Tag != null && boardTag.Tag.Slug == moodSlug) ||
                         board.Books.Any(book => book.BookTags.Any(bookTag => bookTag.Tag != null && bookTag.Tag.Slug == moodSlug)))) ||
                    (!string.IsNullOrWhiteSpace(moodPattern) &&
                        (EF.Functions.Like(board.Title, "%" + moodPattern + "%") ||
                         EF.Functions.Like(board.MoodTags, "%" + moodPattern + "%") ||
                         board.Books.Any(book => EF.Functions.Like(book.MoodTags, "%" + moodPattern + "%")))));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string queryPattern = SanitizeLike(query);
                var querySlugs = TagService.ParseTags(query);

                boards = boards.Where(board =>
                    EF.Functions.Like(board.Title, "%" + queryPattern + "%") ||
                    EF.Functions.Like(board.Description, "%" + queryPattern + "%") ||
                    EF.Functions.Like(board.MoodTags, "%" + queryPattern + "%") ||
                    EF.Functions.Like(board.Theme, "%" + queryPattern + "%") ||
                    board.Books.Any(book =>
                        EF.Functions.Like(book.Title, "%" + queryPattern + "%") ||
                        EF.Functions.Like(book.Author, "%" + queryPattern + "%") ||
                        EF.Functions.Like(book.MoodTags, "%" + queryPattern + "%") ||
                        EF.Functions.Like(book.Reflection, "%" + queryPattern + "%")) ||
                    (querySlugs.Count > 0 &&
                     (board.BoardTags.Any(boardTag =>
                          boardTag.Tag != null && querySlugs.Contains(boardTag.Tag.Slug)) ||
                      board.Books.Any(book =>
                          book.BookTags.Any(bookTag =>
                              bookTag.Tag != null && querySlugs.Contains(bookTag.Tag.Slug))))));
            }

            return boards;
        }

        private async Task<List<string>> GetRelatedMoodSlugsAsync(
            string query,
            string mood,
            CancellationToken cancellationToken)
        {
            var matchingIds = await FilterPublicBoards(query, mood)
                .Select(board => board.Id)
                .ToListAsync(cancellationToken);

            if (matchingIds.Count == 0)
            {
                return new List<string>();
            }

            var excluded = new HashSet<string>(TagService.ParseTags(query));
            foreach (string slug in TagService.ParseTags(mood))
            {
                excluded.Add(slug);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                excluded.Add(query.Trim().ToLowerInvariant());
            }

            var boardSlugs = await _context.BoardTags
                .Where(boardTag => matchingIds.Contains(boardTag.BoardId))
                .Select(boardTag => boardTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            var bookSlugs = await _context.BookTags
                .Where(bookTag => bookTag.BoardBook != null && matchingIds.Contains(bookTag.BoardBook.BoardId))
                .Select(bookTag => bookTag.Tag!.Slug)
                .ToListAsync(cancellationToken);

            return boardSlugs
                .Concat(bookSlugs)
                .Where(slug => !string.IsNullOrWhiteSpace(slug) && slug.Length >= 2 && !excluded.Contains(slug))
                .GroupBy(slug => slug)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .Take(8)
                .ToList();
        }

        private async Task<List<BoardBook>> SearchPublicBooksAsync(
            string query,
            int take,
            CancellationToken cancellationToken)
        {
            string queryPattern = SanitizeLike(query);

            return await _context.BoardBooks
                .Include(book => book.Board)
                .Where(book =>
                    book.Board != null &&
                    book.Board.IsPublic &&
                    (EF.Functions.Like(book.Title, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.Author, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.MoodTags, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.Reflection, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.ShortDescription, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.Board.Title, "%" + queryPattern + "%") ||
                     EF.Functions.Like(book.Board.MoodTags, "%" + queryPattern + "%")))
                .OrderByDescending(book => book.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private async Task<List<Board>> LoadRecentPublicBoardsAsync(
            IEnumerable<int> excludeBoardIds,
            int take,
            CancellationToken cancellationToken)
        {
            if (take <= 0)
            {
                return new List<Board>();
            }

            var exclude = excludeBoardIds.ToHashSet();

            return await _context.Boards
                .Include(board => board.Books)
                .Include(board => board.VisualItems)
                .Where(board => board.IsPublic && !exclude.Contains(board.Id))
                .OrderByDescending(board => board.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private static string SanitizeLike(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Replace("%", string.Empty).Replace("_", string.Empty);
        }

        private static bool ContainsIgnoreCase(string? value, string pattern)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TagHit
        {
            public int BoardId { get; set; }

            public string Slug { get; set; } = string.Empty;
        }
    }
}
