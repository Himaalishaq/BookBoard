using BookBoard.Data;
using BookBoard.Models;
using BookBoard.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// With <Nullable>enable</Nullable> on, ASP.NET Core treats plain non-nullable
// `string` properties (Description, ShortDescription, Reflection, etc.) as
// implicitly required for validation, even without a [Required] attribute.
// Turning this off lets those fields stay genuinely optional.
builder.Services.Configure<MvcOptions>(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var database = DatabaseOptions.Resolve(builder.Configuration);
builder.Services.AddSingleton(database);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (database.IsPostgreSql)
    {
        options.UseNpgsql(database.ConnectionString);
    }
    else
    {
        options.UseSqlite(database.ConnectionString);
    }
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("BookBoard");

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.Name = "BookBoard.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpClient<OpenLibraryService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<BoardRecommendationService>();
builder.Services.AddHttpClient();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

Directory.CreateDirectory(database.DataDirectory);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (database.IsPostgreSql)
    {
        // Historical migrations were generated for SQLite column types.
        // Postgres gets the current model on first boot; later deploys keep
        // the existing schema so accounts and boards survive.
        context.Database.EnsureCreated();
    }
    else
    {
        context.Database.Migrate();
    }
}

app.UseForwardedHeaders();

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

public partial class Program
{
}
