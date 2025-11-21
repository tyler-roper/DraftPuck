using DraftPuck.Application.Common.Exceptions;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;

namespace DraftPuck.Application.Features.Users;

public class UpdateUserCommandHandler(IDbContext dbContext, IMapper mapper, IUserRepository userRepository, IMediator mediator, IAvatarStorageService avatarService) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var requester = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.RequesterUserId, cancellationToken) ?? throw new UnauthorizedException("Requester not found.");
        var userIdToUpdate = request.TargetUserId;

        var isUpdatingSelf = request.RequesterUserId == request.TargetUserId;
        var isRequesterSpoofed = !request.RequesterIsAuthenticated && !requester.IsGuest;

        if (isRequesterSpoofed || (!isUpdatingSelf && !requester.IsAdmin))
            throw new ForbiddenException("Invalid action.");

        await UserValidationHelpers.ValidateUpdateUserRequest(dbContext, request, cancellationToken);

        var userToUpdate = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdToUpdate, cancellationToken) ?? throw new NotFoundException("Target user not found.");
        var oldName = userToUpdate.Nickname;

        if (request.Email != null)
            userToUpdate.Email = request.Email;

        if (request.Nickname != null)
            userToUpdate.Nickname = request.Nickname;

        if (request.FcmRegistrationToken != null)
            userToUpdate.FcmRegistrationToken = request.FcmRegistrationToken;

        if (request.DrinkReceivedNotificationPreference.HasValue)
            userToUpdate.DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference.Value;

        if (request.ChatNotificationPreference.HasValue)
            userToUpdate.ChatNotificationPreference = request.ChatNotificationPreference.Value;

        if (request.PickingStartedNotificationPreference.HasValue)
            userToUpdate.PickingStartedNotificationPreference = request.PickingStartedNotificationPreference.Value;

        if (request.BannerId.HasValue || request.TitleId.HasValue)
            await UpdateEquippedItems(userToUpdate, request.BannerId, request.TitleId, cancellationToken);

        if (request.Password != null)
            userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.AvatarData != null)
        {
            var newAvatarPath = await TryAvatarUpload(userIdToUpdate, request.AvatarData, userToUpdate.AvatarPath);
            userToUpdate.AvatarPath = newAvatarPath ?? userToUpdate.AvatarPath;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var fullUpdatedUserEntity = await userRepository.GetById(userToUpdate.Id, cancellationToken);
        var didProfileChange = request.BannerId.HasValue || request.TitleId.HasValue || request.Nickname != null;
        var didNicknameChange = request.Nickname != null;

        if (didProfileChange)
        {
            var payload = new UserProfileUpdatedPayload(userIdToUpdate, request.Nickname != null ? oldName : null);
            await mediator.Publish(new UserProfileUpdatedNotification(payload), cancellationToken);
        }

        return mapper.Map<UserDto>(fullUpdatedUserEntity);
    }

    private async Task UpdateEquippedItems(UserEntity user, Guid? newBannerId, Guid? newTitleId, CancellationToken ct)
    {
        if (newBannerId.HasValue)
        {
            await dbContext.UserBanners
                .Where(ub => ub.UserId == user.Id && ub.IsEquipped)
                .ExecuteUpdateAsync(s => s.SetProperty(ub => ub.IsEquipped, false), ct);

            await dbContext.Entry(user).Collection(u => u.UserBanners).LoadAsync(ct);

            var targetBannerLink = user.UserBanners.FirstOrDefault(ub => ub.BannerId == newBannerId.Value);
            if (targetBannerLink != null)
            {
                targetBannerLink.IsEquipped = true;
            }
            else
            {
                dbContext.UserBanners.Add(new UserBannerEntity
                {
                    UserId = user.Id,
                    BannerId = newBannerId.Value,
                    IsEquipped = true
                });
            }
        }

        if (newTitleId.HasValue)
        {
            dbContext.UserTitles
                .Where(ut => ut.UserId == user.Id && ut.IsEquipped)
                .ExecuteUpdate(s => s.SetProperty(ut => ut.IsEquipped, false));

            await dbContext.Entry(user).Collection(u => u.UserTitles).LoadAsync(ct);

            var targetTitleLink = user.UserTitles.FirstOrDefault(ut => ut.TitleId == newTitleId.Value);

            if (targetTitleLink != null)
            {
                targetTitleLink.IsEquipped = true;
            }
            else
            {
                dbContext.UserTitles.Add(new UserTitleEntity
                {
                    UserId = user.Id,
                    TitleId = newTitleId.Value,
                    IsEquipped = true
                });
            }
        }

        //SaveChanges handled by calling method
    }

    private async Task<string?> TryAvatarUpload(Guid userId, string newAvatarData, string? previousAvatarPath)
    {
        try
        {
            var base64 = newAvatarData.Split("base64,")[1];
            var bytes = Convert.FromBase64String(base64);
            using MemoryStream ms = new(bytes);

            var extension = ms.Is<PortableNetworkGraphic>() ? "png" :
                            ms.Is<JointPhotographicExpertsGroup>() ? "jpg" :
                            throw new Exception("Invalid filetype.");


            var newAvatarPath = await avatarService.UploadBlobAsync($"{userId}/{Guid.NewGuid()}.{extension}", ms, $"image/{extension}");

            if (!string.IsNullOrEmpty(previousAvatarPath)) 
                await avatarService.DeleteBlobByUriAsync(previousAvatarPath);

            return newAvatarPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to upload avatar to blob storage: {ex.Message}");
            return null;
        }
    }
}
