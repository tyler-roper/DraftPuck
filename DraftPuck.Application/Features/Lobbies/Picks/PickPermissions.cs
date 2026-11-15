namespace DraftPuck.Application.Features.Lobbies.Picks;

public static class PickPermissions
{
    /// <summary>
    /// The lobby's host and site admins can remove any pick, at any time. Everyone else can only remove their own,
    /// and only while <see cref="GameDto.CanMembersRemovePicks"/>.
    /// </summary>
    public static async Task<bool> CanManageAllPicksAsync(IDbContext dbContext, LobbyEntity lobby, Guid userId, CancellationToken ct)
        => lobby.CreatedBy == userId
            || await dbContext.Users.AnyAsync(u => u.Id == userId && u.IsAdmin, ct);
}
