using BookBoard.Models;
using BookBoard.Services;

namespace BookBoard.Tests;

public class BoardCollageComposerTests
{
    [Fact]
    public void Empty_board_fills_eight_palette_tiles_and_never_uses_emoji_symbols()
    {
        var board = new Board
        {
            Title = "Blank board",
            AccentColor = "rose",
            IconSymbols = "📚, ☕, ✨"
        };

        var tiles = BoardCollageComposer.ComposeSurroundingTiles(board);

        Assert.Equal(8, tiles.Count);
        Assert.All(tiles, tile => Assert.Equal("color", tile.Type));
        Assert.DoesNotContain(tiles, tile => tile.Type == "icon");
        Assert.Equal(BoardCollageComposer.GetPalette("rose"), tiles.Select(tile => tile.Content).Distinct().ToList());
    }

    [Fact]
    public void Canvas_tiles_keep_sort_order_before_covers_and_mood_words()
    {
        var board = new Board
        {
            Title = "Chaos Looming",
            MoodTags = "moody, epic",
            VisualItems =
            {
                new BoardVisualItem { SortOrder = 2, ItemType = "text", Content = "Heal more than you destroy." },
                new BoardVisualItem { SortOrder = 1, ItemType = "image", Content = "https://example.com/forest.jpg" }
            },
            Books =
            {
                new BoardBook { Title = "A book", CoverUrl = "https://example.com/cover.jpg", CreatedAt = DateTime.UtcNow }
            }
        };

        var tiles = BoardCollageComposer.ComposeSurroundingTiles(board);

        Assert.Equal("image", tiles[0].Type);
        Assert.Equal("https://example.com/forest.jpg", tiles[0].Content);
        Assert.Equal("text", tiles[1].Type);
        Assert.Equal("Heal more than you destroy.", tiles[1].Content);
        Assert.Equal("bookcover", tiles[2].Type);
        Assert.Equal("text", tiles[3].Type);
        Assert.Equal("moody", tiles[3].Content);
        Assert.Equal(8, tiles.Count);
    }

    [Fact]
    public void User_placed_icons_are_kept_but_theme_symbols_are_not_used_as_filler()
    {
        var board = new Board
        {
            Title = "Soft shelf",
            IconSymbols = "🌙, ✨",
            VisualItems =
            {
                new BoardVisualItem { SortOrder = 1, ItemType = "icon", Content = "🕯️" }
            }
        };

        var tiles = BoardCollageComposer.ComposeSurroundingTiles(board);

        Assert.Equal("icon", tiles[0].Type);
        Assert.Equal("🕯️", tiles[0].Content);
        Assert.DoesNotContain(tiles.Skip(1), tile => tile.Type == "icon");
    }

    [Fact]
    public void OwnerLabel_prefers_display_name_then_email()
    {
        var named = new Board
        {
            User = new ApplicationUser { DisplayName = "H.B. Reneau", Email = "hb@bookboard.test" }
        };
        var emailed = new Board
        {
            User = new ApplicationUser { DisplayName = "", Email = "reader@bookboard.test" }
        };

        Assert.Equal("H.B. Reneau", BoardCollageComposer.OwnerLabel(named));
        Assert.Equal("reader@bookboard.test", BoardCollageComposer.OwnerLabel(emailed));
        Assert.Equal("a reader", BoardCollageComposer.OwnerLabel(new Board()));
    }

    [Fact]
    public void Mood_words_fill_before_palette_colors()
    {
        var board = new Board
        {
            Title = "Healing Winter",
            AccentColor = "blue",
            MoodTags = "cozy, healing"
        };

        var tiles = BoardCollageComposer.ComposeSurroundingTiles(board);

        Assert.Equal("text", tiles[0].Type);
        Assert.Equal("cozy", tiles[0].Content);
        Assert.Equal("healing", tiles[1].Content);
        Assert.Equal("color", tiles[2].Type);
        Assert.Equal(8, tiles.Count);
        Assert.DoesNotContain(tiles, tile => tile.Type == "icon");
    }
}
