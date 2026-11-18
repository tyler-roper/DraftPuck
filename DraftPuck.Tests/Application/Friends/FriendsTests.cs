using System.ComponentModel.DataAnnotations;
using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Friends;
using DraftPuck.Domain.Friends;

namespace DraftPuck.Tests.Application.Friends;

/// <summary>Friend requests, friends lists, and inviting friends to lobbies.</summary>
[Collection(DatabaseCollection.Name)]
public class FriendsTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();

    private async Task<UserEntity> AccountAsync(string? nickname = null, bool withDevice = true)
    {
        await using var db = fixture.CreateContext();
        var user = await SeedAccounts.AccountAsync(db, nickname);
        if (withDevice) await SeedAccounts.AddDeviceAsync(db, user.Id, TokenOf(user));
        return user;
    }

    private static string TokenOf(UserEntity user) => "token-" + user.Id;

    private async Task<FriendStatus> AddAsync(UserEntity user, Guid friendId)
    {
        await using var db = fixture.CreateContext();
        return await new AddFriendCommandHandler(db, _mediator).Handle(new AddFriendCommand { UserId = user.Id, FriendId = friendId }, default);
    }

    private async Task RemoveAsync(UserEntity user, Guid friendId)
    {
        await using var db = fixture.CreateContext();
        await new RemoveFriendCommandHandler(db).Handle(new RemoveFriendCommand { UserId = user.Id, FriendId = friendId }, default);
    }

    private async Task<FriendsDto> FriendsOfAsync(UserEntity user)
    {
        await using var db = fixture.CreateContext();
        return await new GetFriendsQueryHandler(db).Handle(new GetFriendsQuery { UserId = user.Id }, default);
    }

    private async Task<(UserEntity A, UserEntity B)> FriendsAsync()
    {
        var a = await AccountAsync();
        var b = await AccountAsync();
        await AddAsync(a, b.Id);
        await AddAsync(b, a.Id);
        return (a, b);
    }

    private async Task<List<Guid>> InviteAsync(UserEntity sender, string joinCode, params Guid[] friendIds)
    {
        await using var db = fixture.CreateContext();
        return await new SendLobbyInvitesCommandHandler(db, _mediator)
            .Handle(new SendLobbyInvitesCommand { UserId = sender.Id, JoinCode = joinCode, FriendIds = [.. friendIds] }, default);
    }

    /// <summary>An active lobby hosted by <paramref name="host"/>.</summary>
    private async Task<LobbyEntity> LobbyAsync(UserEntity host, bool isActive = true)
    {
        await using var db = fixture.CreateContext();
        var lobby = new LobbyEntity { Id = Guid.NewGuid(), JoinCode = Seed.NewJoinCode(), CreatedBy = host.Id, PicksPerTeam = 1, GameIds = [Seed.NewGameId(), Seed.NewGameId()], IsActive = isActive };
        db.Lobbies.Add(lobby);
        db.LobbyMembers.Add(new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = host.Id, Name = host.Nickname! });
        await db.SaveChangesAsync();
        return lobby;
    }

    private async Task JoinAsync(LobbyEntity lobby, UserEntity user, bool isRemoved = false)
    {
        await using var db = fixture.CreateContext();
        db.LobbyMembers.Add(new LobbyMemberEntity { Id = Guid.NewGuid(), LobbyId = lobby.Id, UserId = user.Id, Name = user.Nickname!, IsRemoved = isRemoved });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ARequestWaitsUntilTheOtherPlayerAddsThemBack()
    {
        var sam = await AccountAsync("sam" + Guid.NewGuid().ToString("N")[..6]);
        var alex = await AccountAsync("alex" + Guid.NewGuid().ToString("N")[..6]);

        Assert.Equal(FriendStatus.RequestSent, await AddAsync(sam, alex.Id));
        await _mediator.Received(1).Publish(Arg.Is<FriendRequestedNotification>(n => n.Requester.Id == sam.Id && n.Addressee.Id == alex.Id), Arg.Any<CancellationToken>());

        var samsView = await FriendsOfAsync(sam);
        Assert.Empty(samsView.Friends);
        Assert.Equal([alex.Id], samsView.Outgoing.Select(f => f.Id));
        var alexsView = await FriendsOfAsync(alex);
        Assert.Equal([sam.Id], alexsView.Incoming.Select(f => f.Id));
        Assert.Equal(sam.Nickname, alexsView.Incoming[0].Nickname);

        // Asking again doesn't push Alex again.
        Assert.Equal(FriendStatus.RequestSent, await AddAsync(sam, alex.Id));
        await _mediator.Received(1).Publish(Arg.Any<FriendRequestedNotification>(), Arg.Any<CancellationToken>());

        // Alex adding Sam accepts the request: one friendship, and Sam hears about it.
        Assert.Equal(FriendStatus.Friends, await AddAsync(alex, sam.Id));
        await _mediator.Received(1).Publish(Arg.Is<FriendRequestAcceptedNotification>(n => n.Requester.Id == sam.Id && n.Addressee.Id == alex.Id), Arg.Any<CancellationToken>());
        Assert.Equal(FriendStatus.Friends, await AddAsync(sam, alex.Id));

        Assert.Equal([alex.Id], (await FriendsOfAsync(sam)).Friends.Select(f => f.Id));
        var alexsFriends = await FriendsOfAsync(alex);
        Assert.Equal([sam.Id], alexsFriends.Friends.Select(f => f.Id));
        Assert.Empty(alexsFriends.Incoming);

        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.Friendships.CountAsync(f => f.RequesterId == sam.Id || f.RequesterId == alex.Id));
    }

    [Fact]
    public async Task EitherPlayerCanUnfriendOrDeclineQuietly()
    {
        var (a, b) = await FriendsAsync();
        await RemoveAsync(b, a.Id);
        Assert.Empty((await FriendsOfAsync(a)).Friends);

        // Declining a request, and removing someone who isn't a friend, are the same: the row goes, if there is one.
        await AddAsync(a, b.Id);
        await RemoveAsync(b, a.Id);
        await RemoveAsync(b, a.Id);
        var view = await FriendsOfAsync(a);
        Assert.Empty(view.Outgoing);
        Assert.Empty(view.Friends);
    }

    [Fact]
    public async Task OnlyPlayersWithAnAccountCanBeFriends()
    {
        var account = await AccountAsync();
        UserEntity guest, bot;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            guest = seed.User();
            bot = seed.User(isBot: true);
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<ForbiddenException>(() => AddAsync(guest, account.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => AddAsync(account, guest.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => AddAsync(account, bot.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => AddAsync(account, Guid.NewGuid()));
        await Assert.ThrowsAsync<ValidationException>(() => AddAsync(account, account.Id));
    }

    [Fact]
    public async Task FriendPushesFollowTheFriendsSetting()
    {
        var sam = await AccountAsync();
        var alex = await AccountAsync();
        var quiet = await AccountAsync();
        await using (var db = fixture.CreateContext())
        {
            var user = await db.Users.SingleAsync(u => u.Id == quiet.Id);
            user.FriendNotificationPreference = NotificationPreference.None;
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var handlers = new FriendPushNotificationHandlers(db, _push, NullLogger<FriendPushNotificationHandlers>.Instance);
            await handlers.Handle(new FriendRequestedNotification(sam, alex), default);
            await handlers.Handle(new FriendRequestAcceptedNotification(sam, alex), default);
            await handlers.Handle(new FriendRequestedNotification(sam, quiet), default);
        }

        await _push.Received(1).SendFriendNotification("Friend request", $"{sam.Nickname} wants to be friends.", FriendPushNotificationHandlers.FriendsPath, PushArgs.Devices(TokenOf(alex)), Arg.Any<CancellationToken>());
        await _push.Received(1).SendFriendNotification("Friend request accepted", $"You and {alex.Nickname} are now friends.", FriendPushNotificationHandlers.FriendsPath, PushArgs.Devices(TokenOf(sam)), Arg.Any<CancellationToken>());
        await _push.Received(1).SendFriendNotification(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), PushArgs.Devices(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PeopleYouHavePlayedWithAreSuggested()
    {
        var me = await AccountAsync();
        var older = await AccountAsync();
        var recent = await AccountAsync();
        var friend = await AccountAsync();
        var asked = await AccountAsync();
        var stranger = await AccountAsync();
        UserEntity guest;
        await using (var db = fixture.CreateContext())
        {
            guest = new Seed(db).User();
            await db.SaveChangesAsync();
        }
        await AddAsync(me, friend.Id);
        await AddAsync(friend, me.Id);
        await AddAsync(asked, me.Id);

        var lastWeek = await LobbyAsync(me, isActive: false);
        var tonight = await LobbyAsync(recent);
        await using (var db = fixture.CreateContext())
        {
            (await db.Lobbies.SingleAsync(l => l.Id == lastWeek.Id)).Created = DateTime.UtcNow.AddDays(-7);
            (await db.Lobbies.SingleAsync(l => l.Id == tonight.Id)).Created = DateTime.UtcNow;
            new Seed(db).Bot(lastWeek);
            await db.SaveChangesAsync();
        }
        foreach (var player in new[] { older, friend, asked, guest })
            await JoinAsync(lastWeek, player);
        await JoinAsync(tonight, me);
        await LobbyAsync(stranger);

        // Most recently played with first; no friends, people with a request either way, guests, bots or strangers.
        Assert.Equal([recent.Id, older.Id], (await FriendsOfAsync(me)).Suggestions.Select(f => f.Id));

        // Guests get no suggestions: they can't add anyone.
        Assert.Empty((await FriendsOfAsync(guest)).Suggestions);
    }

    [Fact]
    public async Task TheFriendsSettingIsOnUntilTurnedOff()
    {
        var user = await AccountAsync();
        Assert.Equal(NotificationPreference.All, user.FriendNotificationPreference);

        await using var db = fixture.CreateContext();
        var saved = await new UpdateNotificationPreferencesCommandHandler(db, new UserRepository(db), TestServices.Mapper())
            .Handle(new UpdateNotificationPreferencesCommand { UserId = user.Id, RecapNotificationPreference = NotificationPreference.All, FriendNotificationPreference = NotificationPreference.None }, default);

        Assert.Equal(NotificationPreference.None, saved.FriendNotificationPreference);
        await Assert.ThrowsAsync<ValidationException>(() => new UpdateNotificationPreferencesCommandHandler(db, new UserRepository(db), TestServices.Mapper())
            .Handle(new UpdateNotificationPreferencesCommand { UserId = user.Id, FriendNotificationPreference = (NotificationPreference)7 }, default));
    }

    [Fact]
    public async Task MembersInviteTheirFriendsOnce()
    {
        var (host, friend) = await FriendsAsync();
        var member = await AccountAsync();
        var stranger = await AccountAsync();
        var lobby = await LobbyAsync(host);
        await JoinAsync(lobby, member);
        await AddAsync(host, member.Id);
        await AddAsync(member, host.Id);

        // Strangers and members already in the lobby are skipped; the friend is invited once, whoever asks again.
        Assert.Equal([friend.Id], await InviteAsync(host, lobby.JoinCode, friend.Id, stranger.Id, member.Id));
        Assert.Equal([friend.Id], await InviteAsync(host, lobby.JoinCode, friend.Id));
        await _mediator.Received(1).Publish(
            Arg.Is<LobbyInvitesSentNotification>(n => n.Lobby.Id == lobby.Id && n.Sender.UserId == host.Id && n.RecipientIds.SequenceEqual(new[] { friend.Id })),
            Arg.Any<CancellationToken>());

        var invite = Assert.Single((await FriendsOfAsync(friend)).Invites);
        Assert.Equal(lobby.JoinCode, invite.JoinCode);
        Assert.Equal(host.Id, invite.Sender.Id);
        Assert.Equal(2, invite.GameCount);
        Assert.Equal(2, invite.MemberCount);
        Assert.Empty((await FriendsOfAsync(stranger)).Invites);

        // Joining answers the invite.
        await JoinAsync(lobby, friend);
        Assert.Empty((await FriendsOfAsync(friend)).Invites);
        await using var db = fixture.CreateContext();
        Assert.Empty(await new GetLobbyInvitesQueryHandler(db).Handle(new GetLobbyInvitesQuery { UserId = host.Id, JoinCode = lobby.JoinCode }, default));
    }

    [Fact]
    public async Task InvitesGoAwayWhenDismissedOrWhenTheLobbyEnds()
    {
        var (host, friend) = await FriendsAsync();
        var tonight = await LobbyAsync(host);
        var other = await LobbyAsync(host);
        await InviteAsync(host, tonight.JoinCode, friend.Id);
        await InviteAsync(host, other.JoinCode, friend.Id);
        Assert.Equal(2, (await FriendsOfAsync(friend)).Invites.Count);

        await using (var db = fixture.CreateContext())
            await new DismissLobbyInviteCommandHandler(db).Handle(new DismissLobbyInviteCommand { UserId = friend.Id, JoinCode = other.JoinCode }, default);
        Assert.Equal([tonight.JoinCode], (await FriendsOfAsync(friend)).Invites.Select(i => i.JoinCode));

        await using (var db = fixture.CreateContext())
        {
            (await db.Lobbies.SingleAsync(l => l.Id == tonight.Id)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Empty((await FriendsOfAsync(friend)).Invites);
    }

    [Fact]
    public async Task RemovedMembersAreNotInvitedBack()
    {
        var (host, friend) = await FriendsAsync();
        var lobby = await LobbyAsync(host);
        await JoinAsync(lobby, friend, isRemoved: true);

        Assert.Empty(await InviteAsync(host, lobby.JoinCode, friend.Id));
    }

    [Fact]
    public async Task OnlySignedInMembersOfALiveLobbyCanInvite()
    {
        var (host, friend) = await FriendsAsync();
        var outsider = await AccountAsync();
        var lobby = await LobbyAsync(host);
        var ended = await LobbyAsync(host, isActive: false);
        UserEntity guest;
        await using (var db = fixture.CreateContext())
        {
            guest = new Seed(db).User();
            await db.SaveChangesAsync();
        }
        await JoinAsync(lobby, guest);

        await Assert.ThrowsAsync<NotFoundException>(() => InviteAsync(outsider, lobby.JoinCode, friend.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => InviteAsync(host, ended.JoinCode, friend.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => InviteAsync(guest, lobby.JoinCode, friend.Id));
        await Assert.ThrowsAsync<ValidationException>(() => InviteAsync(host, lobby.JoinCode));
    }

    [Fact]
    public async Task InvitedFriendsGetAPushThatOpensTheLobby()
    {
        var (host, friend) = await FriendsAsync();
        var lobby = await LobbyAsync(host);

        await using (var db = fixture.CreateContext())
        {
            var sender = await db.LobbyMembers.Include(m => m.User).SingleAsync(m => m.LobbyId == lobby.Id && m.UserId == host.Id);
            await new FriendPushNotificationHandlers(db, _push, NullLogger<FriendPushNotificationHandlers>.Instance)
                .Handle(new LobbyInvitesSentNotification(lobby, sender, [friend.Id]), default);
        }

        await _push.Received(1).SendFriendNotification("Lobby invite", $"{host.Nickname} invited you to their lobby. 2 games tonight. Tap to join!",
            $"lobby/{lobby.JoinCode}", PushArgs.Devices(TokenOf(friend)), Arg.Any<CancellationToken>());
    }
}
