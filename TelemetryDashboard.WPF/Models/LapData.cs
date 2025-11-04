using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TelemetryDashboard.Models
{
    /// <summary>
    /// Represents a single lap with timing and analysis data
    /// </summary>
    public class LapData
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public int LapNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // ==================== Timing ====================
        public float LapTime { get; set; }  // seconds
        public float? DeltaToBest { get; set; }  // seconds
        public float? DeltaToPrevious { get; set; }  // seconds
        public float? DeltaToReference { get; set; }  // seconds
        public bool IsValid { get; set; }  // False if invalid (off-track, pit entry, etc.)
        public bool IsBestLap { get; set; }

        // ==================== Sector Times ====================
        public float? Sector1Time { get; set; }
        public float? Sector2Time { get; set; }
        public float? Sector3Time { get; set; }
        public float? Sector4Time { get; set; }
        public float? Sector5Time { get; set; }

        // ==================== Sector Deltas ====================
        public float? Sector1Delta { get; set; }
        public float? Sector2Delta { get; set; }
        public float? Sector3Delta { get; set; }
        public float? Sector4Delta { get; set; }
        public float? Sector5Delta { get; set; }

        // ==================== Performance Metrics ====================
        public float? MaxSpeed { get; set; }  // km/h
        public float? MinSpeed { get; set; }  // km/h
        public float? AverageSpeed { get; set; }  // km/h
        public float? MaxRPM { get; set; }
        public float? MaxLongG { get; set; }  // G
        public float? MaxLatG { get; set; }  // G
        public float? MaxCombinedG { get; set; }  // G

        // ==================== Fuel & Energy ====================
        public float? FuelUsed { get; set; }  // Liters
        public float? AverageFuelConsumption { get; set; }  // L/lap

        // ==================== Temperature ====================
        public float? MaxCoolantTemp { get; set; }
        public float? MaxOilTemp { get; set; }
        public float? AvgTireTempFL { get; set; }
        public float? AvgTireTempFR { get; set; }
        public float? AvgTireTempRL { get; set; }
        public float? AvgTireTempRR { get; set; }

        // ==================== Driver Performance ====================
        public float? ThrottleApplicationScore { get; set; }  // 0-100
        public float? BrakingScore { get; set; }  // 0-100
        public float? CorneringScore { get; set; }  // 0-100
        public float? ConsistencyScore { get; set; }  // 0-100
        public float? OverallScore { get; set; }  // 0-100

        // ==================== Conditions ====================
        public float? AmbientTemp { get; set; }
        public float? TrackTemp { get; set; }
        public string? WeatherCondition { get; set; }  // Dry, Wet, etc.

        // ==================== Incidents ====================
        public int SpinCount { get; set; }
        public int OffTrackCount { get; set; }
        public bool YellowFlag { get; set; }
        public bool RedFlag { get; set; }

        // ==================== Notes ====================
        public string? Notes { get; set; }
        public string? SetupChanges { get; set; }

        public LapData()
        {
            StartTime = DateTime.Now;
            IsValid = true;
        }
    }

    /// <summary>
    /// Represents a sector within a lap
    /// </summary>
    public class SectorData
    {
        [Key]
        public int Id { get; set; }

        public int LapId { get; set; }
        public int SectorNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public float SectorTime { get; set; }  // seconds
        public float? Delta { get; set; }  // vs reference
        public float StartDistance { get; set; }  // meters
        public float EndDistance { get; set; }  // meters

        public float? MinSpeed { get; set; }
        public float? MaxSpeed { get; set; }
        public float? AverageSpeed { get; set; }

        public bool IsPersonalBest { get; set; }
        public bool IsValid { get; set; }

        public SectorData()
        {
            IsValid = true;
        }
    }

    /// <summary>
    /// Represents a racing session (practice, qualifying, race, etc.)
    /// </summary>
    public class SessionData
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string SessionType { get; set; } = "Practice";  // Practice, Qualifying, Race, Test
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // ==================== Track Information ====================
        public string? TrackName { get; set; }
        public string? TrackConfiguration { get; set; }
        public float? TrackLength { get; set; }  // meters
        public int? SectorCount { get; set; }

        // ==================== Vehicle Information ====================
        public string? VehicleName { get; set; }
        public string? VehicleClass { get; set; }
        public string? SetupName { get; set; }

        // ==================== Driver Information ====================
        public string? DriverName { get; set; }
        public string? Team { get; set; }

        // ==================== Session Summary ====================
        public int TotalLaps { get; set; }
        public int ValidLaps { get; set; }
        public float? BestLapTime { get; set; }
        public int? BestLapNumber { get; set; }
        public float? AverageLapTime { get; set; }

        // ==================== Conditions ====================
        public float? SessionAmbientTemp { get; set; }
        public float? SessionTrackTemp { get; set; }
        public string? WeatherCondition { get; set; }

        // ==================== File Paths ====================
        public string? DataFilePath { get; set; }
        public string? LogFilePath { get; set; }

        // ==================== Notes ====================
        public string? Notes { get; set; }
        public string? SetupNotes { get; set; }

        public SessionData()
        {
            StartTime = DateTime.Now;
        }
    }
}
