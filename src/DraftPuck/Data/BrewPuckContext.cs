using DraftPuck.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DraftPuck.Data;

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

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Drink>(entity =>
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

        modelBuilder.Entity<Lobby>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.JoinCode).HasMaxLength(4);
            entity.Property(e => e.PicksPerTeam).HasDefaultValueSql("((1))");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.CreatedLobbies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lobbies_People");
        });

        modelBuilder.Entity<LobbyEvent>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsSent).HasDefaultValueSql("((0))");
            entity.Property(e => e.SendAttempts).HasDefaultValueSql("((0))");
        });

        modelBuilder.Entity<LobbyMember>(entity =>
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

        modelBuilder.Entity<LobbyMemberPick>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");

            entity.HasOne(d => d.LobbyMember).WithMany(p => p.LobbyMemberPicks)
                .HasForeignKey(d => d.LobbyMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LobbyMemberPicks_LobbyMembers");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Created).HasDefaultValueSql("(getutcdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
