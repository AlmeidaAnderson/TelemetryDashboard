# Telemetry Dashboard

A professional-grade telemetry data acquisition and analysis system with commercial datalogger features inspired by MoTeC i2 and AiM Race Studio. Includes comprehensive data acquisition, lap timing, driver coaching, and advanced analytics.

## Overview

This project provides a complete motorsport telemetry solution for reading, analyzing, and visualizing telemetry data. It includes:

1. **Python Reference Implementation** (`telemetry_service.py`) - Serial port reader with data decoding
2. **C# WPF Application** - Full-featured professional dashboard with:
   - 90+ telemetry channels
   - 60+ derived/calculated channels
   - Automatic lap timing and sector analysis
   - GPS track mapping
   - Driver coaching tools
   - Professional export formats (MoTeC CSV, AiM CSV)

## 🏎️ Commercial Datalogger Features

This system implements features found in professional motorsport dataloggers. See [FEATURES.md](FEATURES.md) for complete documentation.

### Core Capabilities
- **Comprehensive Data Acquisition**: GPS, IMU, engine, wheel speeds, driver inputs, suspension, tires, aerodynamics
- **Derived Channels Engine**: 60+ calculated channels including G-forces, slip angles, fuel consumption, power/torque
- **Lap Timing System**: Automatic lap detection, sector timing, predictive lap times, delta calculations
- **Track Mapping**: GPS-based track maps, corner detection, racing line analysis, speed heatmaps
- **Driver Coaching**: Braking analysis, corner performance breakdown, consistency scoring, personalized recommendations
- **Professional Exports**: MoTeC i2 CSV, AiM CSV, PDF reports, detailed JSON, GPX tracks

## Features

### Comprehensive Telemetry Data (90+ Channels)

#### Raw Sensor Inputs
- **GPS**: Latitude, longitude, speed, heading, altitude, satellite count
- **IMU**: 3-axis accelerometer & gyroscope (longitudinal, lateral, vertical G)
- **Engine**: RPM, throttle, manifold pressure, lambda, oil temp/pressure, coolant temp, fuel pressure
- **Wheel Speeds**: Individual corner speeds (FL, FR, RL, RR)
- **Driver Inputs**: Brake %, throttle %, steering angle, clutch, gear position
- **Digital Events**: Traction control, ABS, launch control, DRS flags
- **Suspension**: Travel for all 4 corners
- **Tires**: Pressure & temperature for all 4 corners
- **Brakes**: Temperature & pressure for all corners
- **Aerodynamics**: Front/rear wing angles, ride heights
- **Environmental**: Ambient temp, track temp, pressure, humidity
- **Electrical**: Battery & alternator voltage

#### Legacy Support
- **Temperature** (°C)
- **Pressure** (kPa)
- **Altitude** (m)
- **Velocity** (m/s)
- **Acceleration** (m/s²)

### Data Formats
- **ASCII Mode**: `TEMP:25.5,PRESS:101.3,ALT:1234.5,VEL:100.2,ACCEL:9.8`
- **Binary Mode**: 24-byte packets with start marker (0xFF 0xAA) and checksum

### C# WPF Application Features
- Real-time telemetry data display
- SQLite database storage for persistent data
- Text log file generation
- Export to JSON and CSV formats
- Configurable serial port settings
- Auto-save functionality
- Recent data loading
- Data visualization in DataGrid

## Project Structure

```
TelemetryDashboard/
├── telemetry_service.py              # Python reference implementation
├── TelemetryDashboard.WPF/           # C# WPF Application
│   ├── TelemetryDashboard.WPF.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   ├── Models/
│   │   └── TelemetryData.cs          # Data model
│   ├── Services/
│   │   ├── TelemetryService.cs       # Serial port communication
│   │   ├── DataStorageService.cs     # Database & file logging
│   │   └── TelemetryDbContext.cs     # Entity Framework context
│   ├── ViewModels/
│   │   └── MainViewModel.cs          # MVVM ViewModel
│   └── Converters/
│       └── ValueConverters.cs        # XAML value converters
└── README.md
```

