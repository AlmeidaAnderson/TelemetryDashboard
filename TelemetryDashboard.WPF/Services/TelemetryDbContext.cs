using Microsoft.EntityFrameworkCore;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Database context for telemetry data storage
    /// </summary>
    public class TelemetryDbContext : DbContext
    {
        public DbSet<TelemetryData> TelemetryData { get; set; } = null!;

        public string DbPath { get; }

        public TelemetryDbContext()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(folder, "TelemetryDashboard");
            Directory.CreateDirectory(appFolder);
            DbPath = Path.Combine(appFolder, "telemetry.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TelemetryData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
