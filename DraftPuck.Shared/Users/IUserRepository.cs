namespace DraftPuck.Shared.Users;
public interface IUserRepository
{
    Task<UserEntity?> GetById(Guid userId, CancellationToken cancellationToken);
    Task<UserEntity?> GetByName(string name, CancellationToken cancellationToken);
    Task<UserEntity?> GetByRefreshToken(string token, CancellationToken cancellationToken);
}
