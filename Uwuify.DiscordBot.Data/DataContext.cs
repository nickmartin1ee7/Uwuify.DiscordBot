using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Uwuify.DiscordBot.Data;

public abstract record BaseAuditable
{
    public DateTime AuditCreation { get; set; }
    public DateTime AuditLastUpdate { get; set; }
}

public record UptimeReport : BaseAuditable
{
    public int Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string EndReason { get; set; }
}

public record Guild : BaseAuditable
{
    public int Id { get; set; }
    public ulong Snowflake { get; set; }
    public string GuildName { get; set; }
    public bool Active { get; set; }
    public uint UserCount { get; set; }
}

public record User : BaseAuditable
{
    public int Id { get; set; }
    public ulong Snowflake { get; set; }
    public string Username { get; set; }
    public Guild Guild { get; set; }
    public uint CommandUses { get; set; }
}

public record RateLimitProfile : BaseAuditable
{
    public int Id { get; set; }
    public ulong Snowflake { get; set; }
    [NotMapped]
    public uint CommandUses => (uint)(UsesInUtc?.Count ?? 0L);
    public List<DateTime> UsesInUtc { get; set; }
}

public class DataContext : DbContext
{
    public DbSet<UptimeReport> UptimeReports { get; set; }
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RateLimitProfile> RateLimitProfiles { get; set; }

    public DataContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Not meant to be called except by EF Core Designer
    /// </summary>
    public DataContext()
    {
#if !EFDESIGNER
        throw new ApplicationException("This .ctor is not meant to be called except by EF Core Designer. Have you set this project's compiler directive correctly?");
#endif
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if EFDESIGNER
        // Use only in EF Core Designer to create a migration
        optionsBuilder.UseNpgsql(
            connectionString: $"Server=localhost;Port=5432;Database={nameof(DataContext)};User Id=sa;Password=sa;Include Error Detail=True;");
#endif
    }

    public override int SaveChanges()
    {
        UpdateAuditRecords();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditRecords();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateAuditRecords();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateAuditRecords()
    {
        var utcNow = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<BaseAuditable>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.AuditCreation = utcNow;
                entry.Entity.AuditLastUpdate = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AuditLastUpdate = utcNow;
            }
        }
    }
}
