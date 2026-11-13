using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DraftPuck.Tests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public class PersistenceTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task FalseIsSavedForBoolsThatDefaultToTrue()
    {
        // EF leaves a property out of the INSERT when it equals its sentinel, letting the database default apply.
        // These default to true, so false must be sent explicitly.
        var userId = Guid.NewGuid();
        Guid lobbyId, pickId;
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(new UserEntity { Id = userId, IsGuest = false, IsActive = false });
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([Seed.NewGameId()], isActive: false);
            var pick = seed.Pick(host, lobby.GameIds[0], TestGames.HomeTeamId, TestGames.HomePlayer(0));
            pick.IsActive = false;
            (lobbyId, pickId) = (lobby.Id, pick.Id);
            await db.SaveChangesAsync();
        }

        await using var check = fixture.CreateContext();
        var user = await check.Users.SingleAsync(u => u.Id == userId);
        Assert.False(user.IsGuest);
        Assert.False(user.IsActive);
        Assert.False((await check.Lobbies.SingleAsync(l => l.Id == lobbyId)).IsActive);
        Assert.False((await check.LobbyMemberPicks.SingleAsync(p => p.Id == pickId)).IsActive);
    }

    [Fact]
    public async Task TrueDefaultsStillApplyWhenNotSet()
    {
        var userId = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
        {
            db.Users.Add(new UserEntity { Id = userId });
            await db.SaveChangesAsync();
        }

        await using var check = fixture.CreateContext();
        var user = await check.Users.SingleAsync(u => u.Id == userId);
        Assert.True(user.IsGuest);
        Assert.True(user.IsActive);
    }

    /// <summary>
    /// Migrations written without the generated Designer file must carry [Migration] themselves, or EF never sees them.
    /// </summary>
    private static readonly string[] KnownUndiscoverableMigrations =
    [
        // Never applied anywhere. Turning it on needs a decision first: its unique email index fails if
        // production has duplicate emails. See the review notes on fix/review-findings.
        "AddPerformanceIndexes"
    ];

    [Fact]
    public void EveryMigrationIsDiscoverable()
    {
        var undiscoverable = typeof(DraftPuckContext).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract && t.GetCustomAttribute<MigrationAttribute>() == null)
            .Select(t => t.Name)
            .Except(KnownUndiscoverableMigrations);

        Assert.Empty(undiscoverable);
    }

    [Fact]
    public async Task GuestFlagCorrectionIsApplied()
    {
        await using var db = fixture.CreateContext();

        Assert.Contains("20261112230000_ClearGuestFlagOnRegisteredUsers", await db.Database.GetAppliedMigrationsAsync());
    }
}
