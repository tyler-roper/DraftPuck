using MediatR;

namespace DraftPuck.Application.Features.Users;

//Event Payloads
public record UserProfileUpdatedPayload(Guid UserId, string? OldName);

//Notifications
public record UserProfileUpdatedNotification(UserProfileUpdatedPayload Data) : INotification;