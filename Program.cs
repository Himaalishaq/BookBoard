using BookBoard.Data;
using BookBoard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookBoard.Services;
using BookBoard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpClient<OpenLibraryService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddHttpClient();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/image-proxy", async (string? url, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(url) ||
        !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "http" && uri.Scheme != "https"))
    {
        return Results.BadRequest("A valid image URL is required.");
    }

    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (compatible; BookBoardImageProxy/1.0)");

    try
    {
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return Results.NotFound();
        }

        string? contentType = response.Content.Headers.ContentType?.MediaType;

        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
        {
            return Results.BadRequest("That link did not return an image.");
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();

        if (bytes.Length > 8 * 1024 * 1024)
        {
            return Results.BadRequest("Image is too large.");
        }

        return Results.File(bytes, contentType);
    }
    catch
    {
        return Results.NotFound();
    }
});

app.MapRazorPages();

app.Run();