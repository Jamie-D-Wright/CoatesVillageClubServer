using Microsoft.EntityFrameworkCore;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Data.Entities;

namespace VillageClub.Membership.Data;

/// <summary>
/// Database context for the Membership service
/// Handles Users, Authentication, and Audit Logging
/// </summary>
public class MembershipDbContext : DbContext
{
    public MembershipDbContext(DbContextOptions<MembershipDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for Membership service
        modelBuilder.HasDefaultSchema("Membership");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);
            
            entity.Property(e => e.Address)
                .HasMaxLength(500);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();
            
            entity.Property(e => e.CommitteeRole)
                .HasMaxLength(50)
                .HasConversion<string>();
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(UserStatus.Active)
                .HasConversion<string>();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
            
            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_Users_Role");
            
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Users_Status");
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.ExpiresAt)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.ReplacedByToken)
                .HasMaxLength(255);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");
            
            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            // Foreign key
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Details)
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            // Indexes
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp");
            
            entity.HasIndex(e => e.ActorId)
                .HasDatabaseName("IX_AuditLogs_ActorId");
            
            entity.HasIndex(e => e.TargetUserId)
                .HasDatabaseName("IX_AuditLogs_TargetUserId");

            // Foreign keys (nullable - can be system actions)
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
