using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Application.Features.Admin.Achievements;
using DraftPuck.Application.Features.Games;
using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Firebase;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Tests.Application.Achievements;

/// <summary>The admin backfill: who gets queued, and what a backfill check awards (and doesn't announce).</summary>
[Collection(DatabaseCollection.Name)]
public class AchievementBackfillTests(DatabaseFixture fixture)
{
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();

    private AchievementAwardService AwardService(DraftPuckContext db)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton<IGameCache>(new InMemoryGameCache()).BuildServiceProvider();
        var rules = typeof(BaseAchievementRule).Assembly.GetTypes()
            .Where(t => typeof(IAchievementRule).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (IAchievementRule)ActivatorUtilities.CreateInstance(services, t));
        return new AchievementAwardService(db, rules, NullLogger<AchievementAwardService>.Instance, new UserRepository(db));
    }

    private async Task CheckAsync(Guid userId, bool isBackfill, AchievementTriggerType trigger = AchievementTriggerType.LobbyCreated, Guid? backfillId = null)
    {
        if (isBackfill && backfillId == null)
            backfillId = await NewRunAsync(usersQueued: 1);

        await using var db = fixture.CreateContext();
        await new CheckAndAwardAllAchievementsCommandHandler(AwardService(db), _push, db)
            .Handle(new CheckAndAwardAllAchievementsCommand { UserId = userId, TriggerType = trigger, BackfillId = isBackfill ? backfillId : null }, default);
    }

    private async Task<Guid> NewRunAsync(int usersQueued)
    {
        await using var db = fixture.CreateContext();
        var run = new AchievementBackfillEntity { Id = Guid.NewGuid(), StartedUtc = DateTime.UtcNow, UsersQueued = usersQueued };
        db.AchievementBackfills.Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    private async Task<AchievementBackfillEntity> RunAsync(Guid id)
    {
        await using var db = fixture.CreateContext();
        return await db.AchievementBackfills.SingleAsync(b => b.Id == id);
    }

    private async Task<string[]> OwnedAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return [.. await db.UserAchievements.Where(ua => ua.UserId == userId).Select(ua => ua.Achievement.UniqueIdentifier).ToListAsync()];
    }

    /// <summary>A user who hosts a lobby and joined someone else's, with a push token and achievement pushes on.</summary>
    private async Task<Guid> PlayerAsync()
    {
        await using var db = fixture.CreateContext();
        var s = new Seed(db);
        var (own, host) = s.Lobby([Seed.NewGameId()]);
        var (other, _) = s.Lobby([Seed.NewGameId()]);
        var user = await db.Users.FindAsync(host.UserId);
        user!.FcmRegistrationToken = "token";
        user.AchievementAwardedNotificationPreference = NotificationPreference.All;
        user.DiscordUserId = "123"; // linked Discord, but nothing says they joined the server
        s.Member(other, user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task ABackfillChecksEveryRuleAtOnceAndSendsNoNotifications()
    {
        var user = await PlayerAsync();

        await CheckAsync(user, isBackfill: true);

        var owned = await OwnedAsync(user);
        Assert.Contains("rookie_host", owned);   // a LobbyCreated rule
        Assert.Contains("party_crasher", owned); // a LobbyJoined rule, earned in the same check
        await _push.DidNotReceiveWithAnyArgs().SendAchievementNotification(default!, default!, default);
    }

    [Fact]
    public async Task EachBackfillCheckCountsTowardItsRun()
    {
        var first = await PlayerAsync();
        var second = await PlayerAsync();
        var run = await NewRunAsync(usersQueued: 2);

        await CheckAsync(first, isBackfill: true, backfillId: run);
        var halfway = await RunAsync(run);
        await CheckAsync(second, isBackfill: true, backfillId: run);
        var done = await RunAsync(run);

        Assert.Equal(1, halfway.UsersChecked);
        Assert.Equal(2, done.UsersChecked);
        // Each player earned at least Rookie Host and Party Crasher, and the run counted what was awarded.
        Assert.Equal((await OwnedAsync(first)).Length + (await OwnedAsync(second)).Length, done.AchievementsAwarded);
        Assert.True(AchievementBackfillDto.From(done).IsComplete);
        Assert.False(AchievementBackfillDto.From(halfway).IsComplete);
    }

    [Fact]
    public async Task ABackfillSkipsRulesThatOnlyTheirEventCanProve()
    {
        var user = await PlayerAsync();

        await CheckAsync(user, isBackfill: true);

        Assert.DoesNotContain("certified_chirper", await OwnedAsync(user));
    }

    [Fact]
    public async Task ANormalCheckStillOnlyRunsItsTriggersRulesAndNotifies()
    {
        var user = await PlayerAsync();

        await CheckAsync(user, isBackfill: false, AchievementTriggerType.LobbyCreated);

        var owned = await OwnedAsync(user);
        Assert.Contains("rookie_host", owned);
        Assert.DoesNotContain("party_crasher", owned);
        await _push.ReceivedWithAnyArgs().SendAchievementNotification(default!, default!, default);
    }

    [Fact]
    public async Task TheBackfillQueuesEveryoneWhoPlayedOrLinkedDiscordButNotBotsOrUnusedGuests()
    {
        Guid player, discordOnly, bot, unusedGuest;
        await using (var db = fixture.CreateContext())
        {
            var s = new Seed(db);
            var (lobby, host) = s.Lobby([Seed.NewGameId()]);
            player = host.UserId;
            bot = s.Bot(lobby).UserId;
            var linked = s.User();
            linked.DiscordUserId = "456";
            discordOnly = linked.Id;
            unusedGuest = s.User().Id;
            await db.SaveChangesAsync();
        }

        var queue = Substitute.For<IAchievementQueueService>();
        AchievementBackfillDto run;
        await using (var db = fixture.CreateContext())
            run = await new BackfillAchievementsCommandHandler(db, queue, NullLogger<BackfillAchievementsCommandHandler>.Instance).Handle(new BackfillAchievementsCommand(), default);

        var queued = queue.ReceivedCalls().Select(c => c.GetArguments()[0]).OfType<CheckAchievementsMessage>().ToList();
        Assert.All(queued, m => Assert.Equal(run.Id, m.BackfillId));
        Assert.Equal(queued.Count, run.UsersQueued);
        Assert.Equal(0, run.UsersChecked);

        await using (var db = fixture.CreateContext())
        {
            var latest = await new BackfillAchievementsCommandHandler(db, queue, NullLogger<BackfillAchievementsCommandHandler>.Instance)
                .Handle(new GetLatestAchievementBackfillQuery(), default);
            Assert.Equal(run.Id, latest!.Id);
        }
        var users = queued.Select(m => m.UserId).ToHashSet();
        Assert.Contains(player, users);
        Assert.Contains(discordOnly, users);
        Assert.DoesNotContain(bot, users);
        Assert.DoesNotContain(unusedGuest, users);
    }
}
