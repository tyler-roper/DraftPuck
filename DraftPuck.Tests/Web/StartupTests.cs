using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace DraftPuck.Tests.Web;

[Collection(ApiCollection.Name)]
public class StartupTests(ApiFactory factory)
{
    [Fact]
    public async Task StartupFailsWhenMigrationsFail()
    {
        // A new database that already has a table the first migration creates, so migrating it fails.
        var database = new SqlConnectionStringBuilder(factory.SqlConnectionString) { InitialCatalog = $"StartupFails_{Guid.NewGuid():N}" };
        await using (var master = new SqlConnection(factory.SqlConnectionString))
        {
            await master.OpenAsync();
            await new SqlCommand($"CREATE DATABASE [{database.InitialCatalog}]", master).ExecuteNonQueryAsync();
        }
        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await new SqlCommand("CREATE TABLE Achievements (Id int)", connection).ExecuteNonQueryAsync();
        }

        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            ApiFactory.UseTestSettings(builder, database.ConnectionString, factory.RedisConnectionString));

        var error = Assert.ThrowsAny<Exception>(() => app.Server);

        Assert.Contains("There is already an object named 'Achievements'", error.GetBaseException().Message);
    }
}
