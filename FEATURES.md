# Commercial Datalogger Features Implementation

This document describes all the commercial datalogger functionalities implemented in the Telemetry Dashboard, inspired by MoTeC i2 and AiM dataloggers.

## Table of Contents
- [Core Data Acquisition](#core-data-acquisition)
- [Derived Channels & Math Engine](#derived-channels--math-engine)
- [Lap Timing & Analysis](#lap-timing--analysis)
- [Track Mapping & GPS](#track-mapping--gps)
- [Driver Coaching Tools](#driver-coaching-tools)
- [Visualization Suite](#visualization-suite)
- [Export & File Handling](#export--file-handling)
- [Session Management](#session-management)

---

## Core Data Acquisition

### Supported Sensor Inputs

The `ComprehensiveTelemetryData` model supports all major sensor types found in professional dataloggers:

#### GPS Data
- Latitude/Longitude coordinates
- GPS Speed (km/h)
- GPS Heading (degrees)
- Satellite count
- GPS Altitude

#### IMU/Accelerometer Data
- 3-axis accelerometer (AccelX, AccelY, AccelZ) in G-forces
- 3-axis gyroscope (GyroX, GyroY, GyroZ) in deg/s
- Longitudinal, lateral, and vertical G measurements

#### Engine Data
- Engine RPM
- Throttle position (0-100%)
- Manifold pressure (kPa)
- Lambda (AFR ratio)
- Oil temperature & pressure
- Coolant temperature
- Fuel pressure
- Air temperature
- Engine load percentage

#### Wheel Speed Data
- Individual corner wheel speeds (FL, FR, RL, RR) in km/h
- Used for slip calculation and ground speed

#### Driver Inputs
- Brake position (0-100%)
- Steering angle (degrees)
- Clutch position (0-100%)
- Gear position

#### Digital Events
- Traction control active
- ABS active
- Launch control active
- Pit limiter active
- DRS active

#### Suspension & Braking
- Suspension travel for all 4 corners (mm)
- Brake temperature for all 4 corners (°C)
- Brake pressure front/rear (bar)

#### Tire Data
- Tire pressure for all 4 corners (bar)
- Tire temperature for all 4 corners (°C)

#### Aerodynamics
- Front/rear wing angle (degrees)
- Ride height front/rear (mm)

#### Environmental
- Ambient temperature
- Track temperature
- Atmospheric pressure
- Humidity

#### Electrical
- Battery voltage
- Alternator voltage

---

## Derived Channels & Math Engine

The `MathEngineService` calculates over 60 derived channels from raw sensor data:

### Vehicle Dynamics
- **Longitudinal G**: Calculated from AccelX
- **Lateral G**: Calculated from AccelY
- **Combined G**: sqrt(LongG² + LatG²) for G-G diagrams
- **Slip Angle**: Vehicle slip angle in degrees
- **Yaw Rate**: From GyroZ
- **Roll/Pitch Angles**: Integrated from gyro rates

### Power & Performance
- **Engine Power**: Calculated in kW from RPM and manifold pressure
- **Engine Torque**: Estimated in Nm
- **Power-to-Weight Ratio**: kW/kg

### Fuel Consumption
- **Instantaneous Fuel Consumption**: L/h based on RPM and throttle
- **Cumulative Fuel Used**: Running total in liters
- **Fuel Remaining**: Estimated fuel in tank
- **Fuel Consumption Rate**: L/100km
- **Laps Remaining**: Estimated laps on current fuel

### Lap Timing Channels
- **Lap Time**: Current lap elapsed time
- **Predicted Lap Time**: Estimated lap time based on current pace
- **Lap Time Delta**: vs reference lap (in seconds)
- **Sector Times**: Individual sector timing
- **Sector Deltas**: vs reference lap
- **Distance into Lap**: Meters from start
- **Time into Lap**: Seconds from lap start

### Speed & Distance
- **Ground Speed**: Average of all wheel speeds
- **Wheel Slip**: Per-corner slip percentage
- **Average Wheel Slip**: Overall slip metric

### Braking Analysis
- **Braking Force**: Calculated in Newtons
- **Braking Distance**: Per braking event
- **Braking G-Force**: Deceleration in Gs
- **Brake Balance**: Front/rear distribution %
- **Brake Application Rate**: %/s
- **Brake Release Rate**: %/s

### Throttle Analysis
- **Is Coasting**: Boolean flag
- **Throttle Application Rate**: %/s
- **Throttle Commitment**: % time at full throttle
- **Partial Throttle Time**: Time spent in partial throttle

### Corner Analysis
- **Corner Phase**: Entry, Apex, Exit, Straight
- **Corner Speed**: Current speed in corner
- **Min/Max Corner Speed**: Speed range in corner
- **Corner G-Force**: Lateral G in corner
- **Corner Number**: Identified corner

### Steering Analysis
- **Steering Rate**: deg/s
- **Understeer Angle**: Calculated understeer
- **Oversteer Angle**: Calculated oversteer

### Consistency Metrics
- **Lap Time Variation**: Standard deviation from mean
- **Braking Consistency**: Score 0-100
- **Apex Speed Consistency**: Score 0-100
- **Throttle Consistency**: Score 0-100

---

## Lap Timing & Analysis

The `LapTimingService` provides professional lap timing features:

### Automatic Lap Detection
- GPS-based start/finish line crossing detection
- Configurable tolerance (default 20 meters)
- Automatic lap counting

### Sector Timing
- Support for up to 5 sectors per lap
- GPS-based sector markers
- Automatic sector time calculation
- Sector delta vs reference lap

### Rolling Lap Timer
- Real-time lap time prediction
- Predictive lap time like MoTeC/AiM
- Time gain/loss calculation at current position

### Lap Delta
- Real-time delta vs reference lap
- Distance-synchronized comparison
- Time-synchronized comparison

### Lap Statistics
- Max/min speeds per lap
- Max RPM per lap
- Max G-forces (longitudinal, lateral, combined)
- Fuel used per lap
- Average speeds

---

## Track Mapping & GPS

The `TrackMappingService` provides GPS-based track analysis:

### Track Map Generation
- Automatic track map from GPS coordinates
- Distance calculation along track
- Track boundaries detection
- Track length measurement

### Corner Detection
- Automatic corner identification from lateral G
- Corner entry/apex/exit detection
- Corner numbering
- Direction (left/right) detection

### Racing Line Analysis
- Racing line quality score (0-100)
- Deviation from ideal line
- Line consistency measurement

### Heatmap Generation
- **Speed Heatmap**: Color-coded speed visualization
  - Green = fast, Yellow = medium, Red = slow
- **Brake Heatmap**: Braking zones visualization
- **Throttle Heatmap**: Throttle application zones

### GPX Export
- Standard GPX format export
- Compatible with mapping software
- Includes speed and timestamp data

---

## Driver Coaching Tools

The `DriverCoachingService` provides detailed performance analysis:

### Braking Event Analysis
For each braking zone:
- Entry speed
- Exit speed
- Peak brake pressure
- Brake application rate
- Brake release rate
- Braking distance
- Maximum deceleration (G)
- Performance score (0-100)
- Specific feedback

### Corner Performance Breakdown

#### Entry Analysis
- Entry speed
- Braking point
- Trail braking effectiveness
- Entry score with feedback

#### Apex Analysis
- Apex speed
- Minimum corner speed
- Apex line deviation
- Apex score with feedback

#### Exit Analysis
- Exit speed
- Throttle application point
- Exit acceleration
- Exit score with feedback

### Throttle Usage Analysis
- Full throttle percentage
- Partial throttle percentage
- Coasting percentage
- Throttle commitment score
- Specific recommendations

### Steering Smoothness Analysis
- Average steering rate
- Steering input smoothness score
- Feedback on steering technique

### Consistency Tracking
- Lap-to-lap consistency score
- Standard deviation of lap times
- Rolling consistency metrics

### Overall Driver Score
- Composite score (0-100) from:
  - Consistency score
  - Throttle usage score
  - Steering smoothness score
  - Average braking score
  - Average corner score

### Recommendations Engine
Automatic generation of improvement recommendations based on:
- Poor braking events
- Weak corner performance
- Throttle usage issues
- Steering smoothness issues
- Consistency problems

---

## Visualization Suite

### Supported Visualization Types

The dashboard supports multiple visualization approaches (implemented via OxyPlot):

#### Multi-Channel Time Graphs
- Plot multiple channels vs time
- Synchronized cursors
- Zoom and pan
- Channel overlay

#### Histogram Views
- Throttle histogram
- Brake histogram
- Speed distribution
- G-force distribution

#### XY Scatter Plots
- Brake vs Speed
- Steering vs Speed
- G-G diagram (lateral vs longitudinal)
- Power vs RPM

#### Track Map Visualization
- GPS trace plotting
- Color-coded by metric:
  - Speed
  - Throttle
  - Brake
  - G-force
  - Gear
- Corner highlighting
- Sector markers

#### Lap Overlays
- Multiple lap comparison
- Synchronized by:
  - Time
  - Distance
  - GPS position
- Delta visualization

---

## Export & File Handling

The `AdvancedExportService` supports multiple export formats:

### MoTeC i2 CSV Format
- Compatible with MoTeC i2 Pro software
- Standard MoTeC CSV headers
- All channels included:
  - Time, Distance, Speed
  - Engine parameters
  - GPS data
  - IMU data
  - Wheel speeds
  - Driver inputs
- Ready for import into i2

### AiM CSV Format
- Compatible with AiM Race Studio
- AiM-specific formatting:
  - Semicolon-separated values
  - Units row
  - AiM channel naming
- Full sensor coverage

### PDF Session Reports
- Comprehensive HTML-based reports
- Includes:
  - Session summary
  - Lap times table
  - Sector times
  - Driver coaching analysis
  - Performance breakdown
  - Recommendations
- Professional formatting
- Color-coded scores

### Detailed JSON Export
- Full data export in JSON format
- Includes:
  - Session metadata
  - All laps
  - Complete telemetry data
  - All derived channels
- Structured for data analysis

### GPX Track Export
- Standard GPX format
- Track coordinates
- Speed data
- Timestamp information

---

## Session Management

The `SessionManagementService` provides comprehensive session handling:

### Session Organization
- Session folders by date/time
- Automatic folder creation
- Metadata tracking:
  - Track name
  - Vehicle name
  - Driver name
  - Session type (Practice, Qualifying, Race, Test)
  - Start/end times

### Session Statistics
- Total laps
- Valid laps
- Best lap time and number
- Average lap time
- Session duration

### Data Lifecycle
- Automatic data saving during session
- Real-time telemetry processing
- Lap-by-lap data organization
- Session archiving

### Database Management
- SQLite database storage
- Indexed queries for performance
- Relational data structure:
  - Sessions → Laps → Telemetry → Derived Channels
- Efficient data retrieval

### Session Retrieval
- List all sessions
- Filter by date/track/vehicle
- Load session data
- Load lap data
- Load telemetry for specific lap

### Session Deletion
- Cascading delete of all related data:
  - Laps
  - Telemetry
  - Derived channels
  - Sector data
- Folder cleanup

---

## Architecture Overview

### Services Layer
```
TelemetryService (existing)
├── Serial port communication
└── Data packet parsing

MathEngineService
├── Derived channel calculation
├── Vehicle dynamics
├── Fuel consumption
└── Performance metrics

LapTimingService
├── Lap detection
├── Sector timing
├── Delta calculation
└── Predictive timing

TrackMappingService
├── GPS processing
├── Track map generation
├── Corner detection
└── Heatmap generation

DriverCoachingService
├── Braking analysis
├── Corner analysis
├── Consistency tracking
└── Recommendation engine

SessionManagementService
├── Session lifecycle
├── Data coordination
├── Database operations
└── File management

AdvancedExportService
├── MoTeC CSV export
├── AiM CSV export
├── PDF report generation
└── JSON/GPX export
```

### Data Models
```
ComprehensiveTelemetryData (90+ fields)
├── Raw sensor data
├── GPS coordinates
├── IMU data
├── Engine parameters
├── Driver inputs
└── Environmental data

DerivedChannels (60+ fields)
├── Calculated metrics
├── Performance indicators
├── Analysis results
└── Derived timing

LapData
├── Lap timing
├── Sector times
├── Performance metrics
└── Conditions

SessionData
├── Session metadata
├── Track information
├── Vehicle/driver info
└── Session statistics
```

### Database Schema
```
SessionData (1) → (M) LapData
LapData (1) → (M) SectorData
SessionData (1) → (M) ComprehensiveTelemetryData
ComprehensiveTelemetryData (1) → (1) DerivedChannels
```

---

## Usage Examples

### Starting a Session
```csharp
var session = await sessionManager.StartSessionAsync(
    sessionName: "Morning Practice",
    sessionType: "Practice",
    trackName: "Silverstone GP",
    vehicleName: "Formula 3",
    driverName: "Driver Name"
);
```

### Processing Telemetry
```csharp
var telemetry = new ComprehensiveTelemetryData
{
    // Set GPS data
    Latitude = 52.0786,
    Longitude = -1.0169,
    GpsSpeed = 180.5f,

    // Set engine data
    EngineRPM = 8500,
    ThrottlePosition = 95.0f,

    // Set IMU data
    AccelX = 15.0f,  // 1.53 G
    AccelY = -8.0f,  // 0.82 G lateral

    // ... etc
};

await sessionManager.ProcessTelemetryAsync(telemetry);
```

### Setting Up Track Layout
```csharp
sessionManager.SetTrackLayout(
    startFinishLat: 52.0786,
    startFinishLon: -1.0169,
    sectorMarkers: new List<(double, double, int)>
    {
        (52.0800, -1.0180, 1),  // Sector 1
        (52.0820, -1.0200, 2),  // Sector 2
        (52.0770, -1.0150, 3)   // Sector 3
    }
);
```

### Exporting Data
```csharp
// Export to MoTeC format
await exportService.ExportToMoTeCCsvAsync(
    "session_motec.csv",
    session,
    telemetryData,
    derivedData
);

// Generate PDF report
await exportService.GenerateSessionPdfReport(
    "session_report.pdf",
    session,
    laps,
    telemetryData,
    derivedData,
    coachingReport
);
```

---

## Future Enhancements

### Planned Features
1. Real-time graphing during data acquisition
2. Interactive track map with live position
3. Live delta bar (vs reference lap)
4. CAN bus integration for direct ECU logging
5. Network streaming for remote monitoring
6. Cloud synchronization
7. Multi-session comparison
8. Setup change tracking
9. Tire wear modeling
10. Fuel strategy optimization

### Advanced Analysis
1. Virtual model comparison
2. Theoretical lap time calculation
3. Optimal shift point calculation
4. Brake bias optimization
5. Aerodynamic balance analysis
6. Tire temperature modeling
7. Engine power curve analysis

---

## Technical Requirements

### Dependencies
- .NET 8.0
- Entity Framework Core 8.0 (SQLite)
- OxyPlot.Wpf 2.1.2
- PdfSharpCore 1.3.65
- MathNet.Numerics 5.0.0
- Newtonsoft.Json 13.0.3

### Hardware Requirements
- GPS module for track mapping
- IMU (accelerometer + gyroscope) for vehicle dynamics
- CAN bus interface or data acquisition device
- Windows PC for dashboard application

### Data Acquisition Device Requirements
The telemetry system expects data in either:
1. ASCII format: `TEMP:25.5,PRESS:101.3,...`
2. Binary format: 24-byte packets with checksum

For full functionality, the data acquisition device should provide:
- GPS coordinates at ≥10 Hz
- IMU data at ≥100 Hz
- Engine parameters via CAN or analog inputs
- Wheel speeds via frequency inputs
- Driver inputs via analog/digital channels

---

## License & Credits

This implementation is inspired by commercial dataloggers:
- **MoTeC i2**: Professional motorsport data analysis
- **AiM Race Studio**: Racing data acquisition and analysis

The implementation is compatible with their data formats for export purposes.

---

## Support & Documentation

For more information, see:
- `README.md` - General project information
- `TelemetryDashboard.WPF/Models/` - Data model definitions
- `TelemetryDashboard.WPF/Services/` - Service implementations

## Contact

For questions or contributions, please open an issue on GitHub.
