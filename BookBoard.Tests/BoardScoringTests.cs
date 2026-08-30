using BookBoard.Models;
using BookBoard.Services;

namespace BookBoard.Tests;

public class BoardScoringTests
{
    [Fact]
    public void ScoreBoardAgainstTags_counts_shared_tags_and_excludes_non_matches()
    {
        var candidate = CreateBoard("Rainy comfort", "cozy", "rainy", "healing");

        var match = BoardRecommendationService.ScoreBoardAgainstTags(
            candidate,
            new[] { "cozy", "spiritual" });

        Assert.NotNull(match);
        Assert.Equal(1, match.Score);
        Assert.Contains("Cozy", match.MatchedTags);
        Assert.DoesNotContain("Spiritual", match.MatchedTags);
    }

    [Fact]
    public void ScoreBoardAgainstTags_returns_null_when_there_is_no_overlap()
    {
        var candidate = CreateBoard("Fantasy only", "fantasy");

        var match = BoardRecommendationService.ScoreBoardAgainstTags(
            candidate,
            new[] { "cozy" });

        Assert.Null(match);
    }

    [Fact]
    public void ScoreBoardAgainstTags_returns_null_for_empty_targets()
    {
        var candidate = CreateBoard("Anything", "cozy");

        Assert.Null(BoardRecommendationService.ScoreBoardAgainstTags(candidate, Array.Empty<string>()));
    }

    [Fact]
    public void ScoreBoardAgainstTags_includes_book_tags_on_the_candidate()
    {
        var candidate = CreateBoard("Quiet shelf", "cozy");
        candidate.Books.Add(new BoardBook
        {
            Title = "A winter book",
            BookTags =
            {
                new BookTag { Tag = new Tag { Slug = "winter", Name = "Winter" } }
            }
        });

        var match = BoardRecommendationService.ScoreBoardAgainstTags(
            candidate,
            new[] { "winter" });

        Assert.NotNull(match);
        Assert.Equal(1, match.Score);
        Assert.Contains("Winter", match.MatchedTags);
    }

    private static Board CreateBoard(string title, params string[] slugs)
    {
        var board = new Board { Title = title };

        foreach (string slug in slugs)
        {
            board.BoardTags.Add(new BoardTag
            {
                Tag = new Tag
                {
                    Slug = slug,
                    Name = TagService.ToDisplayName(slug)
                }
            });
        }

        return board;
    }
}
