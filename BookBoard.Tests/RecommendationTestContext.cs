using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookBoard.Tests;

internal sealed class RecommendationTestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Db { get; }

    public BoardRecommendationService Recommendations { get; }

    public TagService Tags { get; }

    public RecommendationTestContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();

        Recommendations = new BoardRecommendationService(Db);
        Tags = new TagService(Db);
    }

    public ApplicationUser AddUser(string id, string name)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            Email = $"{name}@bookboard.test",
            NormalizedEmail = $"{name.ToUpperInvariant()}@BOOKBOARD.TEST",
            DisplayName = name,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        Db.Users.Add(user);
        return user;
    }

    public async Task<Board> AddBoardAsync(
        string ownerId,
        string title,
        string rawTags,
        bool isPublic = true,
        DateTime? createdAt = null)
    {
        var board = new Board
        {
            Title = title,
            Description = title,
            MoodTags = rawTags,
            IsPublic = isPublic,
            UserId = ownerId,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        Db.Boards.Add(board);
        await Db.SaveChangesAsync();

        await Tags.SyncBoardTagsAsync(board.Id, rawTags);
        await Db.SaveChangesAsync();

        return board;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
