using Testcontainers.MsSql;

namespace DraftPuck.Tests.Support;

/// <summary>
/// One SQL Server container per test run, with the real migrations applied, so tests exercise the same
/// query translation and constraints as production. Requires Docker.
/// Tests share the database, so each one seeds its own lobbies and uses its own game IDs (see <see cref="Seed"/>).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public DraftPuckContext CreateContext() => new(
        new DbContextOptionsBuilder<DraftPuckContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options);

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
