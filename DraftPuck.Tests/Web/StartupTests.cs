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

    [Fact]
    public async Task TheAppStartsAndServesWithTelemetryOn()
    {
        // Production has a connection string; tests normally don't, so this is the only run of the exporter setup.
        // The endpoint is unreachable, so exports fail quietly in the background.
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            ApiFactory.UseTestSettings(builder, factory.SqlConnectionString, factory.RedisConnectionString);
            builder.UseSetting("Telemetry:ConnectionString",
                "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://127.0.0.1:9/");
        });

        var response = await app.CreateClient().GetAsync("/health/live");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
