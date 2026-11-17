using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies;
using DraftPuck.Application.Features.Lobbies.Callouts;
using DraftPuck.Application.Features.Lobbies.Events;

namespace DraftPuck.Tests.Application.Lobbies;

[Collection(DatabaseCollection.Name)]
public class LobbyCalloutTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private int _eventId;

    private record Lobby(Guid Id, string Code, int GameId, LobbyMemberEntity Sam, LobbyMemberEntity Alex, LobbyMemberEntity Jo);

    private async Task<Lobby> LobbyAsync()
    {
        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var gameId = Seed.NewGameId();
        var (lobby, sam) = seed.Lobby([gameId], picksPerTeam: 3);
        sam.Name = "Sam";
        var alex = seed.Member(lobby);
        alex.Name = "Alex";
        var jo = seed.Member(lobby);
        jo.Name = "Jo";
        await db.SaveChangesAsync();
        return new Lobby(lobby.Id, lobby.JoinCode, gameId, sam, alex, jo);
    }

    /// <summary>A pick for <paramref name="member"/>, plus drinks it earned <paramref name="minutesAgo"/>, handed to <paramref name="to"/> if given.</summary>
    private async Task<LobbyMemberPickEntity> PickAsync(Lobby lobby, LobbyMemberEntity member, int drinks = 0, int minutesAgo = 1, LobbyMemberEntity? to = null)
    {
        await using var db = fixture.CreateContext();
        var pick = new Seed(db).Pick(member, lobby.GameId, TestGames.HomeTeamId, Random.Shared.Next(1000, 9999));
        var at = DateTime.UtcNow.AddMinutes(-minutesAgo);
        for (var i = 0; i < drinks; i++)
            db.Drinks.Add(new DrinkEntity
            {
                Id = Guid.NewGuid(), LobbyMemberPickId = pick.Id, EventId = ++_eventId, Created = at,
                RecipientLobbyMemberId = to?.Id, Assigned = to == null ? null : at
            });
        await db.SaveChangesAsync();
        return pick;
    }

    private async Task FeedEventAsync(Lobby lobby, LobbyEventType type, LobbyMemberEntity member, LobbyMemberEntity? member2 = null)
    {
        await using var db = fixture.CreateContext();
        var e = new LobbyEventEntity { LobbyId = lobby.Id, LobbyEventType = type, LobbyMemberId = member.Id, LobbyMember2Id = member2?.Id, Title = "", Text = "" };
        await Handler(db).Handle(new LobbyEventCreatedNotification(e, lobby.Code), default);
    }

    private async Task GameUpdatedAsync(GameDto game)
    {
        await using var db = fixture.CreateContext();
        await Handler(db).Handle(new GameUpdatedNotification(game), default);
    }

    private LobbyCalloutHandler Handler(DraftPuckContext db) =>
        new(db, new LobbyEventWriter(db, _mediator), NullLogger<LobbyCalloutHandler>.Instance);

    private async Task<List<LobbyEventEntity>> CalloutsAsync(Lobby lobby, LobbyEventType type)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyEvents.Where(e => e.LobbyId == lobby.Id && e.LobbyEventType == type).ToListAsync();
    }

    // --- On fire ---

    [Fact]
    public async Task OnFireAfterThreeDrinksInTwentyMinutes()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam, drinks: 1, minutesAgo: 40); // too long ago
        await PickAsync(lobby, lobby.Sam, drinks: 2, minutesAgo: 10);

        await FeedEventAsync(lobby, LobbyEventType.DrinkAwarded, lobby.Sam);
        Assert.Empty(await CalloutsAsync(lobby, LobbyEventType.OnFire));

        await PickAsync(lobby, lobby.Sam, drinks: 1, minutesAgo: 0);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAwarded, lobby.Sam);

        var onFire = Assert.Single(await CalloutsAsync(lobby, LobbyEventType.OnFire));
        Assert.Equal(("On Fire", lobby.Sam.Id), (onFire.Title, onFire.LobbyMemberId));
        Assert.Contains("{{name}}", onFire.Text);
        Assert.Contains("3 drinks in 1", onFire.Text); // within about 10 minutes
        await _mediator.Received().Publish(Arg.Is<LobbyEventCreatedNotification>(n => n.Event.Id == onFire.Id && n.JoinCode == lobby.Code), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnFireOnlyOncePerCooldown()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam, drinks: 3);

        await FeedEventAsync(lobby, LobbyEventType.DrinkAwarded, lobby.Sam);
        await PickAsync(lobby, lobby.Sam, drinks: 1, minutesAgo: 0);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAwarded, lobby.Sam);

        Assert.Single(await CalloutsAsync(lobby, LobbyEventType.OnFire));
    }

    // --- Drinking streak ---

    [Fact]
    public async Task DrinkingStreakAfterThreeDrinksTakenInTenMinutes()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam, drinks: 2, minutesAgo: 20, to: lobby.Jo); // too long ago
        await PickAsync(lobby, lobby.Sam, drinks: 2, minutesAgo: 5, to: lobby.Jo);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAssigned, lobby.Sam, lobby.Jo);
        Assert.Empty(await CalloutsAsync(lobby, LobbyEventType.DrinkingStreak));

        await PickAsync(lobby, lobby.Alex, drinks: 1, minutesAgo: 0, to: lobby.Jo);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAssigned, lobby.Alex, lobby.Jo);

        var streak = Assert.Single(await CalloutsAsync(lobby, LobbyEventType.DrinkingStreak));
        Assert.Equal(lobby.Jo.Id, streak.LobbyMemberId);
        Assert.Contains("{{name}}", streak.Text);
        Assert.Contains("3 drinks in", streak.Text);
    }

    // --- Rivalry ---

    [Fact]
    public async Task RivalryOnceBothHaveGivenTwoAndOnlyOnce()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam, drinks: 3, to: lobby.Alex);
        await PickAsync(lobby, lobby.Alex, drinks: 1, to: lobby.Sam);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAssigned, lobby.Alex, lobby.Sam);
        Assert.Empty(await CalloutsAsync(lobby, LobbyEventType.RivalryBrewing)); // one-sided so far

        await PickAsync(lobby, lobby.Alex, drinks: 1, to: lobby.Sam);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAssigned, lobby.Alex, lobby.Sam);
        await FeedEventAsync(lobby, LobbyEventType.DrinkAssigned, lobby.Sam, lobby.Alex);

        var rivalry = Assert.Single(await CalloutsAsync(lobby, LobbyEventType.RivalryBrewing));
        Assert.Equal((lobby.Alex.Id, lobby.Sam.Id), (rivalry.LobbyMemberId, rivalry.LobbyMember2Id));
        Assert.Contains("2–3", rivalry.Text); // Alex's 2 to Sam's 3
    }

    // --- Ice cold ---

    private GameDto Game(Lobby lobby, int period, int minutesLeft, GameState state = GameState.Live)
    {
        var game = TestGames.Create(lobby.GameId, DateTime.UtcNow.AddHours(-2), state);
        game.Period = period;
        game.PeriodType = PeriodType.Regulation;
        game.MinutesRemainingInPeriod = minutesLeft;
        return game;
    }

    [Fact]
    public async Task IceColdLateInTheThirdForTheMemberWithTheMostScorelessPicks()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam);
        await PickAsync(lobby, lobby.Sam);
        await PickAsync(lobby, lobby.Jo);
        await PickAsync(lobby, lobby.Jo);
        await PickAsync(lobby, lobby.Jo);
        await PickAsync(lobby, lobby.Alex);
        await PickAsync(lobby, lobby.Alex, drinks: 1); // Alex scored: not ice cold

        await GameUpdatedAsync(Game(lobby, period: 2, minutesLeft: 3));
        await GameUpdatedAsync(Game(lobby, period: 3, minutesLeft: 12));
        Assert.Empty(await CalloutsAsync(lobby, LobbyEventType.IceCold));

        await GameUpdatedAsync(Game(lobby, period: 3, minutesLeft: 4));
        await GameUpdatedAsync(Game(lobby, period: 3, minutesLeft: 2));

        var cold = Assert.Single(await CalloutsAsync(lobby, LobbyEventType.IceCold));
        Assert.Equal((lobby.Jo.Id, lobby.GameId), (cold.LobbyMemberId, cold.GameId));
        Assert.Contains("{{name}}", cold.Text);
        Assert.Contains("AWY @ HOM", cold.Text);
    }

    [Fact]
    public async Task IceColdAtTheFinalIfNotCalledEarlier()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam);
        await PickAsync(lobby, lobby.Sam);

        await GameUpdatedAsync(Game(lobby, period: 3, minutesLeft: 0, GameState.Final));

        // Either wording: "went 0 for 2" or "2 picks in ..., not one goal".
        var text = Assert.Single(await CalloutsAsync(lobby, LobbyEventType.IceCold)).Text;
        Assert.Contains("{{name}}", text);
        Assert.True(text.Contains("0 for 2") || text.Contains("2 picks"), text);
    }

    [Fact]
    public async Task NoIceColdWhenEveryoneScoredOrHadOnePick()
    {
        var lobby = await LobbyAsync();
        await PickAsync(lobby, lobby.Sam);
        await PickAsync(lobby, lobby.Alex, drinks: 1);
        await PickAsync(lobby, lobby.Alex);

        await GameUpdatedAsync(Game(lobby, period: 3, minutesLeft: 1));

        Assert.Empty(await CalloutsAsync(lobby, LobbyEventType.IceCold));
    }
}
