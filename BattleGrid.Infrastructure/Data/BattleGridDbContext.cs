using BattleGrid.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BattleGrid.Infrastructure.Data;

public sealed class BattleGridDbContext : DbContext
{
    public BattleGridDbContext(DbContextOptions<BattleGridDbContext> options) : base(options)
    {
    }

    public DbSet<User> User => Set<User>();
    public DbSet<Session> Session => Set<Session>();
    public DbSet<Match> Match => Set<Match>();
    public DbSet<MatchMove> MatchMove => Set<MatchMove>();
    public DbSet<ShipType> ShipType => Set<ShipType>();
    public DbSet<ShipPlacement> ShipPlacement => Set<ShipPlacement>();
    public DbSet<Spectator> Spectator => Set<Spectator>();
    public DbSet<MatchmakingQueue> MatchmakingQueue => Set<MatchmakingQueue>();
    public DbSet<BanList> BanList => Set<BanList>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(x => x.UserID);

            entity.Property(x => x.UserID).HasColumnName("UserID");
            entity.Property(x => x.UserName).HasColumnName("UserName");
            entity.Property(x => x.Email).HasColumnName("Email");
            entity.Property(x => x.PasswordHash).HasColumnName("PasswordHash");
            entity.Property(x => x.Role).HasColumnName("Role");
            entity.Property(x => x.Rating).HasColumnName("Rating");
            entity.Property(x => x.MatchesPlayed).HasColumnName("MatchesPlayed");
            entity.Property(x => x.IsBanned).HasColumnName("IsBanned");
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.LastUpdatedAt).HasColumnName("LastUpdatedAt");

            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");
            entity.HasKey(x => x.SessionID);

            entity.Property(x => x.SessionID).HasColumnName("SessionID");
            entity.Property(x => x.UserID).HasColumnName("UserID");
            entity.Property(x => x.RefreshToken).HasColumnName("RefreshToken");
            entity.Property(x => x.RT_ExpiresAt).HasColumnName("RT_ExpiresAt");
            entity.Property(x => x.AccessToken).HasColumnName("AccessToken");
            entity.Property(x => x.AT_ExpiresAt).HasColumnName("AT_ExpiresAt");
            entity.Property(x => x.LastLogin).HasColumnName("LastLogin");
            entity.Property(x => x.IsRevoked).HasColumnName("IsRevoked");
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.LastUpdatedAt).HasColumnName("LastUpdatedAt");

            entity.HasIndex(x => x.RefreshToken).IsUnique();
            entity.HasIndex(x => x.AccessToken).IsUnique();

            entity.HasOne<User>()
                .WithOne()
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("Match");
            entity.HasKey(x => x.MatchID);

            entity.Property(x => x.MatchID).HasColumnName("MatchID");
            entity.Property(x => x.Player1ID).HasColumnName("Player1ID");
            entity.Property(x => x.Player2ID).HasColumnName("Player2ID");
            entity.Property(x => x.P1RatingChange).HasColumnName("P1RatingChange");
            entity.Property(x => x.P2RatingChange).HasColumnName("P2RatingChange");
            entity.Property(x => x.Status).HasColumnName("Status");
            entity.Property(x => x.TotalNoOfTurns).HasColumnName("TotalNoOfTurns");
            entity.Property(x => x.FinishReason).HasColumnName("FinishReason");
            entity.Property(x => x.StartedAt).HasColumnName("StartedAt");
            entity.Property(x => x.FinishedAt).HasColumnName("FinishedAt");

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
            entity.ToTable("MatchMove");
            entity.HasKey(x => x.MoveID);

            entity.Property(x => x.MoveID).HasColumnName("MoveID");
            entity.Property(x => x.PlayerID).HasColumnName("PlayerID");
            entity.Property(x => x.MatchID).HasColumnName("MatchID");
            entity.Property(x => x.MoveNumber).HasColumnName("MoveNumber");
            entity.Property(x => x.HitX).HasColumnName("HitX");
            entity.Property(x => x.HitY).HasColumnName("HitY");
            entity.Property(x => x.Result).HasColumnName("Result");
            entity.Property(x => x.TimeOfMove).HasColumnName("TimeOfMove");

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
            entity.ToTable("ShipType");
            entity.HasKey(x => x.ShipID);

            entity.Property(x => x.ShipID).HasColumnName("ShipID");
            entity.Property(x => x.ShipName).HasColumnName("ShipName");
            entity.Property(x => x.Length).HasColumnName("Length");
            entity.Property(x => x.Width).HasColumnName("Width");
            entity.Property(x => x.MaxPerPlayer).HasColumnName("MaxPerPlayer");
        });

        modelBuilder.Entity<ShipPlacement>(entity =>
        {
            entity.ToTable("ShipPlacement");
            entity.HasKey(x => x.PlacementID);

            entity.Property(x => x.PlacementID).HasColumnName("PlacementID");
            entity.Property(x => x.PlayerID).HasColumnName("PlayerID");
            entity.Property(x => x.MatchID).HasColumnName("MatchID");
            entity.Property(x => x.ShipID).HasColumnName("ShipID");
            entity.Property(x => x.StartX).HasColumnName("StartX");
            entity.Property(x => x.StartY).HasColumnName("StartY");
            entity.Property(x => x.IsVertical).HasColumnName("IsVertical");
            entity.Property(x => x.PlacedAt).HasColumnName("PlacedAt");

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
            entity.ToTable("Spectator");
            entity.HasKey(x => x.SpectatorID);

            entity.Property(x => x.SpectatorID).HasColumnName("SpectatorID");
            entity.Property(x => x.MatchID).HasColumnName("MatchID");
            entity.Property(x => x.JoinedAt).HasColumnName("JoinedAt");
            entity.Property(x => x.Duration).HasColumnName("Duration");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SpectatorID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany<Match>()
                .WithOne()
                .HasForeignKey(x => x.MatchID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MatchmakingQueue>(entity =>
        {
            entity.ToTable("MatchmakingQueue");
            entity.HasKey(x => x.QueueID);

            entity.Property(x => x.QueueID).HasColumnName("QueueID");
            entity.Property(x => x.PlayerID).HasColumnName("PlayerID");
            entity.Property(x => x.JoinedAt).HasColumnName("JoinedAt");

            entity.HasOne<User>()
                .WithOne()
                .HasForeignKey<MatchmakingQueue>(x => x.PlayerID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BanList>(entity =>
        {
            entity.ToTable("BanList");
            entity.HasKey(x => x.BanID);

            entity.Property(x => x.BanID).HasColumnName("BanID");
            entity.Property(x => x.AdminID).HasColumnName("AdminID");
            entity.Property(x => x.PlayerID).HasColumnName("PLayerID");
            entity.Property(x => x.IsReverted).HasColumnName("IsReverted");
            entity.Property(x => x.Reason).HasColumnName("Reason");
            entity.Property(x => x.IsTemporary).HasColumnName("IsTemporary");
            entity.Property(x => x.BannedAt).HasColumnName("BannedAt");
            entity.Property(x => x.Duration).HasColumnName("Duration");
            entity.Property(x => x.BannedUntil).HasColumnName("BannedUntil");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AdminID)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.PlayerID)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}