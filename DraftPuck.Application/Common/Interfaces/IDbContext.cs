using DraftPuck.Shared.Auth;
using DraftPuck.Shared.Titles;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DraftPuck.Application.Common.Interfaces
{
    public interface IDbContext
    {
        DbSet<AchievementEntity> Achievements { get; }
        DbSet<BannerEntity> Banners { get; }
        DbSet<DrinkEntity> Drinks { get; }
        DbSet<ErrorLogEntity> ErrorLogs { get; }
        DbSet<LobbyEntity> Lobbies { get; }
        DbSet<LobbyEventEntity> LobbyEvents { get; }
        DbSet<LobbyMemberEntity> LobbyMembers { get; }
        DbSet<LobbyMemberPickEntity> LobbyMemberPicks { get; }
        DbSet<MessageEntity> Messages { get; }
        DbSet<UserAchievementEntity> UserAchievements { get; }
        DbSet<UserBannerEntity> UserBanners { get; }
        DbSet<UserTitleEntity> UserTitles { get; }
        DbSet<UserEntity> Users { get; }
        DbSet<UserRefreshTokenEntity> UserRefreshTokens { get; }
        DbSet<TitleEntity> Titles { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    }
}
