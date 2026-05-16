using BattleGrid.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Infrastructure.Data;

public sealed class BattleGridDbContext : DbContext
{
    public BattleGridDbContext(DbContextOptions<BattleGridDbContext> options) : base(options)
    {
    }

    public DbSet<User> User => Set<User>();
    public DbSet<PlayerStat> PlayerStat => Set<PlayerStat>();
    public DbSet<Session> Session => Set<Session>();
    public DbSet<Match> Match => Set<Match>();
    public DbSet<MatchMove> MatchMove => Set<MatchMove>();
    public DbSet<ShipType> ShipType => Set<ShipType>();
    public DbSet<ShipPlacement> ShipPlacement => Set<ShipPlacement>();
    public DbSet<Spectator> Spectator => Set<Spectator>();  
    public DbSet<BanList> BanList => Set<BanList>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.UserID);

            entity.Property(x => x.UserName).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PlayerStat>(entity =>
        {
            entity.HasKey(x => x.StatID);

            entity.Property(x => x.WinRate).ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(x => x.MatchesPlayed);
            entity.HasIndex(x => x.MatchesWon);
            entity.HasIndex(x => x.WinRate);
            entity.HasIndex(x => x.Rating);
            entity.HasIndex(x => x.HighestRating);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(x => x.SessionID);

            entity.HasIndex(x => x.RefreshToken).IsUnique();
            entity.HasIndex(x => x.RT_ExpiresAt);
            entity.HasIndex(x => x.AccessToken).IsUnique();
            entity.HasIndex(x => x.AT_ExpiresAt);

            entity.HasOne<User>()
                .WithOne()
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(x => x.MatchID);

            entity.HasIndex(x => x.Player1ID);
            entity.HasIndex(x => x.Player2ID);
            entity.HasIndex(x => x.StartedAt);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.Player1ID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.Player2ID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MatchMove>(entity =>
        {
            entity.HasKey(x => x.MoveID);

            entity.HasIndex(x => x.PlayerID);
            entity.HasIndex(x => x.MatchID);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.PlayerID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Match>()
                .WithMany()
                .HasForeignKey(x => x.MatchID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ShipType>(entity => 
        {
            entity.HasKey(x => x.ShipID);
        });

        modelBuilder.Entity<ShipPlacement>(entity =>
        {
            entity.HasKey(x => x.PlacementID);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.PlayerID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Match>()
                .WithMany()
                .HasForeignKey(x => x.MatchID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShipType>()
                .WithMany()
                .HasForeignKey(x => x.ShipID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Spectator>(entity =>
        {
            entity.HasKey(x => new { x.SpectatorID, x.MatchID });

            entity.HasIndex(x => new { x.SpectatorID, x.MatchID }).IsUnique();

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SpectatorID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Match>()
                .WithMany()
                .HasForeignKey(x => x.MatchID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BanList>(entity =>
        {
            entity.HasKey(x => x.BanID);

            entity.HasIndex(x => x.AdminID);
            entity.HasIndex(x => x.PlayerID);
            entity.HasIndex(x => x.BannedAt);
            entity.HasIndex(x => x.Duration);
            entity.HasIndex(x => x.BannedUntil);
            entity.HasIndex(x => new { x.IsTemporary, x.BannedAt });
            entity.HasIndex(x => new { x.IsTemporary, x.Duration });
            entity.HasIndex(x => new { x.IsTemporary, x.BannedUntil });
            entity.HasIndex(x => new { x.IsTemporary, x.BannedAt, x.Duration });
            entity.HasIndex(x => x.RevertingAdminID);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AdminID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.PlayerID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.RevertingAdminID)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}