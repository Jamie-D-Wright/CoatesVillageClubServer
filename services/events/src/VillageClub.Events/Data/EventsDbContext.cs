using Microsoft.EntityFrameworkCore;
using VillageClub.Events.Data.Entities;

namespace VillageClub.Events.Data;

/// <summary>
/// Database context for the Events service.
/// Handles events, event types, and event-related data.
/// </summary>
public class EventsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventsDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public EventsDbContext(DbContextOptions<EventsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the Events DbSet.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Configures the schema and entity relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Detect database provider for provider-specific configuration
        var isSqlServer = Database.IsSqlServer();

        // Set default schema for Events service (SQL Server only - SQLite doesn't support schemas)
        if (isSqlServer)
        {
            modelBuilder.HasDefaultSchema("Events");
        }

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            // Description column type: SQL Server uses NVARCHAR(MAX), SQLite uses TEXT
            if (isSqlServer)
            {
                entity.Property(e => e.Description)
                    .HasColumnType("NVARCHAR(MAX)");
            }
            else
            {
                entity.Property(e => e.Description); // SQLite will use TEXT by default
            }
            
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.StartDateTime)
                .IsRequired();
            
            entity.Property(e => e.EndDateTime)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Draft");
            
            entity.Property(e => e.CreatedById)
                .IsRequired();
            
            // Timestamp defaults: SQL Server uses GETUTCDATE(), SQLite will use application default
            if (isSqlServer)
            {
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
            }
            else
            {
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                
                entity.Property(e => e.UpdatedAt)
                    .IsRequired();
            }
            
            entity.Property(e => e.PublishedAt);

            // Indexes per data-model.md
            entity.HasIndex(e => e.StartDateTime)
                .HasDatabaseName("IX_Events_StartDateTime");
            
            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("IX_Events_EventType");
            
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Events_Status");
        });
    }
}
