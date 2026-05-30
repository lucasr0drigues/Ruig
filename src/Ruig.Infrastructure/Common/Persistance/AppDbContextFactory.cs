using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace Ruig.Infrastructure.Common.Persistance
{
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string DefaultConnectionString =
            "Host=localhost;Port=5432;Database=ruig;Username=postgres;Password=postgres";

        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("RUIG_CONNECTION_STRING") ??
                ReadConnectionStringFromAppSettings() ??
                DefaultConnectionString;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .Options;

            return new AppDbContext(options);
        }

        private static string? ReadConnectionStringFromAppSettings()
        {
            var apiProjectDirectory = FindApiProjectDirectory();
            if (apiProjectDirectory is null)
                return null;

            foreach (var fileName in new[] { "appsettings.Development.json", "appsettings.json" })
            {
                var path = Path.Combine(apiProjectDirectory, fileName);
                if (!File.Exists(path))
                    continue;

                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                    && connectionStrings.TryGetProperty("Default", out var defaultConnectionString))
                {
                    return defaultConnectionString.GetString();
                }
            }

            return null;
        }

        private static string? FindApiProjectDirectory()
        {
            var searchDirectories = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var directory in searchDirectories)
            {
                var current = new DirectoryInfo(directory);

                while (current is not null)
                {
                    var apiProjectPath = Path.Combine(current.FullName, "src", "Ruig.Api", "Ruig.Api.csproj");
                    if (File.Exists(apiProjectPath))
                        return Path.GetDirectoryName(apiProjectPath);

                    current = current.Parent;
                }
            }

            return null;
        }
    }
}
