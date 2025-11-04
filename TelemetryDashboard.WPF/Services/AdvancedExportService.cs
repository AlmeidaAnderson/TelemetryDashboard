using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Advanced export service for PDF reports, MoTeC CSV, AiM CSV formats
    /// </summary>
    public class AdvancedExportService
    {
        /// <summary>
        /// Export session to MoTeC i2 compatible CSV format
        /// </summary>
        public async Task ExportToMoTeCCsvAsync(
            string filePath,
            SessionData session,
            List<ComprehensiveTelemetryData> telemetryData,
            List<DerivedChannels> derivedData)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // MoTeC CSV Header
            await writer.WriteLineAsync("Format: MoTeC CSV Telemetry Export v1.0");
            await writer.WriteLineAsync($"Venue: {session.TrackName ?? "Unknown"}");
            await writer.WriteLineAsync($"Vehicle: {session.VehicleName ?? "Unknown"}");
            await writer.WriteLineAsync($"Driver: {session.DriverName ?? "Unknown"}");
            await writer.WriteLineAsync($"Session: {session.SessionType}");
            await writer.WriteLineAsync($"Date: {session.StartTime:yyyy-MM-dd}");
            await writer.WriteLineAsync($"Time: {session.StartTime:HH:mm:ss}");
            await writer.WriteLineAsync();

            // Column headers
            var headers = new List<string>
            {
                "Time (s)",
                "Distance (m)",
                "Speed (km/h)",
                "Engine RPM",
                "Throttle (%)",
                "Brake (%)",
                "Steering (deg)",
                "Gear",
                "Latitude (deg)",
                "Longitude (deg)",
                "AccelX (g)",
                "AccelY (g)",
                "AccelZ (g)",
                "GyroX (deg/s)",
                "GyroY (deg/s)",
                "GyroZ (deg/s)",
                "Wheel Speed FL (km/h)",
                "Wheel Speed FR (km/h)",
                "Wheel Speed RL (km/h)",
                "Wheel Speed RR (km/h)",
                "Oil Temp (C)",
                "Oil Pressure (kPa)",
                "Coolant Temp (C)",
                "Fuel Pressure (kPa)",
                "Manifold Pressure (kPa)",
                "Lambda",
                "Battery Voltage (V)"
            };

            await writer.WriteLineAsync(string.Join(",", headers));

            // Data rows
            DateTime startTime = telemetryData.FirstOrDefault()?.Timestamp ?? DateTime.Now;

            for (int i = 0; i < telemetryData.Count; i++)
            {
                var data = telemetryData[i];
                float timeSeconds = (float)(data.Timestamp - startTime).TotalSeconds;

                var row = new List<string>
                {
                    timeSeconds.ToString("F3"),
                    (data.TrackDistance ?? 0).ToString("F2"),
                    (data.GpsSpeed ?? 0).ToString("F2"),
                    (data.EngineRPM ?? 0).ToString(),
                    (data.ThrottlePosition ?? 0).ToString("F1"),
                    (data.BrakePosition ?? 0).ToString("F1"),
                    (data.SteeringAngle ?? 0).ToString("F2"),
                    (data.GearPosition ?? 0).ToString(),
                    (data.Latitude ?? 0).ToString("F6"),
                    (data.Longitude ?? 0).ToString("F6"),
                    (data.AccelX.HasValue ? (data.AccelX.Value / 9.81f).ToString("F3") : "0"),
                    (data.AccelY.HasValue ? (data.AccelY.Value / 9.81f).ToString("F3") : "0"),
                    (data.AccelZ.HasValue ? (data.AccelZ.Value / 9.81f).ToString("F3") : "0"),
                    (data.GyroX ?? 0).ToString("F2"),
                    (data.GyroY ?? 0).ToString("F2"),
                    (data.GyroZ ?? 0).ToString("F2"),
                    (data.WheelSpeedFL ?? 0).ToString("F2"),
                    (data.WheelSpeedFR ?? 0).ToString("F2"),
                    (data.WheelSpeedRL ?? 0).ToString("F2"),
                    (data.WheelSpeedRR ?? 0).ToString("F2"),
                    (data.OilTemperature ?? 0).ToString("F1"),
                    (data.OilPressure ?? 0).ToString("F1"),
                    (data.CoolantTemperature ?? 0).ToString("F1"),
                    (data.FuelPressure ?? 0).ToString("F1"),
                    (data.ManifoldPressure ?? 0).ToString("F1"),
                    (data.Lambda ?? 0).ToString("F2"),
                    (data.BatteryVoltage ?? 0).ToString("F2")
                };

                await writer.WriteLineAsync(string.Join(",", row));
            }
        }

        /// <summary>
        /// Export session to AiM compatible CSV format
        /// </summary>
        public async Task ExportToAiMCsvAsync(
            string filePath,
            SessionData session,
            List<ComprehensiveTelemetryData> telemetryData,
            List<DerivedChannels> derivedData)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // AiM CSV Header
            await writer.WriteLineAsync($"Track;{session.TrackName ?? "Unknown"}");
            await writer.WriteLineAsync($"Vehicle;{session.VehicleName ?? "Unknown"}");
            await writer.WriteLineAsync($"Driver;{session.DriverName ?? "Unknown"}");
            await writer.WriteLineAsync($"Championship;{session.SessionType}");
            await writer.WriteLineAsync($"Date;{session.StartTime:dd/MM/yyyy}");
            await writer.WriteLineAsync($"Time;{session.StartTime:HH:mm:ss}");
            await writer.WriteLineAsync();

            // Column headers - AiM format
            var headers = new List<string>
            {
                "Time",
                "GPS_Speed",
                "RPM",
                "TPS",
                "Brake",
                "Steer",
                "Gear",
                "GPS_Latitude",
                "GPS_Longitude",
                "GPS_Altitude",
                "AccelX",
                "AccelY",
                "AccelZ",
                "GyroX",
                "GyroY",
                "GyroZ",
                "Susp_Travel_FL",
                "Susp_Travel_FR",
                "Susp_Travel_RL",
                "Susp_Travel_RR",
                "Oil_Temp",
                "Oil_Press",
                "Water_Temp",
                "Fuel_Press",
                "Lambda",
                "Battery"
            };

            await writer.WriteLineAsync(string.Join(";", headers));

            // Units row - AiM format
            var units = new List<string>
            {
                "s",
                "Km/h",
                "rpm",
                "%",
                "%",
                "deg",
                "",
                "deg",
                "deg",
                "m",
                "g",
                "g",
                "g",
                "deg/s",
                "deg/s",
                "deg/s",
                "mm",
                "mm",
                "mm",
                "mm",
                "°C",
                "bar",
                "°C",
                "bar",
                "",
                "V"
            };

            await writer.WriteLineAsync(string.Join(";", units));

            // Data rows
            DateTime startTime = telemetryData.FirstOrDefault()?.Timestamp ?? DateTime.Now;

            for (int i = 0; i < telemetryData.Count; i++)
            {
                var data = telemetryData[i];
                float timeSeconds = (float)(data.Timestamp - startTime).TotalSeconds;

                var row = new List<string>
                {
                    timeSeconds.ToString("F3"),
                    (data.GpsSpeed ?? 0).ToString("F2"),
                    (data.EngineRPM ?? 0).ToString(),
                    (data.ThrottlePosition ?? 0).ToString("F1"),
                    (data.BrakePosition ?? 0).ToString("F1"),
                    (data.SteeringAngle ?? 0).ToString("F2"),
                    (data.GearPosition ?? 0).ToString(),
                    (data.Latitude ?? 0).ToString("F6"),
                    (data.Longitude ?? 0).ToString("F6"),
                    (data.GpsAltitude ?? 0).ToString("F1"),
                    (data.AccelX.HasValue ? (data.AccelX.Value / 9.81f).ToString("F3") : "0"),
                    (data.AccelY.HasValue ? (data.AccelY.Value / 9.81f).ToString("F3") : "0"),
                    (data.AccelZ.HasValue ? (data.AccelZ.Value / 9.81f).ToString("F3") : "0"),
                    (data.GyroX ?? 0).ToString("F2"),
                    (data.GyroY ?? 0).ToString("F2"),
                    (data.GyroZ ?? 0).ToString("F2"),
                    (data.SuspensionTravelFL ?? 0).ToString("F2"),
                    (data.SuspensionTravelFR ?? 0).ToString("F2"),
                    (data.SuspensionTravelRL ?? 0).ToString("F2"),
                    (data.SuspensionTravelRR ?? 0).ToString("F2"),
                    (data.OilTemperature ?? 0).ToString("F1"),
                    (data.OilPressure.HasValue ? (data.OilPressure.Value / 100f).ToString("F2") : "0"),  // Convert kPa to bar
                    (data.CoolantTemperature ?? 0).ToString("F1"),
                    (data.FuelPressure.HasValue ? (data.FuelPressure.Value / 100f).ToString("F2") : "0"),  // Convert kPa to bar
                    (data.Lambda ?? 0).ToString("F2"),
                    (data.BatteryVoltage ?? 0).ToString("F2")
                };

                await writer.WriteLineAsync(string.Join(";", row));
            }
        }

        /// <summary>
        /// Generate comprehensive PDF session report
        /// </summary>
        public async Task<string> GenerateSessionPdfReport(
            string outputPath,
            SessionData session,
            List<LapData> laps,
            List<ComprehensiveTelemetryData> telemetryData,
            List<DerivedChannels> derivedData,
            LapAnalysisReport? coachingReport = null)
        {
            // Note: PdfSharpCore is included but PDF generation implementation
            // would be quite lengthy. This is a placeholder that generates an HTML report
            // which can be converted to PDF later.

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #2c3e50; }");
            html.AppendLine("h2 { color: #34495e; border-bottom: 2px solid #3498db; padding-bottom: 5px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #3498db; color: white; }");
            html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
            html.AppendLine(".score { font-weight: bold; font-size: 1.2em; }");
            html.AppendLine(".good { color: #27ae60; }");
            html.AppendLine(".average { color: #f39c12; }");
            html.AppendLine(".poor { color: #e74c3c; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            // Session Header
            html.AppendLine($"<h1>Telemetry Session Report</h1>");
            html.AppendLine($"<p><strong>Session:</strong> {session.Name}</p>");
            html.AppendLine($"<p><strong>Track:</strong> {session.TrackName ?? "Unknown"}</p>");
            html.AppendLine($"<p><strong>Date:</strong> {session.StartTime:yyyy-MM-dd HH:mm}</p>");
            html.AppendLine($"<p><strong>Driver:</strong> {session.DriverName ?? "Unknown"}</p>");
            html.AppendLine($"<p><strong>Vehicle:</strong> {session.VehicleName ?? "Unknown"}</p>");

            // Session Summary
            html.AppendLine("<h2>Session Summary</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
            html.AppendLine($"<tr><td>Total Laps</td><td>{laps.Count}</td></tr>");
            html.AppendLine($"<tr><td>Valid Laps</td><td>{laps.Count(l => l.IsValid)}</td></tr>");

            var bestLap = laps.Where(l => l.IsValid).OrderBy(l => l.LapTime).FirstOrDefault();
            if (bestLap != null)
            {
                html.AppendLine($"<tr><td>Best Lap Time</td><td>{FormatLapTime(bestLap.LapTime)} (Lap {bestLap.LapNumber})</td></tr>");
            }

            if (laps.Any())
            {
                var avgLapTime = laps.Where(l => l.IsValid).Average(l => l.LapTime);
                html.AppendLine($"<tr><td>Average Lap Time</td><td>{FormatLapTime(avgLapTime)}</td></tr>");
            }

            html.AppendLine("</table>");

            // Lap Times Table
            html.AppendLine("<h2>Lap Times</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Lap</th><th>Time</th><th>Delta</th><th>S1</th><th>S2</th><th>S3</th><th>Status</th></tr>");

            foreach (var lap in laps.OrderBy(l => l.LapNumber))
            {
                string deltaStr = lap.DeltaToBest.HasValue ? $"{(lap.DeltaToBest.Value > 0 ? "+" : "")}{lap.DeltaToBest.Value:F3}" : "-";
                string s1 = lap.Sector1Time.HasValue ? $"{lap.Sector1Time.Value:F3}" : "-";
                string s2 = lap.Sector2Time.HasValue ? $"{lap.Sector2Time.Value:F3}" : "-";
                string s3 = lap.Sector3Time.HasValue ? $"{lap.Sector3Time.Value:F3}" : "-";
                string status = lap.IsBestLap ? "🏆 Best" : (lap.IsValid ? "✓" : "✗ Invalid");

                html.AppendLine($"<tr>");
                html.AppendLine($"<td>{lap.LapNumber}</td>");
                html.AppendLine($"<td>{FormatLapTime(lap.LapTime)}</td>");
                html.AppendLine($"<td>{deltaStr}</td>");
                html.AppendLine($"<td>{s1}</td>");
                html.AppendLine($"<td>{s2}</td>");
                html.AppendLine($"<td>{s3}</td>");
                html.AppendLine($"<td>{status}</td>");
                html.AppendLine($"</tr>");
            }

            html.AppendLine("</table>");

            // Coaching Report
            if (coachingReport != null)
            {
                html.AppendLine("<h2>Driver Coaching Analysis</h2>");

                string scoreClass = coachingReport.OverallScore >= 80 ? "good" :
                                   coachingReport.OverallScore >= 60 ? "average" : "poor";

                html.AppendLine($"<p class='score {scoreClass}'>Overall Score: {coachingReport.OverallScore:F1}/100</p>");

                html.AppendLine("<h3>Performance Breakdown</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Category</th><th>Score</th><th>Feedback</th></tr>");
                html.AppendLine($"<tr><td>Consistency</td><td>{coachingReport.ConsistencyScore:F1}</td><td>-</td></tr>");
                html.AppendLine($"<tr><td>Throttle Usage</td><td>{coachingReport.ThrottleAnalysis.Score:F1}</td><td>{coachingReport.ThrottleAnalysis.Feedback}</td></tr>");
                html.AppendLine($"<tr><td>Steering Smoothness</td><td>{coachingReport.SteeringSmoothness.Score:F1}</td><td>{coachingReport.SteeringSmoothness.Feedback}</td></tr>");
                html.AppendLine("</table>");

                if (coachingReport.Recommendations.Any())
                {
                    html.AppendLine("<h3>Recommendations</h3>");
                    html.AppendLine("<ul>");
                    foreach (var rec in coachingReport.Recommendations)
                    {
                        html.AppendLine($"<li>{rec}</li>");
                    }
                    html.AppendLine("</ul>");
                }
            }

            html.AppendLine("</body></html>");

            // Write HTML report
            string htmlPath = outputPath.Replace(".pdf", ".html");
            await File.WriteAllTextAsync(htmlPath, html.ToString());

            return htmlPath;
        }

        /// <summary>
        /// Format lap time as MM:SS.mmm
        /// </summary>
        private string FormatLapTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            float remainingSeconds = seconds % 60;
            return $"{minutes}:{remainingSeconds:00.000}";
        }

        /// <summary>
        /// Export telemetry data to JSON with full detail
        /// </summary>
        public async Task ExportToDetailedJsonAsync(
            string filePath,
            SessionData session,
            List<LapData> laps,
            List<ComprehensiveTelemetryData> telemetryData,
            List<DerivedChannels> derivedData)
        {
            var export = new
            {
                Session = session,
                Laps = laps,
                TelemetryData = telemetryData,
                DerivedData = derivedData,
                ExportedAt = DateTime.Now
            };

            var json = JsonConvert.SerializeObject(export, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
