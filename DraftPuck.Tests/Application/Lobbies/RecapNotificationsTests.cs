using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Lobbies.Recap;
using DraftPuck.Domain.Achievements;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>The day-after recap push: who gets it, when, and what it says.</summary>
[Collection(DatabaseCollection.Name)]
public class RecapNotificationsTests(DatabaseFixture fixture)
{
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private static int _eventId = 900_000;

    private static string TokenOf(Guid userId) => "token-" + userId;

    private async Task SendAsync(DateTime nowUtc)
    {
        await using var db = fixture.CreateContext();
        var options = new ApplicationOptions { IsTestMode = true, TestModeStartDateTimeUtc = nowUtc.Subtract(ApplicationOptions.TimeSinceStartup) };
        await new SendRecapNotificationsCommandHandler(db, _mediator, _push, Options.Create(options), NullLogger<SendRecapNotificationsCommandHandler>.Instance)
            .Handle(new SendRecapNotificationsCommand(), default);
    }

    /// <summary>The recap pushes sent for these lobbies: (code, body, tokens).</summary>
    private List<(string Code, string Body, IReadOnlyCollection<string> Tokens)> Sent(params string[] codes) =>
    [
        .. _push.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IPushNotificationService.SendRecapNotification))
            .Select(c => c.GetArguments())
            .Where(a => codes.Contains((string)a[0]!))
            .Select(a => ((string)a[0]!, (string)a[2]!, (IReadOnlyCollection<string>)a[3]!))
    ];

    private static void WithDevice(DraftPuckContext db, UserEntity user) =>
        db.UserDevices.Add(new UserDeviceEntity { UserId = user.Id, Token = TokenOf(user.Id), LastSeenUtc = DateTime.UtcNow });

    /// <summary>An ended lobby created at <paramref name="created"/>, its host on a device, with this many drinks.</summary>
    private async Task<(string Code, LobbyMemberEntity Host, LobbyEntity Lobby)> LobbyAsync(DateTime created, int drinks)
    {
        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var gameId = Seed.NewGameId();
        var (lobby, host) = seed.Lobby([gameId], isActive: false);
        lobby.Created = created;
        var pick = seed.Pick(host, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
        for (var i = 0; i < drinks; i++)
            db.Drinks.Add(new DrinkEntity { Id = Guid.NewGuid(), LobbyMemberPickId = pick.Id, EventId = Interlocked.Increment(ref _eventId), Created = created });
        WithDevice(db, (await db.Users.FindAsync(host.UserId))!);
        await db.SaveChangesAsync();
        return (lobby.JoinCode, host, lobby);
    }

    [Fact]
    public async Task AtNoonEasternLastNightsPlayersHearWhatTheyWonAndEarned()
    {
        // 17:00 UTC in January is noon in New York; last night ran from 10:00 UTC yesterday to 10:00 UTC today.
        var now = new DateTime(2027, 1, 15, 17, 0, 0, DateTimeKind.Utc);
        var (code, sam, _) = await LobbyAsync(new DateTime(2027, 1, 15, 1, 0, 0, DateTimeKind.Utc), drinks: 2);
        var (quietCode, _, _) = await LobbyAsync(new DateTime(2027, 1, 15, 1, 0, 0, DateTimeKind.Utc), drinks: 0);
        var (oldCode, _, _) = await LobbyAsync(new DateTime(2027, 1, 13, 1, 0, 0, DateTimeKind.Utc), drinks: 2);

        Guid alexUserId, botUserId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var lobby = await db.Lobbies.SingleAsync(l => l.JoinCode == code);
            // Alex turned the recap push off; the bot has no one to tell.
            var alex = seed.User();
            alex.RecapNotificationPreference = NotificationPreference.None;
            WithDevice(db, alex);
            seed.Member(lobby, alex);
            var bot = seed.Bot(lobby);
            botUserId = bot.UserId;
            alexUserId = alex.Id;

            // Sam's lobby-close achievement, awarded quietly this morning; one from before the night doesn't count.
            var cup = new AchievementEntity { Id = Guid.NewGuid(), UniqueIdentifier = $"recap-push-{Guid.NewGuid():N}", FriendlyName = "The Cup", Description = "Won the lobby" };
            var old = new AchievementEntity { Id = Guid.NewGuid(), UniqueIdentifier = $"recap-push-{Guid.NewGuid():N}", FriendlyName = "Old News", Description = "Earned earlier" };
            db.Achievements.AddRange(cup, old);
            db.UserAchievements.AddRange(
                new UserAchievementEntity { Id = Guid.NewGuid(), UserId = sam.UserId, AchievementId = cup.Id, DateEarned = new DateTime(2027, 1, 15, 10, 5, 0, DateTimeKind.Utc) },
                new UserAchievementEntity { Id = Guid.NewGuid(), UserId = sam.UserId, AchievementId = old.Id, DateEarned = new DateTime(2027, 1, 14, 3, 0, 0, DateTimeKind.Utc) });
            await db.SaveChangesAsync();
        }

        _mediator.Send(Arg.Is<GetLobbyRecapQuery>(q => q.Code == code), Arg.Any<CancellationToken>()).Returns(new LobbyRecapDto
        {
            Awards = [new LobbyAwardDto { Key = LobbyAwards.BestPick, Title = "Best pick", MemberId = sam.Id, MemberName = "Sam", Detail = "" }]
        });

        await SendAsync(now);

        var sent = Assert.Single(Sent(code, quietCode, oldCode));
        Assert.Equal((code, "You won Best pick. You earned The Cup. See the awards and stats."), (sent.Code, sent.Body));
        Assert.Equal([TokenOf(sam.UserId)], sent.Tokens);
        Assert.DoesNotContain(Sent(code), s => s.Tokens.Contains(TokenOf(alexUserId)) || s.Tokens.Contains(TokenOf(botUserId)));
    }

    [Theory]
    [InlineData(1, 17, true)]  // January: noon Eastern is 17:00 UTC
    [InlineData(1, 16, false)]
    [InlineData(7, 16, true)]  // July (daylight saving): 16:00 UTC
    [InlineData(7, 17, false)]
    public async Task ItSendsOnlyAtNoonInNewYork(int month, int utcHour, bool sends)
    {
        var day = new DateTime(2028, month, 10, 0, 0, 0, DateTimeKind.Utc);
        var (code, _, _) = await LobbyAsync(day.AddHours(1), drinks: 1);

        await SendAsync(day.AddHours(utcHour));

        Assert.Equal(sends ? 1 : 0, Sent(code).Count);
    }

    [Fact]
    public async Task SomeoneInSeveralLobbiesGetsOnePushForTheBusiest()
    {
        var night = new DateTime(2029, 2, 1, 1, 0, 0, DateTimeKind.Utc);
        var (quiet, host, _) = await LobbyAsync(night, drinks: 1);
        string busy;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var gameId = Seed.NewGameId();
            var (lobby, _) = seed.Lobby([gameId], isActive: false);
            lobby.Created = night;
            var member = seed.Member(lobby, await db.Users.FindAsync(host.UserId));
            var pick = seed.Pick(member, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
            for (var i = 0; i < 3; i++)
                db.Drinks.Add(new DrinkEntity { Id = Guid.NewGuid(), LobbyMemberPickId = pick.Id, EventId = Interlocked.Increment(ref _eventId), Created = night });
            await db.SaveChangesAsync();
            busy = lobby.JoinCode;
        }

        await SendAsync(new DateTime(2029, 2, 1, 17, 0, 0, DateTimeKind.Utc));

        var sent = Assert.Single(Sent(quiet, busy), s => s.Tokens.Contains(TokenOf(host.UserId)));
        Assert.Equal(busy, sent.Code);
    }

    [Theory]
    [InlineData(new string[0], new string[0], "Casey won Thirstiest. See the awards and stats.")]
    [InlineData(new[] { "Best pick", "Bully" }, new string[0], "You won Best pick and Bully. See the awards and stats.")]
    [InlineData(new string[0], new[] { "The Cup", "Homer", "Goose Egg" }, "You earned The Cup, Homer and Goose Egg. See the awards and stats.")]
    public void ThePushNamesWhatTheyWonOrElseTeasesSomeoneElsesAward(string[] won, string[] earned, string expected)
    {
        var teaser = new LobbyAwardDto { Key = LobbyAwards.Thirstiest, Title = "Thirstiest", MemberName = "Casey", Detail = "" };
        Assert.Equal(expected, RecapPushText.Body(won, earned, teaser));
    }
}
