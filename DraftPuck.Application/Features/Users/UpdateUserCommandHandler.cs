using DraftPuck.Application.Common.Exceptions;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;

namespace DraftPuck.Application.Features.Users;

public class UpdateUserCommandHandler(IDbContext dbContext, IMapper mapper, IUserRepository userRepository, IMediator mediator, IAvatarStorageService avatarService, ILogger<UpdateUserCommandHandler> logger) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var requester = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.RequesterUserId, cancellationToken) ?? throw new UnauthorizedException("Requester not found.");
        var userIdToUpdate = request.TargetUserId;

        var isUpdatingSelf = request.RequesterUserId == request.TargetUserId;
        var isRequesterSpoofed = !request.RequesterIsAuthenticated && !requester.IsGuest;

        if (isRequesterSpoofed || (!isUpdatingSelf && !requester.IsAdmin))
            throw new ForbiddenException("Invalid action.");

        request.Nickname = request.Nickname?.Trim();
        await UserValidationHelpers.ValidateUpdateUserRequest(dbContext, request, cancellationToken);

        var userToUpdate = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdToUpdate, cancellationToken) ?? throw new NotFoundException("Target user not found.");
        var oldName = userToUpdate.Nickname;

        // Changing your own password requires the current one (admins resetting someone else's don't need it).
        if (request.Password != null && isUpdatingSelf && userToUpdate.PasswordHash != null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, userToUpdate.PasswordHash))
                throw new BadRequestException("Current password is incorrect.");
        }

        if (request.Email != null)
            userToUpdate.Email = request.Email;

        if (request.Nickname != null)
            userToUpdate.Nickname = request.Nickname;

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
            var banner = await dbContext.Banners.FirstOrDefaultAsync(b => b.Id == newBannerId.Value, ct)
                ?? throw new NotFoundException("Banner not found.");

            if (!await IsUnlockedAsync(user.Id, banner.AchievementId, dbContext.UserBanners.Where(ub => ub.UserId == user.Id && ub.BannerId == banner.Id), ct))
                throw new ForbiddenException("This banner hasn't been unlocked.");

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
            var title = await dbContext.Titles.FirstOrDefaultAsync(t => t.Id == newTitleId.Value, ct)
                ?? throw new NotFoundException("Title not found.");

            if (!await IsUnlockedAsync(user.Id, title.AchievementId, dbContext.UserTitles.Where(ut => ut.UserId == user.Id && ut.TitleId == title.Id), ct))
                throw new ForbiddenException("This title hasn't been unlocked.");

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

    /// <summary>
    /// Generic items (no achievement) are available to everyone. Achievement rewards require the user to already
    /// have the item, or to have earned the achievement it belongs to.
    /// </summary>
    private async Task<bool> IsUnlockedAsync<TLink>(Guid userId, Guid? achievementId, IQueryable<TLink> existingLinks, CancellationToken ct)
    {
        if (achievementId == null)
            return true;

        if (await existingLinks.AnyAsync(ct))
            return true;

        return await dbContext.UserAchievements.AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId, ct);
    }

    /// <summary>
    /// Uploads the new avatar and returns its URL, or null (keeping the current avatar) if the upload fails. Once the
    /// new one is stored, failing to delete the old blob only leaves it orphaned, so it doesn't undo the change.
    /// </summary>
    private async Task<string?> TryAvatarUpload(Guid userId, string newAvatarData, string? previousAvatarPath)
    {
        string newAvatarPath;
        try
        {
            var base64 = newAvatarData.Split("base64,")[1];
            var bytes = Convert.FromBase64String(base64);
            using MemoryStream ms = new(bytes);

            var extension = ms.Is<PortableNetworkGraphic>() ? "png" :
                            ms.Is<JointPhotographicExpertsGroup>() ? "jpg" :
                            throw new Exception("Invalid filetype.");

            newAvatarPath = await avatarService.UploadBlobAsync($"{userId}/{Guid.NewGuid()}.{extension}", ms, $"image/{extension}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload a new avatar for user {UserId}", userId);
            return null;
        }

        if (!string.IsNullOrEmpty(previousAvatarPath))
        {
            try
            {
                await avatarService.DeleteBlobByUriAsync(previousAvatarPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete the previous avatar {AvatarPath} for user {UserId}", previousAvatarPath, userId);
            }
        }

        return newAvatarPath;
    }
}
