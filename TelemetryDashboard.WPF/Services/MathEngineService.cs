using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Math engine for calculating derived channels from raw telemetry
    /// </summary>
    public class MathEngineService
    {
        private const float GRAVITY = 9.81f;  // m/s²
        private const float KMH_TO_MS = 3.6f;

        private Queue<ComprehensiveTelemetryData> _dataBuffer = new();
        private const int BUFFER_SIZE = 100;

        // Vehicle configuration (should be loaded from setup)
        private float _vehicleMass = 600f;  // kg
        private float _fuelTankCapacity = 50f;  // liters
        private float _fuelDensity = 0.75f;  // kg/L
        private float _frontalArea = 1.5f;  // m²
        private float _dragCoefficient = 0.3f;
        private float _wheelbase = 2.5f;  // meters
        private float _trackWidth = 1.5f;  // meters

        /// <summary>
        /// Calculate all derived channels from raw telemetry
        /// </summary>
        public DerivedChannels CalculateDerivedChannels(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous = null)
        {
            var derived = new DerivedChannels
            {
                TelemetryDataId = current.Id,
                Timestamp = current.Timestamp
            };

            // Add to buffer for rolling calculations
            _dataBuffer.Enqueue(current);
            if (_dataBuffer.Count > BUFFER_SIZE)
                _dataBuffer.Dequeue();

            // Calculate vehicle dynamics
            CalculateVehicleDynamics(current, previous, derived);

            // Calculate power and torque
            CalculatePowerTorque(current, derived);

            // Calculate fuel consumption
            CalculateFuelConsumption(current, previous, derived);

            // Calculate wheel slip
            CalculateWheelSlip(current, derived);

            // Calculate braking metrics
            CalculateBrakingMetrics(current, previous, derived);

            // Calculate throttle metrics
            CalculateThrottleMetrics(current, previous, derived);

            // Calculate steering metrics
            CalculateSteeringMetrics(current, previous, derived);

            // Calculate corner phase
            CalculateCornerPhase(current, derived);

            // Calculate consistency metrics
            CalculateConsistencyMetrics(derived);

            return derived;
        }

        /// <summary>
        /// Calculate vehicle dynamics (G-forces, slip angle, etc.)
        /// </summary>
        private void CalculateVehicleDynamics(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous,
            DerivedChannels derived)
        {
            // Longitudinal and lateral G
            if (current.AccelX.HasValue)
                derived.LongitudinalG = current.AccelX.Value / GRAVITY;

            if (current.AccelY.HasValue)
                derived.LateralG = current.AccelY.Value / GRAVITY;

            // Combined G (G-G diagram)
            if (derived.LongitudinalG.HasValue && derived.LateralG.HasValue)
            {
                derived.CombinedG = (float)Math.Sqrt(
                    Math.Pow(derived.LongitudinalG.Value, 2) +
                    Math.Pow(derived.LateralG.Value, 2)
                );
            }

            // Yaw rate from gyro
            if (current.GyroZ.HasValue)
                derived.YawRate = current.GyroZ.Value;

            // Slip angle calculation (simplified)
            if (current.GyroZ.HasValue && current.GpsSpeed.HasValue && current.GpsSpeed.Value > 10)
            {
                float speed = current.GpsSpeed.Value / KMH_TO_MS;  // Convert to m/s
                derived.SlipAngle = (float)Math.Atan2(
                    current.GyroZ.Value * Math.PI / 180,  // Convert to rad/s
                    speed
                ) * 180f / (float)Math.PI;  // Convert back to degrees
            }

            // Calculate roll and pitch angles (integration - simplified)
            if (current.GyroX.HasValue && previous?.GyroX.HasValue == true)
            {
                float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                derived.RollAngle = current.GyroX.Value * dt;  // Simplified
            }

            if (current.GyroY.HasValue && previous?.GyroY.HasValue == true)
            {
                float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                derived.PitchAngle = current.GyroY.Value * dt;  // Simplified
            }
        }

        /// <summary>
        /// Calculate engine power and torque
        /// </summary>
        private void CalculatePowerTorque(ComprehensiveTelemetryData current, DerivedChannels derived)
        {
            if (current.EngineRPM.HasValue && current.ManifoldPressure.HasValue)
            {
                // Simplified torque estimation based on RPM and manifold pressure
                float rpm = current.EngineRPM.Value;
                float map = current.ManifoldPressure.Value;  // kPa

                // Rough estimation: Torque (Nm) = MAP * displacement factor / RPM factor
                // This is very simplified - real calculation would use VE tables
                float torqueEstimate = (map / 100f) * 200f * (1 - (rpm - 4000f) / 10000f);
                torqueEstimate = Math.Max(0, torqueEstimate);
                derived.EngineTorque = torqueEstimate;

                // Power (kW) = Torque (Nm) * RPM * 2π / 60000
                derived.EnginePower = (torqueEstimate * rpm * 2f * (float)Math.PI) / 60000f;

                // Power to weight ratio
                derived.PowerToWeightRatio = derived.EnginePower / _vehicleMass;
            }
        }

        /// <summary>
        /// Calculate fuel consumption metrics
        /// </summary>
        private void CalculateFuelConsumption(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous,
            DerivedChannels derived)
        {
            if (current.EngineRPM.HasValue && current.ThrottlePosition.HasValue)
            {
                float rpm = current.EngineRPM.Value;
                float throttle = current.ThrottlePosition.Value / 100f;

                // Simplified fuel consumption model (L/h)
                // Base consumption + RPM factor + throttle factor
                float baseConsumption = 2f;  // L/h at idle
                float rpmFactor = (rpm / 1000f) * 0.5f;
                float throttleFactor = throttle * 10f;

                derived.InstantaneousFuelConsumption = baseConsumption + rpmFactor + throttleFactor;

                // Calculate cumulative fuel used
                if (previous != null)
                {
                    float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                    float fuelUsedThisSample = (derived.InstantaneousFuelConsumption.Value / 3600f) * dt;

                    // This would need to accumulate across samples
                    derived.CumulativeFuelUsed = fuelUsedThisSample;
                }

                // Estimate fuel remaining
                derived.FuelRemaining = _fuelTankCapacity - (derived.CumulativeFuelUsed ?? 0);

                // Calculate fuel consumption rate (L/100km)
                if (current.GpsSpeed.HasValue && current.GpsSpeed.Value > 1)
                {
                    float speed = current.GpsSpeed.Value;  // km/h
                    derived.FuelConsumptionRate = (derived.InstantaneousFuelConsumption.Value / speed) * 100f;
                }
            }
        }

        /// <summary>
        /// Calculate wheel slip percentages
        /// </summary>
        private void CalculateWheelSlip(ComprehensiveTelemetryData current, DerivedChannels derived)
        {
            if (current.WheelSpeedFL.HasValue && current.WheelSpeedFR.HasValue &&
                current.WheelSpeedRL.HasValue && current.WheelSpeedRR.HasValue)
            {
                // Ground speed (average of all wheels)
                float avgSpeed = (current.WheelSpeedFL.Value + current.WheelSpeedFR.Value +
                                 current.WheelSpeedRL.Value + current.WheelSpeedRR.Value) / 4f;
                derived.GroundSpeed = avgSpeed;

                // Calculate slip for each wheel
                if (avgSpeed > 1f)
                {
                    derived.WheelSlipFL = ((current.WheelSpeedFL.Value - avgSpeed) / avgSpeed) * 100f;
                    derived.WheelSlipFR = ((current.WheelSpeedFR.Value - avgSpeed) / avgSpeed) * 100f;
                    derived.WheelSlipRL = ((current.WheelSpeedRL.Value - avgSpeed) / avgSpeed) * 100f;
                    derived.WheelSlipRR = ((current.WheelSpeedRR.Value - avgSpeed) / avgSpeed) * 100f;

                    derived.AverageWheelSlip = (
                        Math.Abs(derived.WheelSlipFL.Value) +
                        Math.Abs(derived.WheelSlipFR.Value) +
                        Math.Abs(derived.WheelSlipRL.Value) +
                        Math.Abs(derived.WheelSlipRR.Value)
                    ) / 4f;
                }
            }
        }

        /// <summary>
        /// Calculate braking metrics
        /// </summary>
        private void CalculateBrakingMetrics(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous,
            DerivedChannels derived)
        {
            if (current.BrakePosition.HasValue)
            {
                derived.IsBraking = current.BrakePosition.Value > 5f;

                // Braking G-force
                if (derived.LongitudinalG.HasValue && derived.IsBraking.Value)
                {
                    derived.BrakingGForce = Math.Abs(derived.LongitudinalG.Value);
                }

                // Braking force (simplified)
                if (derived.BrakingGForce.HasValue)
                {
                    derived.BrakingForce = derived.BrakingGForce.Value * _vehicleMass * GRAVITY;
                }

                // Brake application/release rate
                if (previous?.BrakePosition.HasValue == true)
                {
                    float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                    if (dt > 0)
                    {
                        float brakeChange = current.BrakePosition.Value - previous.BrakePosition.Value;
                        if (brakeChange > 0)
                            derived.BrakeApplicationRate = brakeChange / dt;
                        else if (brakeChange < 0)
                            derived.BrakeReleaseRate = Math.Abs(brakeChange) / dt;
                    }
                }

                // Brake balance
                if (current.BrakePressureFront.HasValue && current.BrakePressureRear.HasValue)
                {
                    float total = current.BrakePressureFront.Value + current.BrakePressureRear.Value;
                    if (total > 0)
                    {
                        derived.BrakeBalance = (current.BrakePressureFront.Value / total) * 100f;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate throttle metrics
        /// </summary>
        private void CalculateThrottleMetrics(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous,
            DerivedChannels derived)
        {
            if (current.ThrottlePosition.HasValue)
            {
                // Coasting detection
                derived.IsCoasting = current.ThrottlePosition.Value < 5f &&
                                    (current.BrakePosition ?? 0) < 5f;

                // Throttle application rate
                if (previous?.ThrottlePosition.HasValue == true)
                {
                    float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                    if (dt > 0)
                    {
                        float throttleChange = current.ThrottlePosition.Value - previous.ThrottlePosition.Value;
                        if (throttleChange > 0)
                            derived.ThrottleApplicationRate = throttleChange / dt;
                    }
                }

                // Throttle commitment (% at full throttle over buffer)
                if (_dataBuffer.Count > 0)
                {
                    int fullThrottleCount = _dataBuffer.Count(d => d.ThrottlePosition >= 95f);
                    derived.ThrottleCommitment = (fullThrottleCount / (float)_dataBuffer.Count) * 100f;
                }
            }
        }

        /// <summary>
        /// Calculate steering metrics
        /// </summary>
        private void CalculateSteeringMetrics(
            ComprehensiveTelemetryData current,
            ComprehensiveTelemetryData? previous,
            DerivedChannels derived)
        {
            if (current.SteeringAngle.HasValue && previous?.SteeringAngle.HasValue == true)
            {
                float dt = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                if (dt > 0)
                {
                    derived.SteeringRate = (current.SteeringAngle.Value - previous.SteeringAngle.Value) / dt;
                }
            }

            // Understeer/oversteer estimation (very simplified)
            if (current.SteeringAngle.HasValue && derived.YawRate.HasValue && current.GpsSpeed.HasValue)
            {
                float speed = current.GpsSpeed.Value / KMH_TO_MS;  // Convert to m/s
                if (speed > 10)
                {
                    // Expected yaw rate based on steering angle and speed
                    float expectedYawRate = (speed / _wheelbase) *
                        (float)Math.Tan(current.SteeringAngle.Value * Math.PI / 180f);

                    float yawRateDiff = derived.YawRate.Value - expectedYawRate;

                    if (yawRateDiff < 0)
                        derived.UnderSteerAngle = Math.Abs(yawRateDiff);  // Understeer
                    else
                        derived.OverSteerAngle = yawRateDiff;  // Oversteer
                }
            }
        }

        /// <summary>
        /// Determine corner phase (entry, apex, exit, straight)
        /// </summary>
        private void CalculateCornerPhase(ComprehensiveTelemetryData current, DerivedChannels derived)
        {
            if (derived.LateralG.HasValue)
            {
                float absLatG = Math.Abs(derived.LateralG.Value);

                if (absLatG < 0.3f)
                {
                    derived.CornerPhase = "Straight";
                }
                else
                {
                    // Determine if entering, at apex, or exiting based on G trend
                    if (_dataBuffer.Count >= 3)
                    {
                        var recentData = _dataBuffer.TakeLast(3).ToList();
                        var recentGs = recentData
                            .Where(d => d.AccelY.HasValue)
                            .Select(d => Math.Abs(d.AccelY.Value / GRAVITY))
                            .ToList();

                        if (recentGs.Count >= 3)
                        {
                            bool increasing = recentGs[1] < recentGs[2];
                            bool decreasing = recentGs[1] > recentGs[2];

                            if (increasing)
                                derived.CornerPhase = "Entry";
                            else if (decreasing)
                                derived.CornerPhase = "Exit";
                            else
                                derived.CornerPhase = "Apex";
                        }
                    }
                }

                derived.CornerGForce = absLatG;
            }

            // Corner speed
            if (current.GpsSpeed.HasValue)
            {
                derived.CornerSpeed = current.GpsSpeed.Value;
            }
        }

        /// <summary>
        /// Calculate consistency metrics across buffer
        /// </summary>
        private void CalculateConsistencyMetrics(DerivedChannels derived)
        {
            if (_dataBuffer.Count < 10)
                return;

            // Calculate standard deviations for various metrics
            var brakingPoints = _dataBuffer
                .Where(d => d.BrakePosition.HasValue && d.BrakePosition.Value > 80f)
                .Select(d => d.GpsSpeed ?? 0)
                .ToList();

            if (brakingPoints.Any())
            {
                float avgBrakingSpeed = brakingPoints.Average();
                float stdDev = CalculateStdDev(brakingPoints, avgBrakingSpeed);
                // Convert to consistency score (0-100, where 100 is perfect)
                derived.BrakingConsistency = Math.Max(0, 100f - (stdDev * 2f));
            }

            // Throttle consistency
            var throttleApplications = _dataBuffer
                .Where(d => d.ThrottlePosition.HasValue && d.ThrottlePosition.Value > 90f)
                .Count();

            if (_dataBuffer.Count > 0)
            {
                derived.ThrottleConsistency = (throttleApplications / (float)_dataBuffer.Count) * 100f;
            }
        }

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        private float CalculateStdDev(List<float> values, float mean)
        {
            if (values.Count < 2)
                return 0;

            float sumSquaredDiffs = values.Sum(v => (float)Math.Pow(v - mean, 2));
            return (float)Math.Sqrt(sumSquaredDiffs / values.Count);
        }

        /// <summary>
        /// Configure vehicle parameters
        /// </summary>
        public void ConfigureVehicle(
            float mass,
            float fuelCapacity,
            float wheelbase,
            float trackWidth,
            float frontalArea,
            float dragCoefficient)
        {
            _vehicleMass = mass;
            _fuelTankCapacity = fuelCapacity;
            _wheelbase = wheelbase;
            _trackWidth = trackWidth;
            _frontalArea = frontalArea;
            _dragCoefficient = dragCoefficient;
        }

        /// <summary>
        /// Clear the data buffer
        /// </summary>
        public void ClearBuffer()
        {
            _dataBuffer.Clear();
        }
    }
}
