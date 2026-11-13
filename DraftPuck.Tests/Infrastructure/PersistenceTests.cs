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

    [Fact]
    public void EveryMigrationIsDiscoverable()
    {
        // A migration written by hand without the generated Designer file must carry [Migration] itself, or EF never
        // sees it. (The original AddPerformanceIndexes was never applied because of this.)
        var undiscoverable = typeof(DraftPuckContext).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract && t.GetCustomAttribute<MigrationAttribute>() == null)
            .Select(t => t.Name);

        Assert.Empty(undiscoverable);
    }

    [Fact]
    public async Task EveryMigrationIsApplied()
    {
        await using var db = fixture.CreateContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains("20261112230000_ClearGuestFlagOnRegisteredUsers", await db.Database.GetAppliedMigrationsAsync());
        Assert.Contains("20261113014702_AddPerformanceIndexes", await db.Database.GetAppliedMigrationsAsync());
    }

    [Theory]
    [InlineData("Users", "IX_Users_Email", true)]
    [InlineData("Lobbies", "IX_Lobbies_JoinCode", true)]
    [InlineData("LobbyMemberPicks", "IX_LobbyMemberPicks_Game_Player_Active", false)]
    [InlineData("Drinks", "IX_Drinks_Pick_Event", false)]
    public async Task PerformanceIndexesExist(string table, string index, bool unique)
    {
        await using var db = fixture.CreateContext();

        var found = await db.Database.SqlQueryRaw<bool>(
            "SELECT i.is_unique AS Value FROM sys.indexes i WHERE i.object_id = OBJECT_ID({0}) AND i.name = {1}", table, index).ToListAsync();

        Assert.Equal(unique, Assert.Single(found));
    }

    [Fact]
    public async Task EmailsMustBeUniqueButGuestsHaveNone()
    {
        await using var db = fixture.CreateContext();
        db.Users.AddRange(new UserEntity { Id = Guid.NewGuid() }, new UserEntity { Id = Guid.NewGuid() }); // two guests, no email
        await db.SaveChangesAsync();

        var email = $"{Guid.NewGuid():N}@example.com";
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Email = email });
        await db.SaveChangesAsync();
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Email = email });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
