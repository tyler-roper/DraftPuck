using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Users;

public class UpdateNotificationPreferencesCommandHandler(IDbContext dbContext, IUserRepository userRepository, IMapper mapper) : IRequestHandler<UpdateNotificationPreferencesCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(request.UserId, cancellationToken) ?? throw new NotFoundException($"User not found with ID {request.UserId}");

        user.DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference;
        user.DrinkAwardedNotificationPreference = request.DrinkAwardedNotificationPreference;
        user.ChatNotificationPreference = request.ChatNotificationPreference;
        user.PickingStartedNotificationPreference = request.PickingStartedNotificationPreference;
        user.AchievementAwardedNotificationPreference = request.AchievementAwardedNotificationPreference;

        await dbContext.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserDto>(user);
    }
}