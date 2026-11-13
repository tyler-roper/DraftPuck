using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Members;

public static class LobbyMemberNames
{
    public const int MaxLength = 50;

    /// <summary>
    /// Trims and validates a lobby member display name. Names are user-supplied and shown to every
    /// member of the lobby, so markup characters and control characters are rejected outright.
    /// </summary>
    public static string Normalize(string? name)
    {
        var trimmed = name?.Trim() ?? "";

        if (trimmed.Length == 0)
            throw new ValidationException("Name is required.");

        if (trimmed.Length > MaxLength)
            throw new ValidationException($"Name must be {MaxLength} characters or fewer.");

        if (trimmed.Any(c => char.IsControl(c) || c is '<' or '>'))
            throw new ValidationException("Name contains invalid characters.");

        return trimmed;
    }
}
