using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TelemetryDashboard.Models;
using TelemetryDashboard.Services;

namespace TelemetryDashboard.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly TelemetryService _telemetryService;
        private readonly DataStorageService _storageService;

        [ObservableProperty]
        private ObservableCollection<TelemetryData> _telemetryList = new();

        [ObservableProperty]
        private ObservableCollection<string> _statusMessages = new();

        [ObservableProperty]
        private string[] _availablePorts = Array.Empty<string>();

        [ObservableProperty]
        private string _selectedPort = "COM3";

        [ObservableProperty]
        private int _baudRate = 115200;

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private bool _isReading;

        [ObservableProperty]
        private bool _useAsciiMode = true;

        [ObservableProperty]
        private bool _autoSave = true;

        [ObservableProperty]
        private bool _enableLogging = true;

        [ObservableProperty]
        private int _totalRecordsCount;

        [ObservableProperty]
        private TelemetryData? _latestTelemetry;

        [ObservableProperty]
        private int _maxDisplayRecords = 100;

        public MainViewModel()
        {
            _telemetryService = new TelemetryService();
            _storageService = new DataStorageService();

            // Subscribe to events
            _telemetryService.TelemetryReceived += OnTelemetryReceived;
            _telemetryService.ErrorOccurred += OnErrorOccurred;
            _telemetryService.StatusChanged += OnStatusChanged;
            _storageService.StatusChanged += OnStorageStatusChanged;

            // Initialize
            RefreshPorts();
            AddStatusMessage("Application started");
            LoadRecentData();
        }

        [RelayCommand]
        private void RefreshPorts()
        {
            AvailablePorts = TelemetryService.GetAvailablePorts();
            AddStatusMessage($"Found {AvailablePorts.Length} serial port(s)");

            if (AvailablePorts.Length > 0 && !AvailablePorts.Contains(SelectedPort))
            {
                SelectedPort = AvailablePorts[0];
            }
        }

        [RelayCommand]
        private void Connect()
        {
            if (IsConnected)
            {
                Disconnect();
                return;
            }

            _telemetryService.PortName = SelectedPort;
            _telemetryService.BaudRate = BaudRate;
            _telemetryService.UseAsciiMode = UseAsciiMode;

            if (_telemetryService.Connect())
            {
                IsConnected = true;
                AddStatusMessage($"Connected to {SelectedPort}");
            }
        }

        [RelayCommand]
        private void Disconnect()
        {
            StopReading();
            _telemetryService.Disconnect();
            IsConnected = false;
        }

        [RelayCommand]
        private void ToggleReading()
        {
            if (IsReading)
            {
                StopReading();
            }
            else
            {
                StartReading();
            }
        }

        private void StartReading()
        {
            if (!IsConnected)
            {
                AddStatusMessage("Please connect to a serial port first");
                return;
            }

            if (EnableLogging)
            {
                _storageService.StartNewLogFile();
            }

            _telemetryService.StartReading();
            IsReading = true;
        }

        private void StopReading()
        {
            _telemetryService.StopReading();
            _storageService.CloseCurrentLogFile();
            IsReading = false;
        }

        [RelayCommand]
        private void ClearDisplay()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TelemetryList.Clear();
                AddStatusMessage("Display cleared");
            });
        }

        [RelayCommand]
        private async void ClearAllData()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete all stored telemetry data? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _storageService.ClearAllDataAsync();
                TotalRecordsCount = 0;
                ClearDisplay();
            }
        }

        [RelayCommand]
        private async void ExportToJson()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var endTime = DateTime.Now;
                var startTime = endTime.AddHours(-24); // Last 24 hours

                await _storageService.ExportToJsonAsync(startTime, endTime, saveDialog.FileName);
                AddStatusMessage($"Exported to: {saveDialog.FileName}");
            }
        }

        [RelayCommand]
        private async void ExportToCsv()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var endTime = DateTime.Now;
                var startTime = endTime.AddHours(-24); // Last 24 hours

                await _storageService.ExportToCsvAsync(startTime, endTime, saveDialog.FileName);
                AddStatusMessage($"Exported to: {saveDialog.FileName}");
            }
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            try
            {
                var logPath = _storageService.GetLogFolderPath();
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            catch (Exception ex)
            {
                AddStatusMessage($"Error opening log folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void LoadRecentData()
        {
            var recentData = await _storageService.GetRecentTelemetryAsync(MaxDisplayRecords);
            TotalRecordsCount = await _storageService.GetTelemetryCountAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                TelemetryList.Clear();
                foreach (var data in recentData)
                {
                    TelemetryList.Add(data);
                }
            });

            AddStatusMessage($"Loaded {recentData.Count} recent records (Total: {TotalRecordsCount})");
        }

        private async void OnTelemetryReceived(object? sender, TelemetryData data)
        {
            // Update UI on dispatcher thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                TelemetryList.Add(data);
                LatestTelemetry = data;

                // Keep display list manageable
                while (TelemetryList.Count > MaxDisplayRecords)
                {
                    TelemetryList.RemoveAt(0);
                }
            });

            // Save to database if enabled
            if (AutoSave)
            {
                await _storageService.SaveTelemetryAsync(data);
                TotalRecordsCount++;
            }

            // Write to log file if enabled
            if (EnableLogging)
            {
                _storageService.LogTelemetry(data);
            }
        }

        private void OnErrorOccurred(object? sender, string message)
        {
            AddStatusMessage($"ERROR: {message}");
        }

        private void OnStatusChanged(object? sender, string message)
        {
            AddStatusMessage(message);
        }

        private void OnStorageStatusChanged(object? sender, string message)
        {
            AddStatusMessage($"Storage: {message}");
        }

        private void AddStatusMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
                StatusMessages.Add(timestamped);

                // Keep status messages manageable
                while (StatusMessages.Count > 50)
                {
                    StatusMessages.RemoveAt(0);
                }
            });
        }

        public void Dispose()
        {
            _telemetryService?.Dispose();
            _storageService?.Dispose();
        }
    }
}
