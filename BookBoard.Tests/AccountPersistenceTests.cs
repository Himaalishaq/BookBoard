using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BookBoard.Tests;

public class AccountPersistenceTests : IClassFixture<BookBoardWebFactory>
{
    private readonly BookBoardWebFactory _factory;

    public AccountPersistenceTests(BookBoardWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_creates_account_and_keeps_board_after_login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        string email = $"reader-{Guid.NewGuid():N}@bookboard.test";
        const string password = "Password1!";

        var registerPage = await client.GetAsync("/Identity/Account/Register");
        registerPage.EnsureSuccessStatusCode();
        string registerHtml = await registerPage.Content.ReadAsStringAsync();

        Assert.Contains("Create an account that keeps your boards", registerHtml);

        var registerResponse = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.DisplayName"] = "R. Vale",
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(registerHtml)
        }));

        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var boardsPage = await client.GetAsync("/Boards");
        boardsPage.EnsureSuccessStatusCode();
        string boardsHtml = await boardsPage.Content.ReadAsStringAsync();
        Assert.Contains("Hi, R. Vale", boardsHtml);

        var createPage = await client.GetAsync("/CreateBoard");
        createPage.EnsureSuccessStatusCode();
        string createHtml = await createPage.Content.ReadAsStringAsync();

        var createResponse = await client.PostAsync("/CreateBoard", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Board.Title"] = "Persistent Cozy Shelf",
            ["Board.Description"] = "Should survive logout",
            ["Board.MoodTags"] = "cozy, healing",
            ["Board.Theme"] = "cozy",
            ["Board.BackgroundStyle"] = "soft-glow",
            ["Board.AccentColor"] = "rose",
            ["Board.IconSymbols"] = "📚, ☕, ✨",
            ["Board.IsPublic"] = "true",
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(createHtml)
        }));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var ownedBoards = await client.GetAsync("/Boards");
        string ownedHtml = await ownedBoards.Content.ReadAsStringAsync();
        Assert.Contains("Persistent Cozy Shelf", ownedHtml);

        await client.PostAsync("/Identity/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(ownedHtml)
        }));

        var loginPage = await client.GetAsync("/Identity/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        string loginHtml = await loginPage.Content.ReadAsStringAsync();
        Assert.Contains("Sign in to your reading boards", loginHtml);

        var loginResponse = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "true",
            ["__RequestVerificationToken"] = ReadAntiforgeryToken(loginHtml)
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var afterLogin = await client.GetAsync("/Boards");
        afterLogin.EnsureSuccessStatusCode();
        string afterHtml = await afterLogin.Content.ReadAsStringAsync();
        Assert.Contains("Persistent Cozy Shelf", afterHtml);
        Assert.Contains("Hi, R. Vale", afterHtml);
    }

    private static string ReadAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|value=""([^""]+)""[^>]*name=""__RequestVerificationToken""");

        string token = match.Success
            ? (match.Groups[1].Success && match.Groups[1].Value.Length > 0
                ? match.Groups[1].Value
                : match.Groups[2].Value)
            : string.Empty;

        Assert.False(string.IsNullOrWhiteSpace(token), "Expected an antiforgery token on the page.");
        return token;
    }
}

public class BookBoardWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDir = Path.Combine(
        Path.GetTempPath(),
        "bookboard-web-tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dataDir);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BOOKBOARD_DATA_DIR"] = _dataDir,
                ["ConnectionStrings:DefaultConnection"] = "Data Source=" + Path.Combine(_dataDir, "bookboard.db")
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (Directory.Exists(_dataDir))
            {
                Directory.Delete(_dataDir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
