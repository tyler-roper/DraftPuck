namespace DraftPuck.Application.Features.Discord;

public interface IOAuthCache
{
    Task AddStateAsync(Guid state, Guid draftPuckUserId);
    public Task<Guid?> GetUserIdAndDeleteByState(string state);
    public Task RemoveStateAsync(string state);
}
