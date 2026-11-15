using DraftPuck.Application.Features.Achievements;
using DraftPuck.Shared.Achievements;

namespace DraftPuck.Tests.Application.Achievements;

/// <summary>Who gets an achievement check queued, and with which trigger, when things happen in a lobby.</summary>
[Collection(DatabaseCollection.Name)]
public class AchievementTriggerTests(DatabaseFixture fixture)
{
    private readonly IAchievementQueueService _queue = Substitute.For<IAchievementQueueService>();

    private List<(Guid UserId, AchievementTriggerType Trigger)> Queued()
        => [.. _queue.ReceivedCalls()
            .Select(c => c.GetArguments()[0])
            .OfType<CheckAchievementsMessage>()
            .Select(m => (m.UserId, m.TriggerType))];

    private static (LobbyEntity Lobby, LobbyMemberEntity Host, LobbyMemberEntity Human, LobbyMemberEntity Bot) Lobby()
    {
        var hostUser = Guid.NewGuid();
        var lobby = new LobbyEntity { Id = Guid.NewGuid(), CreatedBy = hostUser, JoinCode = Seed.NewJoinCode() };
        var host = new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = hostUser, Name = "Host" };
        var human = new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = Guid.NewGuid(), Name = "Human" };
        var bot = new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = Guid.NewGuid(), Name = "Bot", IsBot = true };
        return (lobby, host, human, bot);
    }

    [Fact]
    public async Task JoiningChecksTheJoinerAndTheHost()
    {
        var (lobby, host, human, _) = Lobby();

        await new LobbyJoinedAchievementHandler(_queue).Handle(new UserJoinedLobbyNotification(new(lobby, human)), default);

        Assert.Equal([(human.UserId, AchievementTriggerType.LobbyJoined), (host.UserId, AchievementTriggerType.HostedLobbyJoined)], Queued());
    }

    [Fact]
    public async Task BotsJoiningAndTheHostCreatingCheckNobody()
    {
        var (lobby, host, _, bot) = Lobby();
        var handler = new LobbyJoinedAchievementHandler(_queue);

        await handler.Handle(new UserJoinedLobbyNotification(new(lobby, bot)), default);
        await handler.Handle(new UserJoinedLobbyNotification(new(lobby, host)), default);

        Assert.Empty(Queued());
    }

    [Fact]
    public async Task ABotsDrinkChecksTheRecipientAndTheHostNotTheBot()
    {
        var (lobby, host, human, bot) = Lobby();

        await new DrinkAssignedAchievementHandler(_queue).Handle(new DrinkAssignedNotification(new(lobby, bot, human, new DrinkEntity())), default);

        Assert.Equal([(human.UserId, AchievementTriggerType.DrinkAssigned), (host.UserId, AchievementTriggerType.DrinkAssigned)], Queued());
    }

    [Fact]
    public async Task AHumansDrinkChecksGiverAndRecipient()
    {
        var (lobby, host, human, _) = Lobby();

        await new DrinkAssignedAchievementHandler(_queue).Handle(new DrinkAssignedNotification(new(lobby, human, host, new DrinkEntity())), default);

        Assert.Equal([(human.UserId, AchievementTriggerType.DrinkAssigned), (host.UserId, AchievementTriggerType.DrinkAssigned)], Queued());
    }

    [Fact]
    public async Task BotsScoringOrRenamedCheckNobody()
    {
        var (lobby, _, _, bot) = Lobby();

        await new DrinkAwardedAchievementHandler(_queue).Handle(new DrinkAwardedNotification(new(lobby, bot, new DrinkEntity())), default);
        await new NameChangedAchievementHandler(_queue).Handle(new UserNameChangedNotification(new(lobby, bot, "Old")), default);

        Assert.Empty(Queued());
    }

    [Fact]
    public async Task ANameChangeChecksThatMember()
    {
        var (lobby, _, human, _) = Lobby();

        await new NameChangedAchievementHandler(_queue).Handle(new UserNameChangedNotification(new(lobby, human, "Old")), default);

        Assert.Equal([(human.UserId, AchievementTriggerType.NameChanged)], Queued());
    }

    [Fact]
    public async Task AnInvalidatedDrinkChecksTheGiverAndTheDrinkerButNotBots()
    {
        var (lobby, host, human, bot) = Lobby();
        var handler = new DrinkInvalidatedAchievementHandler(_queue);

        await handler.Handle(new DrinkInvalidatedNotification(new(lobby, human, host, new DrinkEntity())), default);
        await handler.Handle(new DrinkInvalidatedNotification(new(lobby, bot, human, new DrinkEntity())), default);

        Assert.Equal([
            (human.UserId, AchievementTriggerType.DrinkInvalidated),
            (host.UserId, AchievementTriggerType.DrinkInvalidated),
            (human.UserId, AchievementTriggerType.DrinkInvalidated)], Queued());
    }

    [Fact]
    public async Task AnEndedLobbyChecksItsCurrentHumans()
    {
        Guid lobbyId, host, human, removed, bot;
        await using (var db = fixture.CreateContext())
        {
            var s = new Seed(db);
            var (lobby, h) = s.Lobby([Seed.NewGameId()], isActive: false);
            (lobbyId, host, human) = (lobby.Id, h.UserId, s.Member(lobby).UserId);
            var gone = s.Member(lobby);
            gone.IsRemoved = true;
            removed = gone.UserId;
            bot = s.Bot(lobby).UserId;
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
            await new LobbiesEndedAchievementHandler(_queue, db).Handle(new LobbiesEndedNotification([lobbyId]), default);

        var queued = Queued();
        Assert.Equal(2, queued.Count);
        Assert.All(queued, q => Assert.Equal(AchievementTriggerType.LobbyEnded, q.Trigger));
        Assert.Equal(new[] { host, human }.Order(), queued.Select(q => q.UserId).Order());
        Assert.DoesNotContain(queued, q => q.UserId == removed || q.UserId == bot);
    }
}
