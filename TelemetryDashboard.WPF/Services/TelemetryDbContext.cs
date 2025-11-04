using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Database context for telemetry data storage
    /// </summary>
    public class TelemetryDbContext : DbContext
    {
        // Legacy model for backward compatibility
        public DbSet<TelemetryData> TelemetryData { get; set; } = null!;

        // Comprehensive telemetry models
        public DbSet<ComprehensiveTelemetryData> ComprehensiveTelemetryData { get; set; } = null!;
        public DbSet<DerivedChannels> DerivedChannels { get; set; } = null!;

        // Lap and session models
        public DbSet<LapData> LapData { get; set; } = null!;
        public DbSet<SectorData> SectorData { get; set; } = null!;
        public DbSet<SessionData> SessionData { get; set; } = null!;

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
            // Legacy TelemetryData
            modelBuilder.Entity<TelemetryData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => e.Timestamp);
            });

            // ComprehensiveTelemetryData
            modelBuilder.Entity<ComprehensiveTelemetryData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.LapNumber);
            });

            // DerivedChannels
            modelBuilder.Entity<DerivedChannels>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => e.TelemetryDataId);
                entity.HasIndex(e => e.Timestamp);
            });

            // LapData
            modelBuilder.Entity<LapData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired();
                entity.Property(e => e.LapNumber).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.LapNumber);
            });

            // SectorData
            modelBuilder.Entity<SectorData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LapId).IsRequired();
                entity.Property(e => e.SectorNumber).IsRequired();
                entity.HasIndex(e => e.LapId);
            });

            // SessionData
            modelBuilder.Entity<SessionData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.HasIndex(e => e.StartTime);
            });
        }
    }
}
