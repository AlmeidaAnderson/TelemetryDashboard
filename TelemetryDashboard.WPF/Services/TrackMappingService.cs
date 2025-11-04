using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// GPS coordinate point for track mapping
    /// </summary>
    public class TrackPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Distance { get; set; }  // Distance from start
        public float Speed { get; set; }
        public float Throttle { get; set; }
        public float Brake { get; set; }
        public float LateralG { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Service for GPS processing and track mapping
    /// </summary>
    public class TrackMappingService
    {
        private List<TrackPoint> _trackMap = new();
        private List<TrackPoint> _currentLapPoints = new();

        private const double EARTH_RADIUS = 6371000;  // meters

        // Track boundaries
        private double? _minLat, _maxLat, _minLon, _maxLon;
        private float _trackLength = 0;

        /// <summary>
        /// Add a GPS point to the track map
        /// </summary>
        public void AddPoint(ComprehensiveTelemetryData data)
        {
            if (!data.Latitude.HasValue || !data.Longitude.HasValue)
                return;

            var point = new TrackPoint
            {
                Latitude = data.Latitude.Value,
                Longitude = data.Longitude.Value,
                Speed = data.GpsSpeed ?? 0,
                Throttle = data.ThrottlePosition ?? 0,
                Brake = data.BrakePosition ?? 0,
                LateralG = data.AccelY.HasValue ? Math.Abs(data.AccelY.Value / 9.81f) : 0,
                Timestamp = data.Timestamp
            };

            // Calculate distance from previous point
            if (_currentLapPoints.Any())
            {
                var lastPoint = _currentLapPoints.Last();
                float distance = CalculateDistance(
                    lastPoint.Latitude, lastPoint.Longitude,
                    point.Latitude, point.Longitude
                );
                point.Distance = lastPoint.Distance + distance;
            }
            else
            {
                point.Distance = 0;
            }

            _currentLapPoints.Add(point);

            // Update track boundaries
            UpdateBoundaries(point);
        }

        /// <summary>
        /// Complete current lap and add to track map
        /// </summary>
        public void CompleteLap()
        {
            if (!_currentLapPoints.Any())
                return;

            // Add points to main track map (or update if better data)
            if (!_trackMap.Any())
            {
                _trackMap = new List<TrackPoint>(_currentLapPoints);
                _trackLength = _currentLapPoints.Last().Distance;
            }
            else
            {
                // Merge with existing track map (keep better data)
                MergeTrackData();
            }

            _currentLapPoints.Clear();
        }

        /// <summary>
        /// Merge current lap data with existing track map
        /// </summary>
        private void MergeTrackData()
        {
            // For now, just keep the track map with more points
            // In a real implementation, this would intelligently merge GPS tracks
            if (_currentLapPoints.Count > _trackMap.Count)
            {
                _trackMap = new List<TrackPoint>(_currentLapPoints);
                _trackLength = _trackMap.Last().Distance;
            }
        }

        /// <summary>
        /// Update track boundaries
        /// </summary>
        private void UpdateBoundaries(TrackPoint point)
        {
            if (!_minLat.HasValue || point.Latitude < _minLat.Value)
                _minLat = point.Latitude;
            if (!_maxLat.HasValue || point.Latitude > _maxLat.Value)
                _maxLat = point.Latitude;
            if (!_minLon.HasValue || point.Longitude < _minLon.Value)
                _minLon = point.Longitude;
            if (!_maxLon.HasValue || point.Longitude > _maxLon.Value)
                _maxLon = point.Longitude;
        }

        /// <summary>
        /// Get track map points
        /// </summary>
        public List<TrackPoint> GetTrackMap() => _trackMap.ToList();

        /// <summary>
        /// Get current lap points
        /// </summary>
        public List<TrackPoint> GetCurrentLapPoints() => _currentLapPoints.ToList();

        /// <summary>
        /// Get track length in meters
        /// </summary>
        public float GetTrackLength() => _trackLength;

        /// <summary>
        /// Get track boundaries
        /// </summary>
        public (double MinLat, double MaxLat, double MinLon, double MaxLon) GetBoundaries()
        {
            return (
                _minLat ?? 0,
                _maxLat ?? 0,
                _minLon ?? 0,
                _maxLon ?? 0
            );
        }

        /// <summary>
        /// Find closest track point to given GPS coordinates
        /// </summary>
        public TrackPoint? FindClosestPoint(double latitude, double longitude)
        {
            if (!_trackMap.Any())
                return null;

            return _trackMap
                .OrderBy(p => CalculateDistance(p.Latitude, p.Longitude, latitude, longitude))
                .First();
        }

        /// <summary>
        /// Get track distance at GPS coordinates
        /// </summary>
        public float? GetDistanceAtCoordinates(double latitude, double longitude)
        {
            var closest = FindClosestPoint(latitude, longitude);
            return closest?.Distance;
        }

        /// <summary>
        /// Detect corners in the track
        /// </summary>
        public List<(int CornerNumber, float StartDistance, float EndDistance, string Direction)> DetectCorners()
        {
            var corners = new List<(int, float, float, string)>();

            if (_trackMap.Count < 10)
                return corners;

            const float CORNER_G_THRESHOLD = 0.5f;  // Minimum lateral G to be considered a corner
            const float CORNER_MIN_DURATION = 1.0f;  // Minimum seconds for a corner

            bool inCorner = false;
            float cornerStartDistance = 0;
            DateTime? cornerStartTime = null;
            float avgLateralG = 0;
            int cornerNumber = 0;

            for (int i = 0; i < _trackMap.Count; i++)
            {
                var point = _trackMap[i];

                if (!inCorner && point.LateralG > CORNER_G_THRESHOLD)
                {
                    // Corner entry
                    inCorner = true;
                    cornerStartDistance = point.Distance;
                    cornerStartTime = point.Timestamp;
                    avgLateralG = point.LateralG;
                }
                else if (inCorner)
                {
                    if (point.LateralG > CORNER_G_THRESHOLD)
                    {
                        // Still in corner
                        avgLateralG = (avgLateralG + point.LateralG) / 2f;
                    }
                    else
                    {
                        // Corner exit
                        if (cornerStartTime.HasValue)
                        {
                            float duration = (float)(point.Timestamp - cornerStartTime.Value).TotalSeconds;
                            if (duration >= CORNER_MIN_DURATION)
                            {
                                cornerNumber++;
                                string direction = avgLateralG > 0 ? "Right" : "Left";
                                corners.Add((cornerNumber, cornerStartDistance, point.Distance, direction));
                            }
                        }
                        inCorner = false;
                    }
                }
            }

            return corners;
        }

        /// <summary>
        /// Calculate racing line quality score (0-100)
        /// </summary>
        public float CalculateRacingLineScore(List<TrackPoint> lapPoints)
        {
            if (!lapPoints.Any() || !_trackMap.Any())
                return 0;

            // Compare lap points to ideal track map
            float totalDeviation = 0;
            int comparedPoints = 0;

            foreach (var point in lapPoints)
            {
                var idealPoint = FindClosestPoint(point.Latitude, point.Longitude);
                if (idealPoint != null)
                {
                    float deviation = CalculateDistance(
                        point.Latitude, point.Longitude,
                        idealPoint.Latitude, idealPoint.Longitude
                    );
                    totalDeviation += deviation;
                    comparedPoints++;
                }
            }

            if (comparedPoints == 0)
                return 0;

            float avgDeviation = totalDeviation / comparedPoints;

            // Convert to score (0-100, where 0m deviation = 100 score)
            // Assume 5m average deviation = 50 score
            float score = Math.Max(0, 100f - (avgDeviation * 10f));
            return Math.Min(100, score);
        }

        /// <summary>
        /// Get speed heatmap data for track
        /// </summary>
        public List<(double Lat, double Lon, float Speed, string Color)> GetSpeedHeatmap()
        {
            if (!_trackMap.Any())
                return new List<(double, double, float, string)>();

            float maxSpeed = _trackMap.Max(p => p.Speed);
            float minSpeed = _trackMap.Min(p => p.Speed);

            return _trackMap.Select(p =>
            {
                // Normalize speed to 0-1
                float normalized = maxSpeed > minSpeed ? (p.Speed - minSpeed) / (maxSpeed - minSpeed) : 0;

                // Convert to color (green = fast, red = slow)
                string color = GetSpeedColor(normalized);

                return (p.Latitude, p.Longitude, p.Speed, color);
            }).ToList();
        }

        /// <summary>
        /// Get brake point heatmap
        /// </summary>
        public List<(double Lat, double Lon, float BrakeForce)> GetBrakeHeatmap()
        {
            return _trackMap
                .Where(p => p.Brake > 10)
                .Select(p => (p.Latitude, p.Longitude, p.Brake))
                .ToList();
        }

        /// <summary>
        /// Get throttle application heatmap
        /// </summary>
        public List<(double Lat, double Lon, float ThrottlePosition)> GetThrottleHeatmap()
        {
            return _trackMap
                .Where(p => p.Throttle > 10)
                .Select(p => (p.Latitude, p.Longitude, p.Throttle))
                .ToList();
        }

        /// <summary>
        /// Convert normalized speed (0-1) to color hex
        /// </summary>
        private string GetSpeedColor(float normalized)
        {
            // Green (fast) -> Yellow -> Red (slow)
            if (normalized > 0.66f)
                return "#00FF00";  // Green
            else if (normalized > 0.33f)
                return "#FFFF00";  // Yellow
            else
                return "#FF0000";  // Red
        }

        /// <summary>
        /// Calculate distance between two GPS coordinates (Haversine formula)
        /// </summary>
        private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(EARTH_RADIUS * c);
        }

        /// <summary>
        /// Clear all track data
        /// </summary>
        public void ClearTrackMap()
        {
            _trackMap.Clear();
            _currentLapPoints.Clear();
            _minLat = _maxLat = _minLon = _maxLon = null;
            _trackLength = 0;
        }

        /// <summary>
        /// Export track map as GPX file format
        /// </summary>
        public string ExportToGPX(string trackName)
        {
            var gpx = new System.Text.StringBuilder();
            gpx.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            gpx.AppendLine("<gpx version=\"1.1\" creator=\"TelemetryDashboard\">");
            gpx.AppendLine($"  <metadata><name>{trackName}</name></metadata>");
            gpx.AppendLine("  <trk>");
            gpx.AppendLine($"    <name>{trackName}</name>");
            gpx.AppendLine("    <trkseg>");

            foreach (var point in _trackMap)
            {
                gpx.AppendLine($"      <trkpt lat=\"{point.Latitude}\" lon=\"{point.Longitude}\">");
                gpx.AppendLine($"        <time>{point.Timestamp:yyyy-MM-ddTHH:mm:ssZ}</time>");
                gpx.AppendLine($"        <extensions>");
                gpx.AppendLine($"          <speed>{point.Speed}</speed>");
                gpx.AppendLine($"        </extensions>");
                gpx.AppendLine($"      </trkpt>");
            }

            gpx.AppendLine("    </trkseg>");
            gpx.AppendLine("  </trk>");
            gpx.AppendLine("</gpx>");

            return gpx.ToString();
        }
    }
}
