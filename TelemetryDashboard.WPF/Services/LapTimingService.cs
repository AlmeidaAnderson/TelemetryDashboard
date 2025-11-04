using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Service for lap timing, sector detection, and lap analysis
    /// </summary>
    public class LapTimingService
    {
        // Start/Finish line coordinates
        private double? _startFinishLat;
        private double? _startFinishLon;
        private float _startFinishTolerance = 20f;  // meters

        // Sector markers
        private List<(double Lat, double Lon, int SectorNumber)> _sectorMarkers = new();

        // Current lap tracking
        private LapData? _currentLap;
        private Dictionary<int, float> _currentSectorTimes = new();
        private DateTime? _lastSectorTime;
        private int _currentSectorNumber = 0;

        // Reference lap (for delta calculation)
        private LapData? _referenceLap;
        private List<ComprehensiveTelemetryData>? _referenceLapData;

        // Session tracking
        private SessionData? _currentSession;
        private List<LapData> _sessionLaps = new();
        private float? _bestLapTime;

        // Events
        public event Action<LapData>? LapCompleted;
        public event Action<int, float>? SectorCompleted;
        public event Action<float>? LapDeltaUpdated;
        public event Action<LapData>? NewBestLap;

        /// <summary>
        /// Set the start/finish line location
        /// </summary>
        public void SetStartFinishLine(double latitude, double longitude, float tolerance = 20f)
        {
            _startFinishLat = latitude;
            _startFinishLon = longitude;
            _startFinishTolerance = tolerance;
        }

        /// <summary>
        /// Add a sector marker
        /// </summary>
        public void AddSectorMarker(double latitude, double longitude, int sectorNumber)
        {
            _sectorMarkers.Add((latitude, longitude, sectorNumber));
            _sectorMarkers = _sectorMarkers.OrderBy(s => s.SectorNumber).ToList();
        }

        /// <summary>
        /// Clear all sector markers
        /// </summary>
        public void ClearSectorMarkers()
        {
            _sectorMarkers.Clear();
        }

        /// <summary>
        /// Start a new session
        /// </summary>
        public void StartSession(SessionData session)
        {
            _currentSession = session;
            _sessionLaps.Clear();
            _currentLap = null;
            _bestLapTime = null;
        }

        /// <summary>
        /// Set reference lap for delta calculation
        /// </summary>
        public void SetReferenceLap(LapData lap, List<ComprehensiveTelemetryData> lapData)
        {
            _referenceLap = lap;
            _referenceLapData = lapData;
        }

        /// <summary>
        /// Process telemetry data for lap timing
        /// </summary>
        public void ProcessTelemetry(ComprehensiveTelemetryData data)
        {
            if (_currentSession == null)
                return;

            // Check for start/finish line crossing
            if (IsAtStartFinish(data))
            {
                CompleteLap(data);
                StartNewLap(data);
            }

            // Check for sector crossings
            CheckSectorCrossing(data);

            // Update current lap data
            if (_currentLap != null)
            {
                UpdateCurrentLap(data);
            }

            // Calculate delta vs reference lap
            if (_referenceLap != null && _referenceLapData != null)
            {
                CalculateLapDelta(data);
            }
        }

        /// <summary>
        /// Check if telemetry data is at start/finish line
        /// </summary>
        private bool IsAtStartFinish(ComprehensiveTelemetryData data)
        {
            if (!_startFinishLat.HasValue || !_startFinishLon.HasValue)
                return false;

            if (!data.Latitude.HasValue || !data.Longitude.HasValue)
                return false;

            float distance = CalculateDistance(
                data.Latitude.Value, data.Longitude.Value,
                _startFinishLat.Value, _startFinishLon.Value
            );

            return distance < _startFinishTolerance;
        }

        /// <summary>
        /// Start a new lap
        /// </summary>
        private void StartNewLap(ComprehensiveTelemetryData data)
        {
            _currentLap = new LapData
            {
                SessionId = _currentSession!.Id,
                LapNumber = _sessionLaps.Count + 1,
                StartTime = data.Timestamp,
                IsValid = true
            };

            _currentSectorTimes.Clear();
            _lastSectorTime = data.Timestamp;
            _currentSectorNumber = 0;
        }

        /// <summary>
        /// Complete the current lap
        /// </summary>
        private void CompleteLap(ComprehensiveTelemetryData data)
        {
            if (_currentLap == null)
                return;

            _currentLap.EndTime = data.Timestamp;
            _currentLap.LapTime = (float)(_currentLap.EndTime.Value - _currentLap.StartTime).TotalSeconds;

            // Set sector times
            AssignSectorTimes(_currentLap);

            // Calculate deltas
            if (_sessionLaps.Any())
            {
                var previousLap = _sessionLaps.Last();
                _currentLap.DeltaToPrevious = _currentLap.LapTime - previousLap.LapTime;
            }

            if (_bestLapTime.HasValue)
            {
                _currentLap.DeltaToBest = _currentLap.LapTime - _bestLapTime.Value;
            }

            if (_referenceLap != null)
            {
                _currentLap.DeltaToReference = _currentLap.LapTime - _referenceLap.LapTime;
            }

            // Check if this is a new best lap
            if (!_bestLapTime.HasValue || _currentLap.LapTime < _bestLapTime.Value)
            {
                _bestLapTime = _currentLap.LapTime;
                _currentLap.IsBestLap = true;
                NewBestLap?.Invoke(_currentLap);
            }

            _sessionLaps.Add(_currentLap);
            LapCompleted?.Invoke(_currentLap);
        }

        /// <summary>
        /// Check if vehicle crossed a sector marker
        /// </summary>
        private void CheckSectorCrossing(ComprehensiveTelemetryData data)
        {
            if (_currentLap == null || !_lastSectorTime.HasValue)
                return;

            if (!data.Latitude.HasValue || !data.Longitude.HasValue)
                return;

            foreach (var marker in _sectorMarkers)
            {
                if (marker.SectorNumber <= _currentSectorNumber)
                    continue;  // Already passed this sector

                float distance = CalculateDistance(
                    data.Latitude.Value, data.Longitude.Value,
                    marker.Lat, marker.Lon
                );

                if (distance < _startFinishTolerance)
                {
                    // Sector crossed!
                    float sectorTime = (float)(data.Timestamp - _lastSectorTime.Value).TotalSeconds;
                    _currentSectorTimes[marker.SectorNumber] = sectorTime;
                    _currentSectorNumber = marker.SectorNumber;
                    _lastSectorTime = data.Timestamp;

                    SectorCompleted?.Invoke(marker.SectorNumber, sectorTime);
                    break;  // Only one sector at a time
                }
            }
        }

        /// <summary>
        /// Assign sector times to lap
        /// </summary>
        private void AssignSectorTimes(LapData lap)
        {
            if (_currentSectorTimes.TryGetValue(1, out float s1))
                lap.Sector1Time = s1;
            if (_currentSectorTimes.TryGetValue(2, out float s2))
                lap.Sector2Time = s2;
            if (_currentSectorTimes.TryGetValue(3, out float s3))
                lap.Sector3Time = s3;
            if (_currentSectorTimes.TryGetValue(4, out float s4))
                lap.Sector4Time = s4;
            if (_currentSectorTimes.TryGetValue(5, out float s5))
                lap.Sector5Time = s5;

            // Calculate sector deltas vs reference
            if (_referenceLap != null)
            {
                if (lap.Sector1Time.HasValue && _referenceLap.Sector1Time.HasValue)
                    lap.Sector1Delta = lap.Sector1Time.Value - _referenceLap.Sector1Time.Value;
                if (lap.Sector2Time.HasValue && _referenceLap.Sector2Time.HasValue)
                    lap.Sector2Delta = lap.Sector2Time.Value - _referenceLap.Sector2Time.Value;
                if (lap.Sector3Time.HasValue && _referenceLap.Sector3Time.HasValue)
                    lap.Sector3Delta = lap.Sector3Time.Value - _referenceLap.Sector3Time.Value;
                if (lap.Sector4Time.HasValue && _referenceLap.Sector4Time.HasValue)
                    lap.Sector4Delta = lap.Sector4Time.Value - _referenceLap.Sector4Time.Value;
                if (lap.Sector5Time.HasValue && _referenceLap.Sector5Time.HasValue)
                    lap.Sector5Delta = lap.Sector5Time.Value - _referenceLap.Sector5Time.Value;
            }
        }

        /// <summary>
        /// Update current lap statistics
        /// </summary>
        private void UpdateCurrentLap(ComprehensiveTelemetryData data)
        {
            if (_currentLap == null)
                return;

            // Update max/min values
            if (data.GpsSpeed.HasValue)
            {
                if (!_currentLap.MaxSpeed.HasValue || data.GpsSpeed.Value > _currentLap.MaxSpeed.Value)
                    _currentLap.MaxSpeed = data.GpsSpeed.Value;

                if (!_currentLap.MinSpeed.HasValue || data.GpsSpeed.Value < _currentLap.MinSpeed.Value)
                    _currentLap.MinSpeed = data.GpsSpeed.Value;
            }

            if (data.EngineRPM.HasValue)
            {
                if (!_currentLap.MaxRPM.HasValue || data.EngineRPM.Value > _currentLap.MaxRPM.Value)
                    _currentLap.MaxRPM = data.EngineRPM.Value;
            }

            if (data.AccelX.HasValue)
            {
                float longG = Math.Abs(data.AccelX.Value / 9.81f);
                if (!_currentLap.MaxLongG.HasValue || longG > _currentLap.MaxLongG.Value)
                    _currentLap.MaxLongG = longG;
            }

            if (data.AccelY.HasValue)
            {
                float latG = Math.Abs(data.AccelY.Value / 9.81f);
                if (!_currentLap.MaxLatG.HasValue || latG > _currentLap.MaxLatG.Value)
                    _currentLap.MaxLatG = latG;
            }
        }

        /// <summary>
        /// Calculate delta vs reference lap
        /// </summary>
        private void CalculateLapDelta(ComprehensiveTelemetryData data)
        {
            if (_referenceLapData == null || !_lastSectorTime.HasValue)
                return;

            // Find corresponding point in reference lap based on time into lap
            float timeIntoLap = (float)(data.Timestamp - _lastSectorTime.Value).TotalSeconds;

            // Find closest reference point
            var referencePoint = _referenceLapData
                .OrderBy(d => Math.Abs((d.Timestamp - _referenceLapData[0].Timestamp).TotalSeconds - timeIntoLap))
                .FirstOrDefault();

            if (referencePoint != null)
            {
                // Calculate time delta based on distance covered
                // This is a simplified approach - real implementation would interpolate
                float delta = timeIntoLap - (float)(referencePoint.Timestamp - _referenceLapData[0].Timestamp).TotalSeconds;
                LapDeltaUpdated?.Invoke(delta);
            }
        }

        /// <summary>
        /// Calculate distance between two GPS coordinates (Haversine formula)
        /// </summary>
        private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;  // Earth radius in meters

            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(R * c);
        }

        /// <summary>
        /// Get all laps for current session
        /// </summary>
        public List<LapData> GetSessionLaps() => _sessionLaps.ToList();

        /// <summary>
        /// Get current lap
        /// </summary>
        public LapData? GetCurrentLap() => _currentLap;

        /// <summary>
        /// Get best lap
        /// </summary>
        public LapData? GetBestLap() => _sessionLaps
            .Where(l => l.IsValid)
            .OrderBy(l => l.LapTime)
            .FirstOrDefault();

        /// <summary>
        /// Calculate predicted lap time based on current pace
        /// </summary>
        public float? PredictLapTime()
        {
            if (_currentLap == null || !_lastSectorTime.HasValue)
                return null;

            float timeIntoLap = (float)(DateTime.Now - _lastSectorTime.Value).TotalSeconds;

            // Use reference lap or best lap for prediction
            var predictionLap = _referenceLap ?? GetBestLap();
            if (predictionLap == null)
                return null;

            // Simple linear prediction based on progress
            float lapProgress = timeIntoLap / predictionLap.LapTime;
            if (lapProgress > 1f)
                return null;

            // Calculate current delta and project
            float currentDelta = timeIntoLap - (predictionLap.LapTime * lapProgress);
            return predictionLap.LapTime + currentDelta;
        }

        /// <summary>
        /// Calculate time gain/loss vs reference at current position
        /// </summary>
        public float? CalculateTimeGainLoss()
        {
            if (_currentLap == null || _referenceLap == null || !_lastSectorTime.HasValue)
                return null;

            float timeIntoLap = (float)(DateTime.Now - _lastSectorTime.Value).TotalSeconds;
            float referenceTimeAtPosition = _referenceLap.LapTime * (timeIntoLap / _referenceLap.LapTime);

            return timeIntoLap - referenceTimeAtPosition;
        }
    }
}
