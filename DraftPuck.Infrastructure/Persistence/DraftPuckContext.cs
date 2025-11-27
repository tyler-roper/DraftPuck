using DraftPuck.Shared.Auth;
using DraftPuck.Shared.Titles;

namespace DraftPuck.Infrastructure.Persistence;

public partial class DraftPuckContext : DbContext, IDbContext
{
    public DraftPuckContext()
    {
    }

    public DraftPuckContext(DbContextOptions<DraftPuckContext> options)
        : base(options)
    {
    }

    public DbSet<AchievementEntity> Achievements { get; set; }
    public DbSet<BannerEntity> Banners { get; set; }
    public DbSet<DrinkEntity> Drinks { get; set; }
    public DbSet<ErrorLogEntity> ErrorLogs { get; set; }
    public DbSet<LobbyEntity> Lobbies { get; set; }
    public DbSet<LobbyEventEntity> LobbyEvents { get; set; }
    public DbSet<LobbyMemberEntity> LobbyMembers { get; set; }
    public DbSet<LobbyMemberPickEntity> LobbyMemberPicks { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<UserAchievementEntity> UserAchievements { get; set; }
    public DbSet<UserBannerEntity> UserBanners { get; set; }
    public DbSet<UserTitleEntity> UserTitles { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<UserRefreshTokenEntity> UserRefreshTokens { get; set; }
    public DbSet<TitleEntity> Titles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchievementEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UniqueIdentifier).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.FriendlyName).HasMaxLength(255);
            entity.Property(e => e.UniqueIdentifier).HasMaxLength(50);
        });

        modelBuilder.Entity<BannerEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImagePath).HasMaxLength(1000);
            entity.Property(e => e.UniqueIdentifier).HasMaxLength(50);
            entity.HasOne(b => b.Achievement)
                .WithMany(a => a.Banners)
                .HasForeignKey(b => b.AchievementId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DrinkEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.LobbyMemberPick).WithMany(p => p.Drinks)
                .HasForeignKey(d => d.LobbyMemberPickId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Drinks_LobbyMemberPicks");

            entity.HasOne(d => d.RecipientLobbyMember).WithMany(p => p.Drinks)
                .HasForeignKey(d => d.RecipientLobbyMemberId)
                .HasConstraintName("FK_Drinks_LobbyMembers");
        });


        modelBuilder.Entity<ErrorLogEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Error).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Info).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<LobbyEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.JoinCode).HasMaxLength(4);
            entity.Property(e => e.GameIds).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");
            entity.Property(e => e.PicksPerTeam).HasDefaultValueSql("((1))");
            entity.Property(e => e.IsBotAutoPickingEnabled).HasDefaultValueSql("((0))");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.CreatedLobbies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lobbies_People");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Lobbies_IsActive");
        });

        modelBuilder.Entity<LobbyEventEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsSent).HasDefaultValueSql("((0))");
            entity.Property(e => e.SendAttempts).HasDefaultValueSql("((0))");
            entity.Property(e => e.Subtext).HasMaxLength(50);
            entity.Property(e => e.Text).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<LobbyMemberEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Joined).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsBot).HasDefaultValueSql("((0))");
            entity.Property(e => e.IsRemoved).HasDefaultValueSql("((0))");

            entity.HasOne(d => d.Lobby).WithMany(p => p.LobbyMembers)
                .HasForeignKey(d => d.LobbyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMembers_Lobbies");

            entity.HasOne(d => d.User).WithMany(p => p.LobbyMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMembers_People");
        });

        modelBuilder.Entity<LobbyMemberPickEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");

            entity.HasOne(d => d.LobbyMember).WithMany(p => p.LobbyMemberPicks)
                .HasForeignKey(d => d.LobbyMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMemberPicks_LobbyMembers");

            entity.HasIndex(e => new { e.LobbyMemberId, e.IsActive })
                .HasDatabaseName("IX_LobbyMemberPicks_LobbyMemberId_IsActive");
        });

        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");
            entity.Property(e => e.Sent).HasDefaultValueSql("(getutcdate())");

            entity
                .HasOne(d => d.LobbyMember)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.LobbyMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_LobbyMembers");

            entity.HasIndex(m => new { m.LobbyMemberId, m.IsDeleted })
                .HasDatabaseName("IX_Messages_LobbyMemberId_IsDeleted");
        });

        modelBuilder.Entity<TitleEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Text).HasMaxLength(255);
            entity.Property(e => e.UniqueIdentifier).HasMaxLength(50);
            entity.HasOne(t => t.Achievement)
                .WithMany(a => a.Titles)
                .HasForeignKey(t => t.AchievementId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAchievementEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateEarned).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(ua => ua.User)
                .WithMany(u => u.UserAchievements)
                .HasForeignKey(ua => ua.UserId);

            entity.HasOne(ua => ua.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(ua => ua.AchievementId);
        });

        modelBuilder.Entity<UserBannerEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IsEquipped).HasDefaultValueSql("((0))");
            entity.HasOne(ub => ub.User).WithMany(u => u.UserBanners).HasForeignKey(ub => ub.UserId);
            entity.HasOne(ub => ub.Banner).WithMany(b => b.UserBanners).HasForeignKey(ub => ub.BannerId);
        });

        modelBuilder.Entity<UserRefreshTokenEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<UserTitleEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IsEquipped).HasDefaultValueSql("((0))");

            entity.HasOne(ut => ut.User).WithMany(u => u.UserTitles).HasForeignKey(ut => ut.UserId);
            entity.HasOne(ut => ut.Title).WithMany(b => b.UserTitles).HasForeignKey(ut => ut.TitleId);
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsBot).HasDefaultValueSql("((0))");
            entity.Property(e => e.FcmRegistrationToken).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DrinkReceivedNotificationPreference).HasDefaultValueSql("((0))");
            entity.Property(e => e.DrinkAwardedNotificationPreference).HasDefaultValueSql("((0))");
            entity.Property(e => e.ChatNotificationPreference).HasDefaultValueSql("((0))");
            entity.Property(e => e.PickingStartedNotificationPreference).HasDefaultValueSql("((0))");
            entity.Property(e => e.AchievementAwardedNotificationPreference).HasDefaultValueSql("((0))");
            entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");
            entity.Property(e => e.IsAdmin).HasDefaultValueSql("((0))");
            entity.Property(e => e.IsGuest).HasDefaultValueSql("((1))");
            entity.Property(e => e.AvatarPath).HasMaxLength(1000);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.DiscordUserId).HasMaxLength(25);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
