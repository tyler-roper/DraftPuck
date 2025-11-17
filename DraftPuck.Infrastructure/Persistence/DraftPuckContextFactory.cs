using DotNetEnv;
using Microsoft.EntityFrameworkCore.Design;

namespace DraftPuck.Infrastructure.Persistence;

public class DraftPuckContextFactory : IDesignTimeDbContextFactory<DraftPuckContext>
{
    public DraftPuckContext CreateDbContext(string[] args)
    {
        var solutionRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
        var envFile = Path.Combine(solutionRoot, "env.development.list");

        if (!File.Exists(envFile))
            throw new FileNotFoundException("Env file not found", envFile);

        Env.Load(envFile);

        var connectionString = Environment.GetEnvironmentVariable("DRAFTPUCK_ConnectionStrings__LocalConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string not found after loading .env file.");

        var optionsBuilder = new DbContextOptionsBuilder<DraftPuckContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DraftPuckContext(optionsBuilder.Options);
    }
}
