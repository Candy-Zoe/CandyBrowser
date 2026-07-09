using Microsoft.EntityFrameworkCore;
using CandyBrowser.Data.Entities;

namespace CandyBrowser.Data.Contexts;

public class BrowserDbContext : DbContext
{
    public DbSet<BookmarkEntity> Bookmarks => Set<BookmarkEntity>();
    public DbSet<HistoryEntity> History => Set<HistoryEntity>();
    public DbSet<PasswordEntity> Passwords => Set<PasswordEntity>();
    public DbSet<TabEntity> Tabs => Set<TabEntity>();
    public DbSet<SettingEntity> Settings => Set<SettingEntity>();
    public DbSet<ExtensionEntity> Extensions => Set<ExtensionEntity>();

    public BrowserDbContext(DbContextOptions<BrowserDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookmarkEntity>(entity =>
        {
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.Url);
        });

        modelBuilder.Entity<HistoryEntity>(entity =>
        {
            entity.HasIndex(e => e.Url);
            entity.HasIndex(e => e.LastVisit);
        });

        modelBuilder.Entity<PasswordEntity>(entity =>
        {
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => new { e.Domain, e.Username }).IsUnique();
        });

        modelBuilder.Entity<TabEntity>(entity =>
        {
            entity.HasIndex(e => e.WindowId);
            entity.HasIndex(e => e.Position);
        });

        modelBuilder.Entity<SettingEntity>(entity =>
        {
            entity.HasKey(e => e.Key);
        });

        modelBuilder.Entity<ExtensionEntity>(entity =>
        {
            entity.HasIndex(e => e.ExtensionId).IsUnique();
        });
    }
}
