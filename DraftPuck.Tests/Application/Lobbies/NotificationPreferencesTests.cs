using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Auth;
using DraftPuck.Application.Features.Lobbies;
using DraftPuck.Application.Features.Lobbies.Management;
using DraftPuck.Application.Features.Lobbies.Members;
using DraftPuck.Application.Features.PushNotifications;
using DraftPuck.Application.Features.Users;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Tests.Application.Lobbies;

/// <summary>
/// Notification settings are per lobby member, stamped from the user's defaults when they join; the push handlers
/// read the member's settings.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class NotificationPreferencesTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();

    private const NotificationPreference None = NotificationPreference.None;
    private const NotificationPreference Relevant = NotificationPreference.Relevant;
    private const NotificationPreference All = NotificationPreference.All;

    private static string TokenOf(UserEntity user) => "token-" + user.Id;

    /// <summary>A registered user with a push token and these defaults (drink received, drink awarded, chat, picking).</summary>
    private async Task<UserEntity> UserWithDefaultsAsync(NotificationPreference received, NotificationPreference awarded, NotificationPreference chat, NotificationPreference picking)
    {
        await using var db = fixture.CreateContext();
        var user = await SeedAccounts.AccountAsync(db);
        db.UserDevices.Add(new UserDeviceEntity { UserId = user.Id, Token = TokenOf(user), LastSeenUtc = DateTime.UtcNow });
        user.DrinkReceivedNotificationPreference = received;
        user.DrinkAwardedNotificationPreference = awarded;
        user.ChatNotificationPreference = chat;
        user.PickingStartedNotificationPreference = picking;
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<LobbyMemberEntity> MemberAsync(string code, Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyMembers.SingleAsync(m => m.Lobby.JoinCode == code && m.UserId == userId);
    }

    private static (NotificationPreference, NotificationPreference, NotificationPreference, NotificationPreference) Settings(LobbyMemberEntity m)
        => (m.DrinkReceivedNotificationPreference, m.DrinkAwardedNotificationPreference, m.ChatNotificationPreference, m.PickingStartedNotificationPreference);

    private async Task<LobbyDto> CreateLobbyAsync(Guid creatorId, int? gameId = null)
    {
        await using var db = fixture.CreateContext();
        return await new CreateLobbyCommandHandler(db, TestServices.Mapper(), _mediator).Handle(new CreateLobbyCommand
        {
            CreatorUserId = creatorId,
            Name = "Host",
            PicksPerTeam = 1,
            GameIds = [gameId ?? Seed.NewGameId()]
        }, default);
    }

    private async Task JoinAsync(string code, Guid userId)
    {
        await using var db = fixture.CreateContext();
        await new JoinLobbyCommandHandler(db, TestServices.Mapper(), _mediator).Handle(
            new JoinLobbyCommand { Code = code, UserId = userId, Name = "Joiner" + Guid.NewGuid().ToString("N")[..4] }, default);
    }

    private async Task SetLobbySettingsAsync(string code, Guid userId, NotificationPreference received, NotificationPreference awarded, NotificationPreference chat, NotificationPreference picking)
    {
        await using var db = fixture.CreateContext();
        await new LobbyNotificationPreferencesHandler(db).Handle(new UpdateLobbyNotificationPreferencesCommand
        {
            Code = code,
            UserId = userId,
            DrinkReceivedNotificationPreference = received,
            DrinkAwardedNotificationPreference = awarded,
            ChatNotificationPreference = chat,
            PickingStartedNotificationPreference = picking
        }, default);
    }

    private async Task SetDefaultsAsync(Guid userId, NotificationPreference all, bool applyToActiveLobbies)
    {
        await using var db = fixture.CreateContext();
        await new UpdateNotificationPreferencesCommandHandler(db, new UserRepository(db), TestServices.Mapper()).Handle(new UpdateNotificationPreferencesCommand
        {
            UserId = userId,
            DrinkReceivedNotificationPreference = all,
            DrinkAwardedNotificationPreference = all,
            ChatNotificationPreference = all,
            PickingStartedNotificationPreference = all,
            AchievementAwardedNotificationPreference = all,
            ApplyToActiveLobbies = applyToActiveLobbies
        }, default);
    }

    private async Task AwardDrinkAsync(string code, Guid scorerUserId)
    {
        await using var db = fixture.CreateContext();
        var lobby = await db.Lobbies.Include(l => l.LobbyMembers).SingleAsync(l => l.JoinCode == code);
        var member = lobby.LobbyMembers.Single(m => m.UserId == scorerUserId);
        await new DrinkAwardedPushNotificationHandler(_push, db).Handle(new DrinkAwardedNotification(new(lobby, member, new DrinkEntity())), default);
    }

    // --- Stamping defaults ---

    [Fact]
    public async Task CreatingALobbyGivesTheHostTheirDefaults()
    {
        var host = await UserWithDefaultsAsync(All, Relevant, None, All);

        var lobby = await CreateLobbyAsync(host.Id);

        Assert.Equal((All, Relevant, None, All), Settings(await MemberAsync(lobby.JoinCode, host.Id)));
    }

    [Fact]
    public async Task JoiningALobbyGivesTheMemberTheirDefaults()
    {
        var host = await UserWithDefaultsAsync(None, None, None, None);
        var joiner = await UserWithDefaultsAsync(Relevant, All, Relevant, None);
        var lobby = await CreateLobbyAsync(host.Id);

        await JoinAsync(lobby.JoinCode, joiner.Id);

        Assert.Equal((Relevant, All, Relevant, None), Settings(await MemberAsync(lobby.JoinCode, joiner.Id)));
    }

    [Fact]
    public async Task LoggingInGivesTheGuestsLobbiesTheAccountsDefaults()
    {
        var account = await UserWithDefaultsAsync(All, All, Relevant, All);
        Guid guestId, memberId;
        await using (var db = fixture.CreateContext())
        {
            var (_, guestHost) = new Seed(db).Lobby([Seed.NewGameId()]);
            (guestId, memberId) = (guestHost.UserId, guestHost.Id);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            await new LoginCommandHandler(db, TestServices.TokenService(), new UserRepository(db), TestServices.Mapper(), _mediator).Handle(
                new LoginCommand { Email = account.Email!, Password = SeedAccounts.Password, IpAddress = "203.0.113.7", GuestUserId = guestId }, default);
        }

        await using var check = fixture.CreateContext();
        Assert.Equal((All, All, Relevant, All), Settings(await check.LobbyMembers.SingleAsync(m => m.Id == memberId)));
    }

    // --- Changing settings ---

    [Fact]
    public async Task LobbySettingsChangeOnlyThatLobby()
    {
        var user = await UserWithDefaultsAsync(All, All, All, All);
        var first = await CreateLobbyAsync(user.Id);
        var second = await CreateLobbyAsync(user.Id);

        await SetLobbySettingsAsync(first.JoinCode, user.Id, None, Relevant, None, None);

        Assert.Equal((None, Relevant, None, None), Settings(await MemberAsync(first.JoinCode, user.Id)));
        Assert.Equal((All, All, All, All), Settings(await MemberAsync(second.JoinCode, user.Id)));
        await using var db = fixture.CreateContext();
        Assert.Equal(All, (await db.Users.SingleAsync(u => u.Id == user.Id)).ChatNotificationPreference); // defaults untouched
    }

    [Fact]
    public async Task LobbySettingsRejectUnknownValuesAndNonMembers()
    {
        var host = await UserWithDefaultsAsync(All, All, All, All);
        var outsider = await UserWithDefaultsAsync(All, All, All, All);
        var lobby = await CreateLobbyAsync(host.Id);

        await Assert.ThrowsAsync<ValidationException>(() => SetLobbySettingsAsync(lobby.JoinCode, host.Id, (NotificationPreference)7, All, All, All));
        await Assert.ThrowsAsync<NotFoundException>(() => SetLobbySettingsAsync(lobby.JoinCode, outsider.Id, All, All, All, All));
    }

    [Fact]
    public async Task ChangingDefaultsLeavesExistingLobbiesAlone()
    {
        var user = await UserWithDefaultsAsync(All, All, All, All);
        var lobby = await CreateLobbyAsync(user.Id);

        await SetDefaultsAsync(user.Id, None, applyToActiveLobbies: false);

        Assert.Equal((All, All, All, All), Settings(await MemberAsync(lobby.JoinCode, user.Id)));
        var next = await CreateLobbyAsync(user.Id);
        Assert.Equal((None, None, None, None), Settings(await MemberAsync(next.JoinCode, user.Id)));
    }

    [Fact]
    public async Task ChangingDefaultsCanApplyToActiveLobbiesOnly()
    {
        var user = await UserWithDefaultsAsync(All, All, All, All);
        var active = await CreateLobbyAsync(user.Id);
        var ended = await CreateLobbyAsync(user.Id);
        await using (var db = fixture.CreateContext())
            await db.Lobbies.Where(l => l.JoinCode == ended.JoinCode).ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false));

        await SetDefaultsAsync(user.Id, Relevant, applyToActiveLobbies: true);

        Assert.Equal((Relevant, Relevant, Relevant, Relevant), Settings(await MemberAsync(active.JoinCode, user.Id)));
        Assert.Equal((All, All, All, All), Settings(await MemberAsync(ended.JoinCode, user.Id)));
    }

    [Fact]
    public async Task DefaultsRejectUnknownValues()
    {
        var user = await UserWithDefaultsAsync(All, All, All, All);

        await Assert.ThrowsAsync<ValidationException>(() => SetDefaultsAsync(user.Id, (NotificationPreference)9, applyToActiveLobbies: false));
    }

    // --- Push handlers read the member's settings ---

    [Fact]
    public async Task YourPlayerScoringNotifiesYouWithNotifyMeIfItsMine()
    {
        // The drink-awarded setting decides, whatever the drink-received setting says (it used to be the other way round).
        var scorer = await UserWithDefaultsAsync(received: None, awarded: Relevant, chat: None, picking: None);
        var lobby = await CreateLobbyAsync(scorer.Id);

        await AwardDrinkAsync(lobby.JoinCode, scorer.Id);

        await _push.Received(1).SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", "Your player scored - give out a drink!",
            PushArgs.Devices(TokenOf(scorer)), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task YourPlayerScoringIsSilentWhenDrinkAwardedIsOff()
    {
        var scorer = await UserWithDefaultsAsync(received: All, awarded: None, chat: None, picking: None);
        var lobby = await CreateLobbyAsync(scorer.Id);

        await AwardDrinkAsync(lobby.JoinCode, scorer.Id);

        await _push.DidNotReceiveWithAnyArgs().SendLobbyEventNotification(default!, default!, default!, default!);
    }

    [Fact]
    public async Task TheSameUserCanHearDifferentThingsInDifferentLobbies()
    {
        var scorer = await UserWithDefaultsAsync(None, All, None, None);
        var watcher = await UserWithDefaultsAsync(None, All, None, None);
        var loud = await CreateLobbyAsync(scorer.Id);
        var quiet = await CreateLobbyAsync(scorer.Id);
        await JoinAsync(loud.JoinCode, watcher.Id);
        await JoinAsync(quiet.JoinCode, watcher.Id);
        await SetLobbySettingsAsync(quiet.JoinCode, watcher.Id, None, None, None, None);

        await AwardDrinkAsync(loud.JoinCode, scorer.Id);
        await AwardDrinkAsync(quiet.JoinCode, scorer.Id);

        await _push.Received(1).SendLobbyEventNotification(loud.JoinCode, "Drink Awarded", Arg.Any<string>(),
            PushArgs.Devices(TokenOf(watcher)), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
        await _push.DidNotReceive().SendLobbyEventNotification(quiet.JoinCode, Arg.Any<string>(), Arg.Any<string>(),
            PushArgs.Devices(TokenOf(watcher)), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task APushReachesEveryDeviceTheUserHas()
    {
        // The phone missed "time to pick" because the computer's token had replaced it.
        var scorer = await UserWithDefaultsAsync(received: None, awarded: Relevant, chat: None, picking: None);
        await using (var db = fixture.CreateContext())
            await SeedAccounts.AddDeviceAsync(db, scorer.Id, "phone-" + scorer.Id);
        var lobby = await CreateLobbyAsync(scorer.Id);

        await AwardDrinkAsync(lobby.JoinCode, scorer.Id);

        await _push.Received(1).SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", Arg.Any<string>(),
            PushArgs.Devices(TokenOf(scorer), "phone-" + scorer.Id), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TimeToPickUsesALobbyWherePickingNotificationsAreOn()
    {
        var gameId = Seed.NewGameId();
        var user = await UserWithDefaultsAsync(None, None, None, All);
        var muted = await CreateLobbyAsync(user.Id, gameId);
        var on = await CreateLobbyAsync(user.Id, gameId);
        await SetLobbySettingsAsync(muted.JoinCode, user.Id, None, None, None, None);
        _push.SendLobbyEventNotification(default!, default!, default!, default!).ReturnsForAnyArgs(1);

        await using (var db = fixture.CreateContext())
        {
            var handler = new NotifyUsersPicksReadyNotificationHandler(db, new InMemoryGameCache(), _push,
                Options.Create(new ApplicationOptions()), NullLogger<NotifyUsersPicksReadyNotificationHandler>.Instance);
            await handler.Handle(new PicksReadyNotification(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20), GameState.Upcoming)), default);
        }

        await _push.Received(1).SendLobbyEventNotification(on.JoinCode, "Time to pick players!", Arg.Any<string>(),
            PushArgs.Devices(TokenOf(user)), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
        await _push.DidNotReceive().SendLobbyEventNotification(muted.JoinCode, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<Dictionary<string, string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoTimeToPickWhenEveryLobbyHasItOff()
    {
        var gameId = Seed.NewGameId();
        var user = await UserWithDefaultsAsync(All, All, All, None);
        await CreateLobbyAsync(user.Id, gameId);

        await using (var db = fixture.CreateContext())
        {
            var handler = new NotifyUsersPicksReadyNotificationHandler(db, new InMemoryGameCache(), _push,
                Options.Create(new ApplicationOptions()), NullLogger<NotifyUsersPicksReadyNotificationHandler>.Instance);
            await handler.Handle(new PicksReadyNotification(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20), GameState.Upcoming)), default);
        }

        await _push.DidNotReceiveWithAnyArgs().SendLobbyEventNotification(default!, default!, default!, default!);
    }
}
