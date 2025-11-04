using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Braking event analysis
    /// </summary>
    public class BrakingEvent
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public float Duration { get; set; }
        public float EntrySpeed { get; set; }
        public float ExitSpeed { get; set; }
        public float PeakBrakePressure { get; set; }
        public float BrakeApplicationRate { get; set; }
        public float BrakeReleaseRate { get; set; }
        public float Distance { get; set; }
        public float MaxDeceleration { get; set; }
        public float Score { get; set; }
        public string? Feedback { get; set; }
    }

    /// <summary>
    /// Corner performance analysis
    /// </summary>
    public class CornerAnalysis
    {
        public int CornerNumber { get; set; }
        public string? CornerName { get; set; }

        // Entry phase
        public float EntrySpeed { get; set; }
        public float BrakingPoint { get; set; }
        public float EntryScore { get; set; }
        public string? EntryFeedback { get; set; }

        // Apex phase
        public float ApexSpeed { get; set; }
        public float MinimumSpeed { get; set; }
        public float ApexLineDeviation { get; set; }
        public float ApexScore { get; set; }
        public string? ApexFeedback { get; set; }

        // Exit phase
        public float ExitSpeed { get; set; }
        public float ThrottleApplicationPoint { get; set; }
        public float ExitScore { get; set; }
        public string? ExitFeedback { get; set; }

        // Overall
        public float OverallScore { get; set; }
        public float TimeLost { get; set; }  // vs reference
    }

    /// <summary>
    /// Driver coaching and performance analysis service
    /// </summary>
    public class DriverCoachingService
    {
        private List<BrakingEvent> _brakingEvents = new();
        private List<CornerAnalysis> _cornerAnalyses = new();

        // Reference data for comparison
        private List<ComprehensiveTelemetryData>? _referenceLapData;
        private LapData? _referenceLap;

        // Consistency tracking
        private Queue<float> _recentLapTimes = new();
        private const int LAP_BUFFER_SIZE = 10;

        /// <summary>
        /// Set reference lap for comparison
        /// </summary>
        public void SetReferenceLap(LapData lap, List<ComprehensiveTelemetryData> data)
        {
            _referenceLap = lap;
            _referenceLapData = data;
        }

        /// <summary>
        /// Analyze a complete lap
        /// </summary>
        public LapAnalysisReport AnalyzeLap(
            LapData lap,
            List<ComprehensiveTelemetryData> lapData,
            List<DerivedChannels> derivedData)
        {
            var report = new LapAnalysisReport
            {
                LapNumber = lap.LapNumber,
                LapTime = lap.LapTime,
                Timestamp = lap.StartTime
            };

            // Analyze braking events
            report.BrakingEvents = AnalyzeBrakingEvents(lapData, derivedData);

            // Analyze corners
            report.CornerAnalyses = AnalyzeCorners(lapData, derivedData);

            // Calculate consistency score
            report.ConsistencyScore = CalculateConsistencyScore(lap.LapTime);

            // Analyze throttle usage
            report.ThrottleAnalysis = AnalyzeThrottleUsage(lapData);

            // Analyze steering smoothness
            report.SteeringSmoothness = AnalyzeSteeringSmoothness(lapData);

            // Overall driver score
            report.OverallScore = CalculateOverallScore(report);

            // Generate recommendations
            report.Recommendations = GenerateRecommendations(report);

            return report;
        }

        /// <summary>
        /// Analyze braking events in a lap
        /// </summary>
        private List<BrakingEvent> AnalyzeBrakingEvents(
            List<ComprehensiveTelemetryData> lapData,
            List<DerivedChannels> derivedData)
        {
            var events = new List<BrakingEvent>();

            BrakingEvent? currentEvent = null;
            float entrySpeed = 0;

            for (int i = 0; i < lapData.Count; i++)
            {
                var data = lapData[i];
                var derived = derivedData.ElementAtOrDefault(i);

                bool isBraking = data.BrakePosition.HasValue && data.BrakePosition.Value > 10f;

                if (isBraking && currentEvent == null)
                {
                    // Braking event start
                    currentEvent = new BrakingEvent
                    {
                        StartTime = data.Timestamp,
                        EntrySpeed = data.GpsSpeed ?? 0,
                        Distance = data.TrackDistance ?? 0
                    };
                    entrySpeed = currentEvent.EntrySpeed;
                }
                else if (currentEvent != null)
                {
                    // Update peak values
                    if (data.BrakePosition.HasValue && data.BrakePosition.Value > currentEvent.PeakBrakePressure)
                    {
                        currentEvent.PeakBrakePressure = data.BrakePosition.Value;
                    }

                    if (derived?.BrakingGForce.HasValue == true &&
                        derived.BrakingGForce.Value > currentEvent.MaxDeceleration)
                    {
                        currentEvent.MaxDeceleration = derived.BrakingGForce.Value;
                    }

                    if (derived?.BrakeApplicationRate.HasValue == true &&
                        derived.BrakeApplicationRate.Value > currentEvent.BrakeApplicationRate)
                    {
                        currentEvent.BrakeApplicationRate = derived.BrakeApplicationRate.Value;
                    }

                    if (!isBraking)
                    {
                        // Braking event end
                        currentEvent.EndTime = data.Timestamp;
                        currentEvent.ExitSpeed = data.GpsSpeed ?? 0;
                        currentEvent.Duration = (float)(currentEvent.EndTime - currentEvent.StartTime).TotalSeconds;

                        // Get release rate from previous point
                        if (i > 0 && derivedData.Count > i - 1)
                        {
                            var prevDerived = derivedData[i - 1];
                            if (prevDerived.BrakeReleaseRate.HasValue)
                            {
                                currentEvent.BrakeReleaseRate = prevDerived.BrakeReleaseRate.Value;
                            }
                        }

                        // Score the braking event
                        ScoreBrakingEvent(currentEvent);

                        events.Add(currentEvent);
                        currentEvent = null;
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Score a braking event (0-100)
        /// </summary>
        private void ScoreBrakingEvent(BrakingEvent evt)
        {
            float score = 100f;
            var feedback = new List<string>();

            // Brake application rate (should be high initially)
            if (evt.BrakeApplicationRate < 100f)
            {
                score -= 10f;
                feedback.Add("Apply brakes more quickly initially");
            }

            // Peak brake pressure (should be high)
            if (evt.PeakBrakePressure < 90f)
            {
                score -= 10f;
                feedback.Add("Increase peak brake pressure");
            }

            // Max deceleration
            if (evt.MaxDeceleration < 1.2f)  // Less than 1.2G
            {
                score -= 10f;
                feedback.Add("Not enough braking force");
            }

            // Brake release (should be smooth)
            if (evt.BrakeReleaseRate > 200f)
            {
                score -= 5f;
                feedback.Add("Release brake more smoothly");
            }

            evt.Score = Math.Max(0, score);
            evt.Feedback = feedback.Any() ? string.Join("; ", feedback) : "Good braking technique";
        }

        /// <summary>
        /// Analyze corners in a lap
        /// </summary>
        private List<CornerAnalysis> AnalyzeCorners(
            List<ComprehensiveTelemetryData> lapData,
            List<DerivedChannels> derivedData)
        {
            var corners = new List<CornerAnalysis>();

            // Detect corners based on lateral G
            var cornerPhases = DetectCornerPhases(derivedData);

            foreach (var (cornerNum, entryIdx, apexIdx, exitIdx) in cornerPhases)
            {
                var analysis = new CornerAnalysis
                {
                    CornerNumber = cornerNum
                };

                // Entry analysis
                if (entryIdx < lapData.Count)
                {
                    var entryData = lapData[entryIdx];
                    analysis.EntrySpeed = entryData.GpsSpeed ?? 0;
                    analysis.BrakingPoint = entryData.TrackDistance ?? 0;

                    // Score entry
                    analysis.EntryScore = ScoreCornerEntry(entryData, derivedData.ElementAtOrDefault(entryIdx));
                }

                // Apex analysis
                if (apexIdx < lapData.Count)
                {
                    var apexData = lapData[apexIdx];
                    analysis.ApexSpeed = apexData.GpsSpeed ?? 0;
                    analysis.MinimumSpeed = analysis.ApexSpeed;  // Simplified

                    // Score apex
                    analysis.ApexScore = ScoreCornerApex(apexData, derivedData.ElementAtOrDefault(apexIdx));
                }

                // Exit analysis
                if (exitIdx < lapData.Count)
                {
                    var exitData = lapData[exitIdx];
                    analysis.ExitSpeed = exitData.GpsSpeed ?? 0;

                    // Score exit
                    analysis.ExitScore = ScoreCornerExit(exitData, derivedData.ElementAtOrDefault(exitIdx));
                }

                // Overall score
                analysis.OverallScore = (analysis.EntryScore + analysis.ApexScore + analysis.ExitScore) / 3f;

                corners.Add(analysis);
            }

            return corners;
        }

        /// <summary>
        /// Detect corner phases (entry, apex, exit indices)
        /// </summary>
        private List<(int CornerNum, int EntryIdx, int ApexIdx, int ExitIdx)> DetectCornerPhases(
            List<DerivedChannels> derivedData)
        {
            var phases = new List<(int, int, int, int)>();
            const float CORNER_G_THRESHOLD = 0.5f;

            int cornerNum = 0;
            int? entryIdx = null;
            int? apexIdx = null;
            float maxG = 0;

            for (int i = 0; i < derivedData.Count; i++)
            {
                var derived = derivedData[i];
                float absLatG = Math.Abs(derived.LateralG ?? 0);

                if (absLatG > CORNER_G_THRESHOLD)
                {
                    if (!entryIdx.HasValue)
                    {
                        // Corner entry
                        entryIdx = i;
                        maxG = absLatG;
                        apexIdx = i;
                    }
                    else
                    {
                        // Track apex (highest G)
                        if (absLatG > maxG)
                        {
                            maxG = absLatG;
                            apexIdx = i;
                        }
                    }
                }
                else if (entryIdx.HasValue)
                {
                    // Corner exit
                    cornerNum++;
                    phases.Add((cornerNum, entryIdx.Value, apexIdx ?? entryIdx.Value, i));
                    entryIdx = null;
                    apexIdx = null;
                    maxG = 0;
                }
            }

            return phases;
        }

        /// <summary>
        /// Score corner entry (0-100)
        /// </summary>
        private float ScoreCornerEntry(ComprehensiveTelemetryData data, DerivedChannels? derived)
        {
            float score = 100f;

            // Should be braking at entry
            if (data.BrakePosition.HasValue && data.BrakePosition.Value < 50f)
            {
                score -= 20f;
            }

            // Should have some trail braking
            if (derived?.BrakingGForce.HasValue == true && derived.BrakingGForce.Value < 0.8f)
            {
                score -= 10f;
            }

            return Math.Max(0, score);
        }

        /// <summary>
        /// Score corner apex (0-100)
        /// </summary>
        private float ScoreCornerApex(ComprehensiveTelemetryData data, DerivedChannels? derived)
        {
            float score = 100f;

            // Should have good lateral G at apex
            if (derived?.LateralG.HasValue == true && Math.Abs(derived.LateralG.Value) < 1.0f)
            {
                score -= 15f;
            }

            // Should be minimum speed point
            // (Would need to check against surrounding points)

            return Math.Max(0, score);
        }

        /// <summary>
        /// Score corner exit (0-100)
        /// </summary>
        private float ScoreCornerExit(ComprehensiveTelemetryData data, DerivedChannels? derived)
        {
            float score = 100f;

            // Should be on throttle at exit
            if (data.ThrottlePosition.HasValue && data.ThrottlePosition.Value < 70f)
            {
                score -= 20f;
            }

            // Speed should be increasing
            // (Would need to check derivative)

            return Math.Max(0, score);
        }

        /// <summary>
        /// Calculate consistency score based on recent lap times
        /// </summary>
        private float CalculateConsistencyScore(float lapTime)
        {
            _recentLapTimes.Enqueue(lapTime);
            if (_recentLapTimes.Count > LAP_BUFFER_SIZE)
                _recentLapTimes.Dequeue();

            if (_recentLapTimes.Count < 3)
                return 50f;  // Not enough data

            float mean = _recentLapTimes.Average();
            float variance = _recentLapTimes.Sum(t => (float)Math.Pow(t - mean, 2)) / _recentLapTimes.Count;
            float stdDev = (float)Math.Sqrt(variance);

            // Convert to score (lower stddev = higher score)
            // Assume 1 second stddev = 50 score
            float score = Math.Max(0, 100f - (stdDev * 50f));
            return Math.Min(100, score);
        }

        /// <summary>
        /// Analyze throttle usage
        /// </summary>
        private ThrottleAnalysis AnalyzeThrottleUsage(List<ComprehensiveTelemetryData> lapData)
        {
            var analysis = new ThrottleAnalysis();

            int fullThrottleCount = 0;
            int partialThrottleCount = 0;
            int coastingCount = 0;

            foreach (var data in lapData)
            {
                if (!data.ThrottlePosition.HasValue)
                    continue;

                if (data.ThrottlePosition.Value > 95f)
                    fullThrottleCount++;
                else if (data.ThrottlePosition.Value > 10f)
                    partialThrottleCount++;
                else
                    coastingCount++;
            }

            int total = fullThrottleCount + partialThrottleCount + coastingCount;
            if (total > 0)
            {
                analysis.FullThrottlePercentage = (fullThrottleCount / (float)total) * 100f;
                analysis.PartialThrottlePercentage = (partialThrottleCount / (float)total) * 100f;
                analysis.CoastingPercentage = (coastingCount / (float)total) * 100f;
            }

            // Score throttle usage
            analysis.Score = analysis.FullThrottlePercentage;  // Higher is better

            if (analysis.CoastingPercentage > 20f)
            {
                analysis.Feedback = "Reduce coasting time - be more decisive with throttle";
            }
            else if (analysis.PartialThrottlePercentage > 40f)
            {
                analysis.Feedback = "Reduce partial throttle time - commit to full throttle earlier";
            }
            else
            {
                analysis.Feedback = "Good throttle usage";
            }

            return analysis;
        }

        /// <summary>
        /// Analyze steering smoothness
        /// </summary>
        private SteeringAnalysis AnalyzeSteeringSmoothness(List<ComprehensiveTelemetryData> lapData)
        {
            var analysis = new SteeringAnalysis();

            float totalSteeringRate = 0;
            int count = 0;

            for (int i = 1; i < lapData.Count; i++)
            {
                var prev = lapData[i - 1];
                var curr = lapData[i];

                if (prev.SteeringAngle.HasValue && curr.SteeringAngle.HasValue)
                {
                    float dt = (float)(curr.Timestamp - prev.Timestamp).TotalSeconds;
                    if (dt > 0)
                    {
                        float rate = Math.Abs(curr.SteeringAngle.Value - prev.SteeringAngle.Value) / dt;
                        totalSteeringRate += rate;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                analysis.AverageSteeringRate = totalSteeringRate / count;

                // Score smoothness (lower rate = smoother = higher score)
                // Assume 50 deg/s average = 50 score
                analysis.Score = Math.Max(0, 100f - analysis.AverageSteeringRate);
            }

            if (analysis.AverageSteeringRate > 100f)
            {
                analysis.Feedback = "Steering inputs are too aggressive - be smoother";
            }
            else
            {
                analysis.Feedback = "Good steering smoothness";
            }

            return analysis;
        }

        /// <summary>
        /// Calculate overall driver performance score
        /// </summary>
        private float CalculateOverallScore(LapAnalysisReport report)
        {
            var scores = new List<float>
            {
                report.ConsistencyScore,
                report.ThrottleAnalysis.Score,
                report.SteeringSmoothness.Score
            };

            if (report.BrakingEvents.Any())
            {
                scores.Add(report.BrakingEvents.Average(b => b.Score));
            }

            if (report.CornerAnalyses.Any())
            {
                scores.Add(report.CornerAnalyses.Average(c => c.OverallScore));
            }

            return scores.Average();
        }

        /// <summary>
        /// Generate recommendations for improvement
        /// </summary>
        private List<string> GenerateRecommendations(LapAnalysisReport report)
        {
            var recommendations = new List<string>();

            // Consistency
            if (report.ConsistencyScore < 70f)
            {
                recommendations.Add("Focus on consistency - your lap times are varying too much");
            }

            // Braking
            var poorBrakingEvents = report.BrakingEvents.Where(b => b.Score < 70f).ToList();
            if (poorBrakingEvents.Any())
            {
                recommendations.Add($"Improve braking technique at {poorBrakingEvents.Count} braking zone(s)");
            }

            // Corners
            var poorCorners = report.CornerAnalyses.Where(c => c.OverallScore < 70f).ToList();
            if (poorCorners.Any())
            {
                recommendations.Add($"Work on corner technique at corner(s): {string.Join(", ", poorCorners.Select(c => c.CornerNumber))}");
            }

            // Throttle
            if (report.ThrottleAnalysis.Score < 60f)
            {
                recommendations.Add("Increase full throttle usage - be more aggressive on throttle application");
            }

            // Steering
            if (report.SteeringSmoothness.Score < 70f)
            {
                recommendations.Add("Make steering inputs smoother and more progressive");
            }

            if (!recommendations.Any())
            {
                recommendations.Add("Great driving! Keep it up!");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Lap analysis report
    /// </summary>
    public class LapAnalysisReport
    {
        public int LapNumber { get; set; }
        public float LapTime { get; set; }
        public DateTime Timestamp { get; set; }

        public List<BrakingEvent> BrakingEvents { get; set; } = new();
        public List<CornerAnalysis> CornerAnalyses { get; set; } = new();
        public float ConsistencyScore { get; set; }
        public ThrottleAnalysis ThrottleAnalysis { get; set; } = new();
        public SteeringAnalysis SteeringSmoothness { get; set; } = new();
        public float OverallScore { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Throttle usage analysis
    /// </summary>
    public class ThrottleAnalysis
    {
        public float FullThrottlePercentage { get; set; }
        public float PartialThrottlePercentage { get; set; }
        public float CoastingPercentage { get; set; }
        public float Score { get; set; }
        public string? Feedback { get; set; }
    }

    /// <summary>
    /// Steering analysis
    /// </summary>
    public class SteeringAnalysis
    {
        public float AverageSteeringRate { get; set; }
        public float Score { get; set; }
        public string? Feedback { get; set; }
    }
}
