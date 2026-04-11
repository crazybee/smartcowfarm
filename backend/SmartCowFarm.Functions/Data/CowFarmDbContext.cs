using Microsoft.EntityFrameworkCore;
using SmartCowFarm.Functions.Models;

namespace SmartCowFarm.Functions.Data;

public class CowFarmDbContext(DbContextOptions<CowFarmDbContext> options) : DbContext(options)
{
    public DbSet<Cow> Cows => Set<Cow>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cow>(entity =>
        {
            entity.HasKey(e => e.CowId);
            entity.Property(e => e.Gender).HasConversion<string>();
            entity.HasIndex(e => e.NextVaxDue);
            entity.HasMany(e => e.VaccinationRecords)
                  .WithOne(v => v.Cow)
                  .HasForeignKey(v => v.CowId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId);
            entity.HasIndex(e => e.CowId);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.AlertId);
            entity.Property(e => e.AlertType).HasConversion<string>();
            entity.HasIndex(e => new { e.CowId, e.IsResolved });
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
