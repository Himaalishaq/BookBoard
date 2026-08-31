using BookBoard.Services;
using Microsoft.Extensions.Configuration;

namespace BookBoard.Tests;

public class DatabaseOptionsTests
{
    [Fact]
    public void NormalizePostgres_converts_render_style_url()
    {
        string normalized = DatabaseOptions.NormalizePostgres(
            "postgres://book:p%40ss@dpg-host:5432/bookboard");

        Assert.Contains("Host=dpg-host", normalized);
        Assert.Contains("Port=5432", normalized);
        Assert.Contains("Database=bookboard", normalized);
        Assert.Contains("Username=book", normalized);
        Assert.Contains("Password=p@ss", normalized);
        Assert.Contains("SSL Mode=Require", normalized);
    }

    [Fact]
    public void NormalizePostgres_disables_ssl_for_localhost()
    {
        string normalized = DatabaseOptions.NormalizePostgres(
            "postgresql://reader:secret@127.0.0.1:5432/bookboard");

        Assert.Contains("SSL Mode=Disable", normalized);
        Assert.DoesNotContain("SSL Mode=Require", normalized);
    }

    [Fact]
    public void Resolve_uses_DATABASE_URL_for_postgres()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgres://book:pass@db.example:5432/bookboard",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=bookboard.db"
            })
            .Build();

        var resolved = DatabaseOptions.Resolve(config);

        Assert.Equal(BookBoardDatabaseProvider.PostgreSql, resolved.Provider);
        Assert.Contains("Host=db.example", resolved.ConnectionString);
    }

    [Fact]
    public void Resolve_keeps_sqlite_in_data_directory()
    {
        string dataDir = Path.Combine(Path.GetTempPath(), "bookboard-db-tests", Guid.NewGuid().ToString("N"));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BOOKBOARD_DATA_DIR"] = dataDir,
                ["ConnectionStrings:DefaultConnection"] = "Data Source=bookboard.db"
            })
            .Build();

        var resolved = DatabaseOptions.Resolve(config);

        Assert.Equal(BookBoardDatabaseProvider.Sqlite, resolved.Provider);
        Assert.Equal(Path.GetFullPath(dataDir), resolved.DataDirectory);
        Assert.Equal("Data Source=" + Path.Combine(Path.GetFullPath(dataDir), "bookboard.db"), resolved.ConnectionString);
    }
}
