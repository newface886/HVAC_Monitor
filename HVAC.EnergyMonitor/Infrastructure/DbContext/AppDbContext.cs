using HVAC.EnergyMonitor.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HVAC.EnergyMonitor.Infrastructure.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Point> Points => Set<Point>();
    public DbSet<PointValue> PointValues => Set<PointValue>();
    public DbSet<AlarmRule> AlarmRules => Set<AlarmRule>();
    public DbSet<AlarmRecord> AlarmRecords => Set<AlarmRecord>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.SerialPortName).HasMaxLength(50);
        });

        modelBuilder.Entity<Point>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.HasOne(e => e.Device)
                  .WithMany(d => d.Points)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PointValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PointId, e.Timestamp });
            entity.HasOne(e => e.Point)
                  .WithMany(p => p.Values)
                  .HasForeignKey(e => e.PointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlarmRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Point)
                  .WithMany(p => p.AlarmRules)
                  .HasForeignKey(e => e.PointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlarmRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TriggerTime);
            entity.HasOne(e => e.Point)
                  .WithMany()
                  .HasForeignKey(e => e.PointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TableName)
                  .HasMaxLength(50)
                  .IsRequired();
            entity.HasIndex(e => e.TableName)
                  .IsUnique();
        });
    }
}
