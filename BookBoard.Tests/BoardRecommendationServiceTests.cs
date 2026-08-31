using BookBoard.Models;

namespace BookBoard.Tests;

public class BoardRecommendationServiceTests
{
    [Fact]
    public async Task GetSimilar_returns_public_boards_that_share_tags()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("owner-a", "Ava");
        fixture.AddUser("owner-b", "Ben");
        await fixture.Db.SaveChangesAsync();

        var source = await fixture.AddBoardAsync("owner-a", "Healing winter", "healing, cozy");
        var match = await fixture.AddBoardAsync("owner-b", "Cozy recovery", "cozy, healing, tea");
        await fixture.AddBoardAsync("owner-b", "Only fantasy", "fantasy");

        var result = await fixture.Recommendations.GetSimilarAsync(source.Id, "owner-a");

        Assert.True(result.Found);
        Assert.True(result.CanView);
        Assert.Equal("Healing winter", result.BoardTitle);
        Assert.Single(result.Matches);
        Assert.Equal(match.Id, result.Matches[0].Board.Id);
        Assert.Equal(2, result.Matches[0].Score);
        Assert.Contains("Cozy", result.Matches[0].MatchedTags);
        Assert.Contains("Healing", result.Matches[0].MatchedTags);
    }

    [Fact]
    public async Task GetSimilar_excludes_the_source_board_and_private_boards()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("owner-a", "Ava");
        fixture.AddUser("owner-b", "Ben");
        await fixture.Db.SaveChangesAsync();

        var source = await fixture.AddBoardAsync("owner-a", "Public cozy", "cozy");
        await fixture.AddBoardAsync("owner-b", "Secret cozy", "cozy", isPublic: false);

        var result = await fixture.Recommendations.GetSimilarAsync(source.Id, "stranger");

        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task GetSimilar_hides_private_boards_from_other_viewers()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("owner-a", "Ava");
        await fixture.Db.SaveChangesAsync();

        var privateBoard = await fixture.AddBoardAsync("owner-a", "Private diary", "cozy", isPublic: false);

        var hidden = await fixture.Recommendations.GetSimilarAsync(privateBoard.Id, "someone-else");
        var visibleToOwner = await fixture.Recommendations.GetSimilarAsync(privateBoard.Id, "owner-a");

        Assert.True(hidden.Found);
        Assert.False(hidden.CanView);
        Assert.Empty(hidden.Matches);
        Assert.True(visibleToOwner.CanView);
    }

    [Fact]
    public async Task GetSimilar_returns_empty_matches_when_the_board_has_no_tags()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("owner-a", "Ava");
        await fixture.Db.SaveChangesAsync();

        var source = await fixture.AddBoardAsync("owner-a", "Blank board", "");

        var result = await fixture.Recommendations.GetSimilarAsync(source.Id, "owner-a");

        Assert.True(result.Found);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task GetPersonalized_uses_created_and_saved_board_tags()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("reader", "Rina");
        fixture.AddUser("curator", "Cole");
        await fixture.Db.SaveChangesAsync();

        await fixture.AddBoardAsync("reader", "My healing shelf", "healing");
        var saved = await fixture.AddBoardAsync("curator", "Public cozy", "cozy");
        var recommended = await fixture.AddBoardAsync("curator", "Soft recovery", "healing, cozy");
        await fixture.AddBoardAsync("curator", "Adventure only", "fantasy");

        fixture.Db.SavedBoards.Add(new SavedBoard
        {
            UserId = "reader",
            BoardId = saved.Id,
            SavedAt = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var feed = await fixture.Recommendations.GetPersonalizedAsync("reader");

        Assert.True(feed.IsPersonalized);
        Assert.Contains(feed.Boards, board => board.Id == recommended.Id);
        Assert.DoesNotContain(feed.Boards, board => board.Title == "My healing shelf");
        Assert.Contains("Healing", feed.TasteTags);
        Assert.Contains("Cozy", feed.TasteTags);
    }

    [Fact]
    public async Task GetPersonalized_falls_back_to_newest_public_boards_without_taste()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("new-user", "Nia");
        fixture.AddUser("curator", "Cole");
        await fixture.Db.SaveChangesAsync();

        var newest = await fixture.AddBoardAsync(
            "curator",
            "Brand new public board",
            "fantasy",
            createdAt: DateTime.UtcNow.AddMinutes(1));
        await fixture.AddBoardAsync(
            "curator",
            "Older public board",
            "cozy",
            createdAt: DateTime.UtcNow.AddDays(-2));

        var feed = await fixture.Recommendations.GetPersonalizedAsync("new-user");

        Assert.False(feed.IsPersonalized);
        Assert.Equal(newest.Id, feed.Boards[0].Id);
    }

    [Fact]
    public async Task Search_finds_boards_by_tag_and_paginates()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("curator", "Cole");
        await fixture.Db.SaveChangesAsync();

        for (int i = 1; i <= 3; i++)
        {
            await fixture.AddBoardAsync("curator", $"Cozy board {i}", "cozy");
        }

        await fixture.AddBoardAsync("curator", "Fantasy board", "fantasy");

        var page = await fixture.Recommendations.SearchAsync(query: null, mood: "cozy", page: 1, pageSize: 2);

        Assert.Equal(3, page.TotalBoards);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Boards.Count);
        Assert.All(page.Boards, board => Assert.Contains("Cozy", board.MoodTags, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPopularMoods_counts_public_board_tags()
    {
        using var fixture = new RecommendationTestContext();
        fixture.AddUser("curator", "Cole");
        await fixture.Db.SaveChangesAsync();

        await fixture.AddBoardAsync("curator", "One", "cozy, healing");
        await fixture.AddBoardAsync("curator", "Two", "cozy");
        await fixture.AddBoardAsync("curator", "Hidden", "cozy", isPublic: false);

        var moods = await fixture.Recommendations.GetPopularMoodsAsync();

        Assert.Equal("cozy", moods[0].Slug);
        Assert.Equal(2, moods[0].Count);
        Assert.Equal("Cozy", moods[0].Mood);
    }
}
