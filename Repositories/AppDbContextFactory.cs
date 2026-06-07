using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AutoStock.Repositories
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var apiProjectPath = ResolveApiProjectPath(currentDirectory);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionStrings = configuration
                .GetSection(ConnectionStringOption.Key)
                .Get<ConnectionStringOption>();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlServer(
                connectionStrings!.SqlServer,
                sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(RepositoryAssembly).Assembly.FullName);
                });

            return new AppDbContext(optionsBuilder.Options);
        }

        private static string ResolveApiProjectPath(string currentDirectory)
        {
            var candidates = new[]
            {
                currentDirectory,
                Path.Combine(currentDirectory, "AutoStock.API"),
                Path.Combine(currentDirectory, "../AutoStock.API"),
                Path.Combine(currentDirectory, "AutoStockAPI"),
                Path.Combine(currentDirectory, "../AutoStockAPI")
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);

                if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
                    return fullPath;
            }

            throw new FileNotFoundException("AutoStock.API appsettings.json bulunamadı.");
        }
    }
}