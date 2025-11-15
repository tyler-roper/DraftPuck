namespace DraftPuck.Application.Features.Users;

public class CreateUserCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        request.Nickname = request.Nickname.Trim();

        await UserValidationHelpers.ValidateCreateUserRequest(dbContext, request, cancellationToken);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.GuestUserId, cancellationToken) ?? new UserEntity() { Id = Guid.NewGuid() };
        user.Email = request.Email;
        user.Nickname = request.Nickname;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.IsActive = true;
        user.IsAdmin = false;
        user.IsBot = false;
        user.IsGuest = false;

        if (user.Id != request.GuestUserId)
            dbContext.Users.Add(user);

        await ApplyDefaultTitleAndBanner(user, cancellationToken);

        var associatedLobbyMembers = await dbContext.LobbyMembers
            .Include(lm => lm.Lobby)
            .Where(lm => lm.UserId == user.Id && lm.Lobby.IsActive)
            .ToListAsync(cancellationToken);

        var lobbyNotifications = new List<INotification>();
        foreach (var member in associatedLobbyMembers)
        {
            if (member.Name == user.Nickname) continue;

            var oldName = member.Name;
            member.Name = user.Nickname;

            if (!member.IsRemoved)
                lobbyNotifications.Add(new UserNameChangedNotification(new LobbyNameChangeEventPayload(member.Lobby, member, oldName)));

            lobbyNotifications.Add(new LobbyStateChangedNotification(new LobbyStateChangedPayload(member.Lobby.JoinCode)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var notification in lobbyNotifications)
            await mediator.Publish(notification, cancellationToken);

        return mapper.Map<UserDto>(user);
    }

    private async Task ApplyDefaultTitleAndBanner(UserEntity user, CancellationToken ct)
    {
        var defaultBanner = await dbContext.Banners.SingleAsync(b => b.UniqueIdentifier == "default", ct);
        var defaultTitle = await dbContext.Titles.SingleAsync(t => t.UniqueIdentifier == "default", ct);

        await dbContext.UserBanners
            .Where(ub => ub.UserId == user.Id && ub.IsEquipped)
            .ExecuteUpdateAsync(s => s.SetProperty(ub => ub.IsEquipped, false), ct);

        await dbContext.UserTitles
            .Where(ut => ut.UserId == user.Id && ut.IsEquipped)
            .ExecuteUpdateAsync(s => s.SetProperty(ut => ut.IsEquipped, false), ct);

        dbContext.UserBanners.Add(new UserBannerEntity()
        {
            UserId = user.Id,
            Banner = defaultBanner,
            IsEquipped = true
        });

        dbContext.UserTitles.Add(new UserTitleEntity()
        {
            UserId = user.Id,
            Title = defaultTitle,
            IsEquipped = true
        });
    }
}


