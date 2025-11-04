using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemetryDashboard.Models
{
    /// <summary>
    /// Comprehensive telemetry data model supporting all commercial datalogger features
    /// </summary>
    public class ComprehensiveTelemetryData
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }
        public int SessionId { get; set; }
        public int? LapNumber { get; set; }

        // ==================== GPS Data ====================
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public float? GpsSpeed { get; set; }  // km/h
        public float? GpsHeading { get; set; }  // degrees
        public int? SatelliteCount { get; set; }
        public float? GpsAltitude { get; set; }  // meters

        // ==================== IMU/Accelerometer Data ====================
        public float? AccelX { get; set; }  // Longitudinal G
        public float? AccelY { get; set; }  // Lateral G
        public float? AccelZ { get; set; }  // Vertical G
        public float? GyroX { get; set; }   // Roll rate (deg/s)
        public float? GyroY { get; set; }   // Pitch rate (deg/s)
        public float? GyroZ { get; set; }   // Yaw rate (deg/s)

        // ==================== Engine Data ====================
        public int? EngineRPM { get; set; }
        public float? ThrottlePosition { get; set; }  // Percentage 0-100
        public float? ManifoldPressure { get; set; }  // kPa
        public float? Lambda { get; set; }  // AFR ratio
        public float? OilTemperature { get; set; }  // °C
        public float? OilPressure { get; set; }  // kPa
        public float? CoolantTemperature { get; set; }  // °C
        public float? FuelPressure { get; set; }  // kPa
        public float? AirTemperature { get; set; }  // °C
        public float? EngineLoad { get; set; }  // Percentage 0-100

        // ==================== Wheel Speed Data ====================
        public float? WheelSpeedFL { get; set; }  // Front Left (km/h)
        public float? WheelSpeedFR { get; set; }  // Front Right (km/h)
        public float? WheelSpeedRL { get; set; }  // Rear Left (km/h)
        public float? WheelSpeedRR { get; set; }  // Rear Right (km/h)

        // ==================== Driver Inputs ====================
        public float? BrakePosition { get; set; }  // Percentage 0-100
        public float? SteeringAngle { get; set; }  // degrees
        public float? ClutchPosition { get; set; }  // Percentage 0-100
        public int? GearPosition { get; set; }

        // ==================== Digital Events ====================
        public bool? TractionControlActive { get; set; }
        public bool? ABSActive { get; set; }
        public bool? LaunchControlActive { get; set; }
        public bool? PitLimiterActive { get; set; }
        public bool? DRSActive { get; set; }

        // ==================== Suspension & Braking ====================
        public float? SuspensionTravelFL { get; set; }  // mm
        public float? SuspensionTravelFR { get; set; }  // mm
        public float? SuspensionTravelRL { get; set; }  // mm
        public float? SuspensionTravelRR { get; set; }  // mm
        public float? BrakeTempFL { get; set; }  // °C
        public float? BrakeTempFR { get; set; }  // °C
        public float? BrakeTempRL { get; set; }  // °C
        public float? BrakeTempRR { get; set; }  // °C
        public float? BrakePressureFront { get; set; }  // bar
        public float? BrakePressureRear { get; set; }  // bar

        // ==================== Tire Data ====================
        public float? TirePressureFL { get; set; }  // bar
        public float? TirePressureFR { get; set; }  // bar
        public float? TirePressureRL { get; set; }  // bar
        public float? TirePressureRR { get; set; }  // bar
        public float? TireTempFL { get; set; }  // °C
        public float? TireTempFR { get; set; }  // °C
        public float? TireTempRL { get; set; }  // °C
        public float? TireTempRR { get; set; }  // °C

        // ==================== Aerodynamics ====================
        public float? FrontWingAngle { get; set; }  // degrees
        public float? RearWingAngle { get; set; }  // degrees
        public float? RideHeightFront { get; set; }  // mm
        public float? RideHeightRear { get; set; }  // mm

        // ==================== Environmental ====================
        public float? AmbientTemperature { get; set; }  // °C
        public float? TrackTemperature { get; set; }  // °C
        public float? AtmosphericPressure { get; set; }  // kPa
        public float? Humidity { get; set; }  // Percentage 0-100

        // ==================== Battery/Electrical ====================
        public float? BatteryVoltage { get; set; }  // Volts
        public float? AlternatorVoltage { get; set; }  // Volts

        // ==================== CAN Bus Data ====================
        public string? CanData { get; set; }  // JSON serialized CAN data

        // ==================== Distance/Position ====================
        public float? TrackDistance { get; set; }  // Meters from start/finish
        public float? LapDistance { get; set; }  // Meters into current lap

        // ==================== Legacy fields for backward compatibility ====================
        public float Temperature { get; set; }  // Generic temperature
        public float Pressure { get; set; }  // Generic pressure
        public float Altitude { get; set; }  // Generic altitude
        public float Velocity { get; set; }  // Generic velocity
        public float Acceleration { get; set; }  // Generic acceleration

        public ComprehensiveTelemetryData()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Creates from legacy TelemetryData for backward compatibility
        /// </summary>
        public static ComprehensiveTelemetryData FromLegacy(TelemetryData legacy)
        {
            return new ComprehensiveTelemetryData
            {
                Timestamp = legacy.Timestamp,
                Temperature = legacy.Temperature,
                Pressure = legacy.Pressure,
                Altitude = legacy.Altitude,
                Velocity = legacy.Velocity,
                Acceleration = legacy.Acceleration,
                // Map to appropriate specific fields
                CoolantTemperature = legacy.Temperature,
                AtmosphericPressure = legacy.Pressure,
                GpsAltitude = legacy.Altitude,
                GpsSpeed = legacy.Velocity * 3.6f,  // m/s to km/h
                AccelX = legacy.Acceleration / 9.81f  // m/s² to G
            };
        }

        /// <summary>
        /// Converts to legacy TelemetryData for backward compatibility
        /// </summary>
        public TelemetryData ToLegacy()
        {
            return new TelemetryData
            {
                Timestamp = Timestamp,
                Temperature = Temperature,
                Pressure = Pressure,
                Altitude = Altitude,
                Velocity = Velocity,
                Acceleration = Acceleration
            };
        }
    }
}
