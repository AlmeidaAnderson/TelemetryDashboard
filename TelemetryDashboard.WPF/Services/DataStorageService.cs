using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Handles data storage, logging, and retrieval for telemetry data
    /// </summary>
    public class DataStorageService : IDisposable
    {
        private readonly TelemetryDbContext _dbContext;
        private readonly string _logFolder;
        private StreamWriter? _currentLogWriter;
        private string? _currentLogFile;

        public event EventHandler<string>? StatusChanged;

        public DataStorageService()
        {
            _dbContext = new TelemetryDbContext();

            // Initialize database
            _dbContext.Database.EnsureCreated();

            // Setup log folder
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logFolder = Path.Combine(appDataFolder, "TelemetryDashboard", "Logs");
            Directory.CreateDirectory(_logFolder);
        }

        /// <summary>
        /// Save telemetry data to database
        /// </summary>
        public async Task SaveTelemetryAsync(TelemetryData data)
        {
            try
            {
                _dbContext.TelemetryData.Add(data);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Database save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save multiple telemetry records
        /// </summary>
        public async Task SaveTelemetryBatchAsync(IEnumerable<TelemetryData> dataList)
        {
            try
            {
                _dbContext.TelemetryData.AddRange(dataList);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Batch save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get telemetry data within a time range
        /// </summary>
        public async Task<List<TelemetryData>> GetTelemetryAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                return await _dbContext.TelemetryData
                    .Where(t => t.Timestamp >= startTime && t.Timestamp <= endTime)
                    .OrderBy(t => t.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Database query error: {ex.Message}");
                return new List<TelemetryData>();
            }
        }

        /// <summary>
        /// Get the most recent telemetry records
        /// </summary>
        public async Task<List<TelemetryData>> GetRecentTelemetryAsync(int count = 100)
        {
            try
            {
                return await _dbContext.TelemetryData
                    .OrderByDescending(t => t.Timestamp)
                    .Take(count)
                    .OrderBy(t => t.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Database query error: {ex.Message}");
                return new List<TelemetryData>();
            }
        }

        /// <summary>
        /// Get total number of telemetry records
        /// </summary>
        public async Task<int> GetTelemetryCountAsync()
        {
            try
            {
                return await _dbContext.TelemetryData.CountAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Database count error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Clear all telemetry data from database
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM TelemetryData");
                StatusChanged?.Invoke(this, "All data cleared from database");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Clear data error: {ex.Message}");
            }
        }

        /// <summary>
        /// Start a new log file for the current session
        /// </summary>
        public void StartNewLogFile()
        {
            try
            {
                CloseCurrentLogFile();

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(_logFolder, $"telemetry_{timestamp}.log");
                _currentLogWriter = new StreamWriter(_currentLogFile, append: true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                _currentLogWriter.WriteLine($"=== Telemetry Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                StatusChanged?.Invoke(this, $"Started new log file: {Path.GetFileName(_currentLogFile)}");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Log file creation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Write telemetry data to log file
        /// </summary>
        public void LogTelemetry(TelemetryData data)
        {
            if (_currentLogWriter == null)
                return;

            try
            {
                _currentLogWriter.WriteLine(data.ToString());
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Log write error: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the current log file
        /// </summary>
        public void CloseCurrentLogFile()
        {
            if (_currentLogWriter != null)
            {
                try
                {
                    _currentLogWriter.WriteLine($"=== Telemetry Log Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    _currentLogWriter.Close();
                    _currentLogWriter.Dispose();
                    _currentLogWriter = null;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke(this, $"Log close error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Export telemetry data to JSON file
        /// </summary>
        public async Task<bool> ExportToJsonAsync(DateTime startTime, DateTime endTime, string filePath)
        {
            try
            {
                var data = await GetTelemetryAsync(startTime, endTime);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
                StatusChanged?.Invoke(this, $"Exported {data.Count} records to JSON");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export telemetry data to CSV file
        /// </summary>
        public async Task<bool> ExportToCsvAsync(DateTime startTime, DateTime endTime, string filePath)
        {
            try
            {
                var data = await GetTelemetryAsync(startTime, endTime);

                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,Temperature,Pressure,Altitude,Velocity,Acceleration");

                foreach (var record in data)
                {
                    csv.AppendLine($"{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                 $"{record.Temperature}," +
                                 $"{record.Pressure}," +
                                 $"{record.Altitude}," +
                                 $"{record.Velocity}," +
                                 $"{record.Acceleration}");
                }

                await File.WriteAllTextAsync(filePath, csv.ToString());
                StatusChanged?.Invoke(this, $"Exported {data.Count} records to CSV");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of available log files
        /// </summary>
        public List<string> GetLogFiles()
        {
            try
            {
                return Directory.GetFiles(_logFolder, "*.log")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Get log files error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get the path to the log folder
        /// </summary>
        public string GetLogFolderPath() => _logFolder;

        public void Dispose()
        {
            CloseCurrentLogFile();
            _dbContext?.Dispose();
        }
    }
}
