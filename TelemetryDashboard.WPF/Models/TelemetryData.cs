using System;
using System.ComponentModel.DataAnnotations;

namespace TelemetryDashboard.Models
{
    /// <summary>
    /// Represents a telemetry data packet
    /// </summary>
    public class TelemetryData
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public float Temperature { get; set; }

        public float Pressure { get; set; }

        public float Altitude { get; set; }

        public float Velocity { get; set; }

        public float Acceleration { get; set; }

        public TelemetryData()
        {
            Timestamp = DateTime.Now;
        }

        public TelemetryData(float temperature, float pressure, float altitude,
                           float velocity, float acceleration)
        {
            Timestamp = DateTime.Now;
            Temperature = temperature;
            Pressure = pressure;
            Altitude = altitude;
            Velocity = velocity;
            Acceleration = acceleration;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] Temp: {Temperature:F2}°C, " +
                   $"Press: {Pressure:F2} kPa, Alt: {Altitude:F2}m, " +
                   $"Vel: {Velocity:F2} m/s, Accel: {Acceleration:F2} m/s²";
        }
    }
}
