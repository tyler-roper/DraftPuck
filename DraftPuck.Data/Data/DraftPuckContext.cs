using DraftPuck.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace DraftPuck.Data.Data;

public partial class DraftPuckContext : DbContext
{
    public DraftPuckContext()
    {
    }

    public DraftPuckContext(DbContextOptions<DraftPuckContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Drink> Drinks { get; set; }

    public virtual DbSet<Lobby> Lobbies { get; set; }

    public virtual DbSet<LobbyEvent> LobbyEvents { get; set; }

    public virtual DbSet<LobbyMember> LobbyMembers { get; set; }

    public virtual DbSet<LobbyMemberPick> LobbyMemberPicks { get; set; }

    public virtual DbSet<MessageEntity> Messages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<Drink>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");

            _ = entity.HasOne(d => d.LobbyMemberPick).WithMany(p => p.Drinks)
                .HasForeignKey(d => d.LobbyMemberPickId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Drinks_LobbyMemberPicks");

            _ = entity.HasOne(d => d.RecipientLobbyMember).WithMany(p => p.Drinks)
                .HasForeignKey(d => d.RecipientLobbyMemberId)
                .HasConstraintName("FK_Drinks_LobbyMembers");
        });

        _ = modelBuilder.Entity<Lobby>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            _ = entity.Property(e => e.CreatedBy).HasMaxLength(50);
            _ = entity.Property(e => e.JoinCode).HasMaxLength(4);
            _ = entity.Property(e => e.PicksPerTeam).HasDefaultValueSql("((1))");

            _ = entity.HasOne(d => d.CreatedByUser).WithMany(p => p.CreatedLobbies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lobbies_People");
        });

        _ = modelBuilder.Entity<LobbyEvent>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            _ = entity.Property(e => e.IsSent).HasDefaultValueSql("((0))");
            _ = entity.Property(e => e.SendAttempts).HasDefaultValueSql("((0))");
        });

        _ = modelBuilder.Entity<LobbyMember>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Name).HasMaxLength(50);
            _ = entity.Property(e => e.Joined).HasDefaultValueSql("(getutcdate())");
            _ = entity.Property(e => e.IsBot).HasDefaultValueSql("((0))");
            _ = entity.Property(e => e.IsRemoved).HasDefaultValueSql("((0))");

            _ = entity.HasOne(d => d.Lobby).WithMany(p => p.LobbyMembers)
                .HasForeignKey(d => d.LobbyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMembers_Lobbies");

            _ = entity.HasOne(d => d.User).WithMany(p => p.LobbyMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMembers_People");
        });

        _ = modelBuilder.Entity<LobbyMemberPick>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            _ = entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");

            _ = entity.HasOne(d => d.LobbyMember).WithMany(p => p.LobbyMemberPicks)
                .HasForeignKey(d => d.LobbyMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMemberPicks_LobbyMembers");
        });

        _ = modelBuilder.Entity<MessageEntity>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.IsDeleted).HasDefaultValueSql("((0))");
            _ = entity.Property(e => e.Sent).HasDefaultValueSql("(getutcdate())");

            _ = entity
                .HasOne(d => d.LobbyMember)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.LobbyMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_LobbyMembers");
        });

        _ = modelBuilder.Entity<User>(entity =>
        {
            _ = entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            _ = entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            _ = entity.Property(e => e.IsBot).HasDefaultValueSql("((0))");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
