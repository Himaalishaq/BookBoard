using System.Data.Common;

namespace BookBoard.Services
{
    public enum BookBoardDatabaseProvider
    {
        Sqlite,
        PostgreSql
    }

    public sealed class ResolvedDatabase
    {
        public BookBoardDatabaseProvider Provider { get; init; }

        public string ConnectionString { get; init; } = string.Empty;

        public string DataDirectory { get; init; } = string.Empty;

        public bool IsPostgreSql => Provider == BookBoardDatabaseProvider.PostgreSql;
    }

    public static class DatabaseOptions
    {
        public const string SqliteFileName = "bookboard.db";

        public static ResolvedDatabase Resolve(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            string? databaseUrl = FirstNonEmpty(
                Environment.GetEnvironmentVariable("DATABASE_URL"),
                configuration["DATABASE_URL"],
                configuration.GetConnectionString("Postgres"),
                configuration.GetConnectionString("PostgreSQL"));

            if (IsPostgres(databaseUrl))
            {
                return new ResolvedDatabase
                {
                    Provider = BookBoardDatabaseProvider.PostgreSql,
                    ConnectionString = NormalizePostgres(databaseUrl!),
                    DataDirectory = ResolveDataDirectory(configuration, preferExisting: false)
                };
            }

            string? defaultConnection = configuration.GetConnectionString("DefaultConnection");
            if (IsPostgres(defaultConnection))
            {
                return new ResolvedDatabase
                {
                    Provider = BookBoardDatabaseProvider.PostgreSql,
                    ConnectionString = NormalizePostgres(defaultConnection!),
                    DataDirectory = ResolveDataDirectory(configuration, preferExisting: false)
                };
            }

            string dataDirectory = ResolveDataDirectory(configuration, preferExisting: true);
            string sqlitePath = Path.Combine(dataDirectory, SqliteFileName);

            return new ResolvedDatabase
            {
                Provider = BookBoardDatabaseProvider.Sqlite,
                ConnectionString = "Data Source=" + sqlitePath,
                DataDirectory = dataDirectory
            };
        }

        public static bool IsPostgres(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            string trimmed = connectionString.Trim();
            return trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                    && !trimmed.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizePostgres(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A PostgreSQL connection string is required.", nameof(connectionString));
            }

            string trimmed = connectionString.Trim();

            if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var uri = new Uri(trimmed);
            var builder = new DbConnectionStringBuilder();

            string[] userInfo = uri.UserInfo.Split(':', 2);
            builder["Host"] = uri.Host;
            if (uri.Port > 0)
            {
                builder["Port"] = uri.Port;
            }

            builder["Database"] = uri.AbsolutePath.Trim('/');
            builder["Username"] = Uri.UnescapeDataString(userInfo[0]);
            if (userInfo.Length > 1)
            {
                builder["Password"] = Uri.UnescapeDataString(userInfo[1]);
            }

            bool local = uri.Host is "localhost" or "127.0.0.1" or "::1";
            builder["SSL Mode"] = local ? "Disable" : "Require";
            if (!local)
            {
                builder["Trust Server Certificate"] = true;
            }

            return builder.ConnectionString;
        }

        private static string ResolveDataDirectory(IConfiguration configuration, bool preferExisting)
        {
            string? configured = FirstNonEmpty(
                Environment.GetEnvironmentVariable("BOOKBOARD_DATA_DIR"),
                configuration["BOOKBOARD_DATA_DIR"]);

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            if (preferExisting && Directory.Exists("/data"))
            {
                return "/data";
            }

            return Directory.GetCurrentDirectory();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
