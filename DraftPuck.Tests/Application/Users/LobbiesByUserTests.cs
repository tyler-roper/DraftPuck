namespace DraftPuck.Tests.Application.Users;

/// <summary>A user's lobbies: their history, or just the live ones (for rejoining from the home page).</summary>
[Collection(DatabaseCollection.Name)]
public class LobbiesByUserTests(DatabaseFixture fixture)
{
    private async Task<List<LobbySummaryDto>> LobbiesOfAsync(Guid userId, bool activeOnly)
    {
        await using var db = fixture.CreateContext();
        return await new GetLobbiesByUserQueryHandler(db).Handle(new GetLobbiesByUserQuery { UserId = userId, ActiveOnly = activeOnly }, default);
    }

    [Fact]
    public async Task LiveOnlyLeavesOutEndedLobbiesAndOnesYouWereRemovedFrom()
    {
        Guid me;
        string tonight, ended, removedFrom;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var user = seed.User();
            me = user.Id;
            var (live, _) = seed.Lobby([Seed.NewGameId()]);
            seed.Member(live, user);
            var (old, _) = seed.Lobby([Seed.NewGameId()], isActive: false);
            seed.Member(old, user);
            var (kicked, _) = seed.Lobby([Seed.NewGameId()]);
            seed.Member(kicked, user).IsRemoved = true;
            (tonight, ended, removedFrom) = (live.JoinCode, old.JoinCode, kicked.JoinCode);
            await db.SaveChangesAsync();
        }

        Assert.Equal([tonight], (await LobbiesOfAsync(me, activeOnly: true)).Select(l => l.JoinCode));
        var all = (await LobbiesOfAsync(me, activeOnly: false)).Select(l => l.JoinCode).ToList();
        Assert.Contains(ended, all);
        Assert.DoesNotContain(removedFrom, all);
    }

    [Fact]
    public async Task ThePlayerCountLeavesOutRemovedMembers()
    {
        Guid me;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([Seed.NewGameId()]);
            me = host.UserId;
            seed.Member(lobby);
            seed.Bot(lobby);
            seed.Member(lobby).IsRemoved = true;
            await db.SaveChangesAsync();
        }

        var summary = Assert.Single(await LobbiesOfAsync(me, activeOnly: true));
        // Host, member and bot; not the removed member.
        Assert.Equal(3, summary.MemberCount);
        Assert.Equal(me, summary.CreatedBy);
    }
}