## Building the C# WPF Application

### Prerequisites
- .NET 8.0 SDK or later
- Windows OS (for WPF)
- Visual Studio 2022 or later (recommended) or Visual Studio Code

### Build Instructions

#### Using .NET CLI
```bash
cd TelemetryDashboard.WPF
dotnet restore
dotnet build
dotnet run
```

#### Using Visual Studio
1. Open the solution in Visual Studio
2. Right-click on the project and select "Restore NuGet Packages"
3. Press F5 to build and run

## Using the Application

### 1. Connect to Serial Port
1. Select a serial port from the dropdown
2. Set the baud rate (default: 115200)
3. Click "Connect"

### 2. Start Reading Data
1. Choose data format (ASCII or Binary mode)
2. Enable/disable auto-save to database
3. Enable/disable logging to file
4. Click "Start Reading"

### 3. View Telemetry Data
- Real-time values displayed at the top
- Historical data shown in the DataGrid
- Status messages appear in the log at the bottom

### 4. Data Management
- **Clear Display**: Clear the on-screen data grid
- **Load Recent**: Load recent data from database
- **Export JSON**: Export data to JSON format
- **Export CSV**: Export data to CSV format
- **Open Logs**: Open the log file directory
- **Clear All Data**: Delete all stored telemetry data

## Data Storage

### Database
- **Location**: `%LocalAppData%\TelemetryDashboard\telemetry.db`
- **Type**: SQLite
- **Schema**:
  - Id (Primary Key)
  - Timestamp
  - Temperature, Pressure, Altitude, Velocity, Acceleration

### Log Files
- **Location**: `%LocalAppData%\TelemetryDashboard\Logs\`
- **Format**: Text files with `.log` extension
- **Naming**: `telemetry_YYYYMMDD_HHMMSS.log`

## Python Reference Implementation

### Usage
```python
from telemetry_service import TelemetryService

def on_data(telemetry):
    print(f"Temperature: {telemetry.temperature}°C")

service = TelemetryService(port='COM3', baudrate=115200)
if service.connect():
    service.start_reading(on_data, use_ascii=True)
    # ... keep running ...
    service.disconnect()
```

### Running the Python Script
```bash
pip install pyserial
python telemetry_service.py
```

## Serial Port Configuration

### Common Serial Ports
- **Windows**: COM1, COM2, COM3, etc.
- **Linux**: /dev/ttyUSB0, /dev/ttyACM0, etc.
- **macOS**: /dev/tty.usbserial, /dev/cu.usbserial, etc.

### Serial Settings
- **Baud Rate**: 115200 (default, configurable)
- **Data Bits**: 8
- **Parity**: None
- **Stop Bits**: 1
- **Flow Control**: None

## Dependencies

### C# WPF Application
- .NET 8.0
- System.IO.Ports (8.0.0)
- Newtonsoft.Json (13.0.3)
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Sqlite (8.0.0)
- CommunityToolkit.Mvvm (8.2.2)

### Python Reference
- Python 3.8+
- pyserial

## Troubleshooting

### Serial Port Issues
- Ensure the device is properly connected
- Check that no other application is using the port
- Verify the correct baud rate is set
- On Linux, ensure you have permissions: `sudo usermod -a -G dialout $USER`

### Database Issues
- Check available disk space
- Ensure write permissions to `%LocalAppData%`
- Database is created automatically on first run

### Data Not Appearing
- Verify the data format matches (ASCII vs Binary)
- Check serial port settings match the device
- Look for error messages in the status log

## License

This project is provided as-is for telemetry data acquisition and monitoring purposes.

## Contributing

When contributing to this project:
1. Follow C# naming conventions and coding standards
2. Maintain MVVM pattern in WPF application
3. Add appropriate error handling
4. Update documentation for new features
