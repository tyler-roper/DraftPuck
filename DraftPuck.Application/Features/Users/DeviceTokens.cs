using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Users;

public static class DeviceTokens
{
    /// <summary>Trims the token and checks it could be a Firebase registration token.</summary>
    public static string Validate(string? token)
    {
        var trimmed = token?.Trim() ?? "";
        if (trimmed.Length == 0 || trimmed.Length > UserDeviceEntity.MaxTokenLength || trimmed.Any(char.IsWhiteSpace))
            throw new ValidationException("Invalid notification token.");
        return trimmed;
    }
}
