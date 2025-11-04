using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemetryDashboard.Models
{
    /// <summary>
    /// Derived/calculated channels from raw telemetry data
    /// </summary>
    public class DerivedChannels
    {
        [Key]
        public int Id { get; set; }

        public int TelemetryDataId { get; set; }
        public DateTime Timestamp { get; set; }

        // ==================== Vehicle Dynamics ====================
        public float? LongitudinalG { get; set; }  // Calculated from AccelX
        public float? LateralG { get; set; }  // Calculated from AccelY
        public float? CombinedG { get; set; }  // sqrt(LongG² + LatG²)
        public float? SlipAngle { get; set; }  // degrees
        public float? YawRate { get; set; }  // deg/s from GyroZ
        public float? RollAngle { get; set; }  // degrees (integrated from GyroX)
        public float? PitchAngle { get; set; }  // degrees (integrated from GyroY)

        // ==================== Power & Performance ====================
        public float? EnginePower { get; set; }  // kW (calculated)
        public float? EngineTorque { get; set; }  // Nm (calculated)
        public float? PowerToWeightRatio { get; set; }  // kW/kg

        // ==================== Fuel Consumption ====================
        public float? InstantaneousFuelConsumption { get; set; }  // L/h
        public float? CumulativeFuelUsed { get; set; }  // Liters
        public float? FuelRemaining { get; set; }  // Liters (estimated)
        public float? FuelConsumptionRate { get; set; }  // L/100km
        public float? LapsRemaining { get; set; }  // Estimated laps on current fuel

        // ==================== Lap Timing & Analysis ====================
        public float? LapTime { get; set; }  // seconds
        public float? PredictedLapTime { get; set; }  // seconds
        public float? LapTimeDelta { get; set; }  // seconds vs reference lap
        public float? SectorTime { get; set; }  // seconds
        public int? SectorNumber { get; set; }
        public float? SectorDelta { get; set; }  // seconds vs reference
        public float? DistanceIntoLap { get; set; }  // meters
        public float? TimeIntoLap { get; set; }  // seconds
        public bool? IsNewLap { get; set; }
        public bool? IsNewSector { get; set; }

        // ==================== Speed & Distance ====================
        public float? GroundSpeed { get; set; }  // km/h (average of wheel speeds)
        public float? WheelSlipFL { get; set; }  // Percentage
        public float? WheelSlipFR { get; set; }  // Percentage
        public float? WheelSlipRL { get; set; }  // Percentage
        public float? WheelSlipRR { get; set; }  // Percentage
        public float? AverageWheelSlip { get; set; }  // Percentage

        // ==================== Braking Analysis ====================
        public float? BrakingForce { get; set; }  // N (calculated)
        public float? BrakingDistance { get; set; }  // meters (calculated for event)
        public float? BrakingGForce { get; set; }  // G
        public float? BrakeBalance { get; set; }  // Front/Rear percentage
        public bool? IsBraking { get; set; }
        public float? BrakeApplicationRate { get; set; }  // %/s
        public float? BrakeReleaseRate { get; set; }  // %/s

        // ==================== Throttle Analysis ====================
        public bool? IsCoasting { get; set; }
        public float? ThrottleApplicationRate { get; set; }  // %/s
        public float? ThrottleCommitment { get; set; }  // 0-100 (% of time at full throttle)
        public float? PartialThrottleTime { get; set; }  // seconds

        // ==================== Corner Analysis ====================
        public string? CornerPhase { get; set; }  // Entry, Apex, Exit, Straight
        public float? CornerSpeed { get; set; }  // km/h
        public float? MinCornerSpeed { get; set; }  // km/h (minimum in corner)
        public float? MaxCornerSpeed { get; set; }  // km/h (maximum in corner)
        public float? CornerGForce { get; set; }  // G (lateral)
        public int? CornerNumber { get; set; }

        // ==================== Steering Analysis ====================
        public float? SteeringRate { get; set; }  // deg/s
        public float? UnderSteerAngle { get; set; }  // degrees (calculated)
        public float? OverSteerAngle { get; set; }  // degrees (calculated)

        // ==================== Aerodynamics ====================
        public float? DownForce { get; set; }  // N (estimated)
        public float? DragForce { get; set; }  // N (estimated)
        public float? AerodynamicBalance { get; set; }  // Front/Rear %

        // ==================== Temperature Deltas ====================
        public float? TireHeatFL { get; set; }  // °C delta from start
        public float? TireHeatFR { get; set; }  // °C delta from start
        public float? TireHeatRL { get; set; }  // °C delta from start
        public float? TireHeatRR { get; set; }  // °C delta from start
        public float? BrakeHeatFL { get; set; }  // °C delta from start
        public float? BrakeHeatFR { get; set; }  // °C delta from start
        public float? BrakeHeatRL { get; set; }  // °C delta from start
        public float? BrakeHeatRR { get; set; }  // °C delta from start

        // ==================== Gear Analysis ====================
        public float? TimeInGear { get; set; }  // seconds
        public float? OptimalShiftPoint { get; set; }  // RPM
        public bool? ShiftWarning { get; set; }
        public float? GearRatio { get; set; }  // Calculated gear ratio

        // ==================== Track Position ====================
        public float? TrackProgress { get; set; }  // Percentage 0-100
        public string? TrackSegment { get; set; }  // Name of current segment
        public float? SpeedTrap { get; set; }  // km/h at predefined points

        // ==================== Consistency Metrics ====================
        public float? LapTimeVariation { get; set; }  // Std deviation from mean
        public float? BrakingConsistency { get; set; }  // Score 0-100
        public float? ApexSpeedConsistency { get; set; }  // Score 0-100
        public float? ThrottleConsistency { get; set; }  // Score 0-100

        // ==================== Driver Performance ====================
        public float? DriverScore { get; set; }  // Overall 0-100
        public float? SmoothnessFactor { get; set; }  // 0-100
        public float? AggressionIndex { get; set; }  // 0-100

        // ==================== Energy Management (for hybrid/electric) ====================
        public float? EnergyRecovered { get; set; }  // kJ
        public float? EnergyDeployed { get; set; }  // kJ
        public float? BatterySOC { get; set; }  // State of Charge %
        public float? RegenerationRate { get; set; }  // kW

        public DerivedChannels()
        {
            Timestamp = DateTime.Now;
        }
    }
}
