using BookBoard.Models;
using BookBoard.Services;

namespace BookBoard.Tests;

public class TagServiceTests
{
    [Fact]
    public void ParseTags_splits_commas_and_normalizes_to_slugs()
    {
        var slugs = TagService.ParseTags("Dark Academia,  healing, DARK ACADEMIA, cozy winter");

        Assert.Equal(new[] { "dark-academia", "healing", "cozy-winter" }, slugs);
    }

    [Fact]
    public void ParseTags_returns_empty_for_blank_input()
    {
        Assert.Empty(TagService.ParseTags(null));
        Assert.Empty(TagService.ParseTags("   "));
    }

    [Fact]
    public void ToDisplayName_title_cases_hyphenated_slugs()
    {
        Assert.Equal("Dark Academia", TagService.ToDisplayName("dark-academia"));
        Assert.Equal(string.Empty, TagService.ToDisplayName(""));
    }

    [Fact]
    public void GetAllTagSlugs_uses_relational_tags_when_present()
    {
        var cozy = new Tag { Id = 1, Name = "Cozy", Slug = "cozy" };
        var healing = new Tag { Id = 2, Name = "Healing", Slug = "healing" };

        var board = new Board
        {
            Title = "Soft reads",
            MoodTags = "ignored, because-relational-exists",
            BoardTags =
            {
                new BoardTag { Tag = cozy }
            },
            Books =
            {
                new BoardBook
                {
                    Title = "A book",
                    MoodTags = "also-ignored",
                    BookTags = { new BookTag { Tag = healing } }
                }
            }
        };

        var slugs = TagService.GetAllTagSlugs(board);

        Assert.Equal(new[] { "cozy", "healing" }, slugs);
    }

    [Fact]
    public void GetAllTagSlugs_falls_back_to_mood_tag_strings()
    {
        var board = new Board
        {
            Title = "Legacy board",
            MoodTags = "Spiritual, Healing",
            Books =
            {
                new BoardBook { Title = "A book", MoodTags = "reflective" }
            }
        };

        var slugs = TagService.GetAllTagSlugs(board);

        Assert.Equal(new[] { "spiritual", "healing", "reflective" }, slugs);
    }
}
