using BookBoard.Models;

namespace BookBoard.Services
{
    public sealed class CollageTile
    {
        public string Type { get; init; } = "color";

        public string Content { get; init; } = string.Empty;
    }

    public static class BoardCollageComposer
    {
        public const int SurroundingSlotCount = 8;

        public static List<string> GetPalette(string? accentColor)
        {
            string accent = (accentColor ?? "brown").Trim().ToLowerInvariant();

            return accent switch
            {
                "gold" => new List<string> { "#e8c988", "#f1dba6", "#b9862f", "#f7ecd2" },
                "green" => new List<string> { "#b7cfa4", "#8aaa7b", "#5f7d4e", "#e4f0dc" },
                "blue" => new List<string> { "#b6c4e0", "#899bc8", "#566a99", "#e4eaf6" },
                "lavender" => new List<string> { "#d9c6ec", "#bda6d9", "#8064a3", "#f3ebfa" },
                "rose" => new List<string> { "#eac6c6", "#d8a1a1", "#a9585c", "#f8eaea" },
                _ => new List<string> { "#e6d2bf", "#c9a37f", "#8a5a3d", "#f3dfcf" }
            };
        }

        public static List<CollageTile> ComposeSurroundingTiles(Board board)
        {
            var tiles = new List<CollageTile>();

            if (board.VisualItems != null)
            {
                foreach (var item in board.VisualItems.OrderBy(visual => visual.SortOrder))
                {
                    if (tiles.Count >= SurroundingSlotCount)
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(item.Content) || string.IsNullOrWhiteSpace(item.ItemType))
                    {
                        continue;
                    }

                    tiles.Add(new CollageTile
                    {
                        Type = item.ItemType.Trim().ToLowerInvariant(),
                        Content = item.Content.Trim()
                    });
                }
            }

            if (board.Books != null)
            {
                foreach (var book in board.Books
                    .Where(item => !string.IsNullOrWhiteSpace(item.CoverUrl))
                    .OrderByDescending(item => item.CreatedAt))
                {
                    if (tiles.Count >= SurroundingSlotCount)
                    {
                        break;
                    }

                    string coverUrl = book.CoverUrl.Trim();

                    if (tiles.Any(tile => tile.Type == "bookcover" && tile.Content == coverUrl))
                    {
                        continue;
                    }

                    tiles.Add(new CollageTile
                    {
                        Type = "bookcover",
                        Content = coverUrl
                    });
                }
            }

            foreach (string word in GetMoodWords(board))
            {
                if (tiles.Count >= SurroundingSlotCount)
                {
                    break;
                }

                tiles.Add(new CollageTile
                {
                    Type = "text",
                    Content = word
                });
            }

            var palette = GetPalette(board.AccentColor);
            int paletteIndex = 0;

            while (tiles.Count < SurroundingSlotCount)
            {
                tiles.Add(new CollageTile
                {
                    Type = "color",
                    Content = palette[paletteIndex % palette.Count]
                });

                paletteIndex++;
            }

            return tiles;
        }

        public static string OwnerLabel(Board board)
        {
            if (!string.IsNullOrWhiteSpace(board.User?.DisplayName))
            {
                return board.User.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(board.User?.Email))
            {
                return board.User.Email;
            }

            return "a reader";
        }

        private static IEnumerable<string> GetMoodWords(Board board)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(board.MoodTags))
            {
                foreach (string word in board.MoodTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (seen.Add(word))
                    {
                        yield return word;
                    }
                }
            }

            foreach (string slug in TagService.GetAllTagSlugs(board))
            {
                string display = TagService.ToDisplayName(slug);
                if (!string.IsNullOrWhiteSpace(display) && seen.Add(display))
                {
                    yield return display;
                }
            }
        }
    }

    public sealed class CanvaCollageModel
    {
        public string Title { get; init; } = "Mood Board";

        public string Owner { get; init; } = "a reader";

        public string Accent { get; init; } = "brown";

        public string Kicker { get; init; } = "Mood Board";

        public List<CollageTile> Tiles { get; init; } = new List<CollageTile>();

        public string AccentClass =>
            "accent-" + (string.IsNullOrWhiteSpace(Accent) ? "brown" : Accent.Trim().ToLowerInvariant().Replace(" ", "-"));

        public static CanvaCollageModel FromBoard(Board board)
        {
            return new CanvaCollageModel
            {
                Title = string.IsNullOrWhiteSpace(board.Title) ? "Untitled board" : board.Title,
                Owner = BoardCollageComposer.OwnerLabel(board),
                Accent = string.IsNullOrWhiteSpace(board.AccentColor) ? "brown" : board.AccentColor,
                Tiles = BoardCollageComposer.ComposeSurroundingTiles(board)
            };
        }

        public static CanvaCollageModel Sample()
        {
            var board = new Board
            {
                Title = "Mood Board",
                AccentColor = "brown",
                MoodTags = "cozy, healing, rain"
            };

            return new CanvaCollageModel
            {
                Title = "Mood Board",
                Owner = "BookBoard",
                Accent = "brown",
                Tiles = BoardCollageComposer.ComposeSurroundingTiles(board)
            };
        }
    }
}
