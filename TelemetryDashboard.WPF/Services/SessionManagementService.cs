using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Service for managing telemetry sessions
    /// </summary>
    public class SessionManagementService
    {
        private readonly string _sessionFolder;
        private SessionData? _currentSession;
        private readonly TelemetryDbContext _dbContext;

        // Services
        private readonly LapTimingService _lapTimingService;
        private readonly TrackMappingService _trackMappingService;
        private readonly MathEngineService _mathEngineService;
        private readonly DriverCoachingService _coachingService;

        // Buffers for current lap
        private List<ComprehensiveTelemetryData> _currentLapTelemetry = new();
        private List<DerivedChannels> _currentLapDerived = new();

        public SessionManagementService(
            TelemetryDbContext dbContext,
            LapTimingService lapTimingService,
            TrackMappingService trackMappingService,
            MathEngineService mathEngineService,
            DriverCoachingService coachingService)
        {
            _dbContext = dbContext;
            _lapTimingService = lapTimingService;
            _trackMappingService = trackMappingService;
            _mathEngineService = mathEngineService;
            _coachingService = coachingService;

            // Set up session folder
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _sessionFolder = Path.Combine(localAppData, "TelemetryDashboard", "Sessions");
            Directory.CreateDirectory(_sessionFolder);

            // Subscribe to lap timing events
            _lapTimingService.LapCompleted += OnLapCompleted;
        }

        /// <summary>
        /// Start a new session
        /// </summary>
        public async Task<SessionData> StartSessionAsync(
            string sessionName,
            string sessionType,
            string? trackName = null,
            string? vehicleName = null,
            string? driverName = null)
        {
            _currentSession = new SessionData
            {
                Name = sessionName,
                SessionType = sessionType,
                StartTime = DateTime.Now,
                TrackName = trackName,
                VehicleName = vehicleName,
                DriverName = driverName
            };

            // Save to database
            _dbContext.SessionData.Add(_currentSession);
            await _dbContext.SaveChangesAsync();

            // Create session folder
            string sessionFolderPath = Path.Combine(
                _sessionFolder,
                $"{_currentSession.StartTime:yyyyMMdd_HHmmss}_{sessionName}"
            );
            Directory.CreateDirectory(sessionFolderPath);
            _currentSession.DataFilePath = sessionFolderPath;

            // Start lap timing
            _lapTimingService.StartSession(_currentSession);

            // Clear buffers
            _currentLapTelemetry.Clear();
            _currentLapDerived.Clear();
            _mathEngineService.ClearBuffer();

            return _currentSession;
        }

        /// <summary>
        /// End current session
        /// </summary>
        public async Task EndSessionAsync()
        {
            if (_currentSession == null)
                return;

            _currentSession.EndTime = DateTime.Now;

            // Update session statistics
            var laps = _lapTimingService.GetSessionLaps();
            _currentSession.TotalLaps = laps.Count;
            _currentSession.ValidLaps = laps.Count(l => l.IsValid);

            var bestLap = laps.Where(l => l.IsValid).OrderBy(l => l.LapTime).FirstOrDefault();
            if (bestLap != null)
            {
                _currentSession.BestLapTime = bestLap.LapTime;
                _currentSession.BestLapNumber = bestLap.LapNumber;
            }

            if (laps.Any(l => l.IsValid))
            {
                _currentSession.AverageLapTime = laps.Where(l => l.IsValid).Average(l => l.LapTime);
            }

            // Save to database
            await _dbContext.SaveChangesAsync();

            _currentSession = null;
        }

        /// <summary>
        /// Process incoming telemetry data
        /// </summary>
        public async Task ProcessTelemetryAsync(ComprehensiveTelemetryData data)
        {
            if (_currentSession == null)
                return;

            // Set session ID
            data.SessionId = _currentSession.Id;

            // Calculate derived channels
            var derived = _mathEngineService.CalculateDerivedChannels(
                data,
                _currentLapTelemetry.LastOrDefault()
            );

            // Save to database
            _dbContext.ComprehensiveTelemetryData.Add(data);
            _dbContext.DerivedChannels.Add(derived);
            await _dbContext.SaveChangesAsync();

            // Update derived with ID
            derived.TelemetryDataId = data.Id;
            await _dbContext.SaveChangesAsync();

            // Add to buffers
            _currentLapTelemetry.Add(data);
            _currentLapDerived.Add(derived);

            // Process lap timing
            _lapTimingService.ProcessTelemetry(data);

            // Update track mapping
            _trackMappingService.AddPoint(data);
        }

        /// <summary>
        /// Handle lap completion
        /// </summary>
        private async void OnLapCompleted(LapData lap)
        {
            if (_currentSession == null)
                return;

            // Save lap to database
            _dbContext.LapData.Add(lap);
            await _dbContext.SaveChangesAsync();

            // Complete lap in track mapping
            _trackMappingService.CompleteLap();

            // Generate coaching analysis
            var analysis = _coachingService.AnalyzeLap(
                lap,
                _currentLapTelemetry,
                _currentLapDerived
            );

            // Clear buffers for next lap
            _currentLapTelemetry.Clear();
            _currentLapDerived.Clear();

            // Update session total laps
            _currentSession.TotalLaps++;
            if (lap.IsValid)
                _currentSession.ValidLaps++;

            if (lap.IsBestLap)
            {
                _currentSession.BestLapTime = lap.LapTime;
                _currentSession.BestLapNumber = lap.LapNumber;
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Get all sessions
        /// </summary>
        public async Task<List<SessionData>> GetAllSessionsAsync()
        {
            return await _dbContext.SessionData
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// Get session by ID
        /// </summary>
        public async Task<SessionData?> GetSessionAsync(int sessionId)
        {
            return await _dbContext.SessionData.FindAsync(sessionId);
        }

        /// <summary>
        /// Get laps for a session
        /// </summary>
        public async Task<List<LapData>> GetSessionLapsAsync(int sessionId)
        {
            return await _dbContext.LapData
                .Where(l => l.SessionId == sessionId)
                .OrderBy(l => l.LapNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get telemetry data for a lap
        /// </summary>
        public async Task<List<ComprehensiveTelemetryData>> GetLapTelemetryAsync(int lapId)
        {
            var lap = await _dbContext.LapData.FindAsync(lapId);
            if (lap == null)
                return new List<ComprehensiveTelemetryData>();

            return await _dbContext.ComprehensiveTelemetryData
                .Where(t => t.SessionId == lap.SessionId &&
                           t.Timestamp >= lap.StartTime &&
                           t.Timestamp <= lap.EndTime)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Get derived data for a lap
        /// </summary>
        public async Task<List<DerivedChannels>> GetLapDerivedDataAsync(int lapId)
        {
            var lap = await _dbContext.LapData.FindAsync(lapId);
            if (lap == null)
                return new List<DerivedChannels>();

            var telemetry = await GetLapTelemetryAsync(lapId);
            var telemetryIds = telemetry.Select(t => t.Id).ToList();

            return await _dbContext.DerivedChannels
                .Where(d => telemetryIds.Contains(d.TelemetryDataId))
                .OrderBy(d => d.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Delete a session and all its data
        /// </summary>
        public async Task DeleteSessionAsync(int sessionId)
        {
            var session = await _dbContext.SessionData.FindAsync(sessionId);
            if (session == null)
                return;

            // Delete all related data
            var laps = await _dbContext.LapData
                .Where(l => l.SessionId == sessionId)
                .ToListAsync();

            var telemetry = await _dbContext.ComprehensiveTelemetryData
                .Where(t => t.SessionId == sessionId)
                .ToListAsync();

            var telemetryIds = telemetry.Select(t => t.Id).ToList();
            var derived = await _dbContext.DerivedChannels
                .Where(d => telemetryIds.Contains(d.TelemetryDataId))
                .ToListAsync();

            _dbContext.DerivedChannels.RemoveRange(derived);
            _dbContext.ComprehensiveTelemetryData.RemoveRange(telemetry);
            _dbContext.LapData.RemoveRange(laps);
            _dbContext.SessionData.Remove(session);

            await _dbContext.SaveChangesAsync();

            // Delete session folder if it exists
            if (!string.IsNullOrEmpty(session.DataFilePath) && Directory.Exists(session.DataFilePath))
            {
                Directory.Delete(session.DataFilePath, true);
            }
        }

        /// <summary>
        /// Set track layout for current session
        /// </summary>
        public void SetTrackLayout(
            double startFinishLat,
            double startFinishLon,
            List<(double Lat, double Lon, int SectorNum)> sectorMarkers)
        {
            _lapTimingService.SetStartFinishLine(startFinishLat, startFinishLon);
            _lapTimingService.ClearSectorMarkers();

            foreach (var marker in sectorMarkers)
            {
                _lapTimingService.AddSectorMarker(marker.Lat, marker.Lon, marker.SectorNum);
            }

            if (_currentSession != null)
            {
                _currentSession.SectorCount = sectorMarkers.Count;
            }
        }

        /// <summary>
        /// Set reference lap for coaching
        /// </summary>
        public async Task SetReferenceLapAsync(int lapId)
        {
            var lap = await _dbContext.LapData.FindAsync(lapId);
            if (lap == null)
                return;

            var telemetry = await GetLapTelemetryAsync(lapId);

            _lapTimingService.SetReferenceLap(lap, telemetry);
            _coachingService.SetReferenceLap(lap, telemetry);
        }

        /// <summary>
        /// Get current session
        /// </summary>
        public SessionData? GetCurrentSession() => _currentSession;

        /// <summary>
        /// Check if a session is active
        /// </summary>
        public bool IsSessionActive() => _currentSession != null;
    }
}
