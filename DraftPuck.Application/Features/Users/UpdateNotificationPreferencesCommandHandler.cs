using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Lobbies.Members;

namespace DraftPuck.Application.Features.Users;

public class UpdateNotificationPreferencesCommandHandler(IDbContext dbContext, IUserRepository userRepository, IMapper mapper)
    : IRequestHandler<UpdateNotificationPreferencesCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        MemberNotificationPreferences.Validate(
            request.DrinkReceivedNotificationPreference,
            request.DrinkAwardedNotificationPreference,
            request.ChatNotificationPreference,
            request.PickingStartedNotificationPreference,
            request.AchievementAwardedNotificationPreference,
            request.RecapNotificationPreference,
            request.FriendNotificationPreference);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsBot, ct)
            ?? throw new NotFoundException("User not found.");

        user.DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference;
        user.DrinkAwardedNotificationPreference = request.DrinkAwardedNotificationPreference;
        user.ChatNotificationPreference = request.ChatNotificationPreference;
        user.PickingStartedNotificationPreference = request.PickingStartedNotificationPreference;
        user.AchievementAwardedNotificationPreference = request.AchievementAwardedNotificationPreference;
        user.RecapNotificationPreference = request.RecapNotificationPreference;
        user.FriendNotificationPreference = request.FriendNotificationPreference;

        if (request.ApplyToActiveLobbies)
        {
            var members = await dbContext.LobbyMembers
                .Where(lm => lm.UserId == user.Id && !lm.IsRemoved && lm.Lobby.IsActive)
                .ToListAsync(ct);
            foreach (var member in members)
                MemberNotificationPreferences.StampDefaults(member, user);
        }

        await dbContext.SaveChangesAsync(ct);

        var updated = await userRepository.GetById(user.Id, ct);
        return mapper.Map<UserDto>(updated);
    }
}
