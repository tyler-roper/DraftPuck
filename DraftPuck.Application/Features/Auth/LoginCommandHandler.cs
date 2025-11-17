using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.QueryExtensions;
using DraftPuck.Application.Features.Users;
using DraftPuck.Shared.Auth;
namespace DraftPuck.Application.Features.Auth;

public class LoginCommandHandler(IDbContext dbContext, ITokenService tokenService, IUserRepository userRepository, IMapper mapper, IMediator mediator) : IRequestHandler<LoginCommand, AuthenticationResultDto>
{
    public async Task<AuthenticationResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userEntity = await dbContext
            .Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true, cancellationToken);

        if (userEntity == null || !BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var jwtToken = tokenService.GenerateJwtToken(userEntity.Id);
        var refreshToken = tokenService.GenerateRefreshToken(request.IpAddress!);

        userEntity.RefreshTokens.Add(refreshToken);
        dbContext.UserRefreshTokens.RemoveRange(userEntity.RefreshTokens.Where(t => !t.IsValid && t.Created.AddDays(2) <= DateTime.UtcNow));

        var mediatorNotifications = await HandleLobbiesAndGetNotifications(request.GuestUserId, userEntity.Id, userEntity.Nickname!, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var userWithDetails = await userRepository.GetById(userEntity.Id, cancellationToken);
        var userDto = mapper.Map<UserDto>(userWithDetails);

        foreach (var notification in mediatorNotifications)
            await mediator.Publish(notification, cancellationToken);

        return new AuthenticationResultDto
        {
            User = userDto,
            JwtToken = jwtToken,
            RefreshToken = refreshToken.Token,
            AntiCsrfToken = refreshToken.AntiCsrfToken
        };
    }

    private async Task<List<INotification>> HandleLobbiesAndGetNotifications(Guid oldUserId, Guid newUserId, string nickname, CancellationToken ct)
    {
        var result = new List<INotification>();

        //If the old and new ID are the same, it means we're converting the user from Guest -> User (a new sign-up)
        //In which case, Lobby updates are already handled by the CreateUserCommandHandler
        if (oldUserId == newUserId) return result;

        var activeLobbiesWithUser = await dbContext
            .Lobbies
            .Where(l => l.IsActive)
            .Where(l => l.LobbyMembers.Select(lm => lm.UserId).Contains(oldUserId) || l.CreatedBy == oldUserId)
            .IncludeLobbyDetails(true)
            .ToListAsync(ct);

        if (activeLobbiesWithUser.Count == 0) return result;

        foreach (var lobby in activeLobbiesWithUser)
        {
            if (lobby.CreatedBy == oldUserId) lobby.CreatedBy = newUserId;
            var guestMember = lobby.LobbyMembers.Single(lm => lm.UserId == oldUserId);

            // Logged-In User is already in the lobby
            // Remove guest
            var existingLoggedInUser = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == newUserId);
            if (existingLoggedInUser != null)
            {
                guestMember.IsRemoved = true;
                foreach (var pick in guestMember.LobbyMemberPicks) pick.IsActive = false;
                var payload = new LobbyMemberEventPayload(lobby, guestMember);
                result.Add(new UserRemovedNotification(payload));
            }
            else
            // Logged-In User NOT already in the lobby
            // Convert guest to Logged-In user
            {
                guestMember.UserId = newUserId;

                var lobbyMemberWithSameName = lobby.LobbyMembers.FirstOrDefault(lm => !lm.IsRemoved && lm.UserId != newUserId && lm.Name.Equals(nickname, StringComparison.CurrentCultureIgnoreCase));
                if (lobbyMemberWithSameName != null)
                {
                    var oldName = lobbyMemberWithSameName.Name;
                    lobbyMemberWithSameName.Name = lobbyMemberWithSameName.Name + "2";
                    var payload = new LobbyNameChangeEventPayload(lobby, lobbyMemberWithSameName, oldName);
                    result.Add(new UserNameChangedNotification(payload));
                }

                if (!nickname.Equals(guestMember.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    var oldName = guestMember.Name;
                    guestMember.Name = nickname;
                    var payload = new LobbyNameChangeEventPayload(lobby, guestMember, oldName);
                    result.Add(new UserNameChangedNotification(payload));
                }
            }

            var lobbyStateChangePayload = new LobbyStateChangedPayload(lobby.JoinCode);
            result.Add(new LobbyStateChangedNotification(lobbyStateChangePayload));
        }

        return result;
    }
}