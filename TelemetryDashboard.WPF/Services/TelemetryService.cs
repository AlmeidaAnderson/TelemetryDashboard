using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelemetryDashboard.Models;

namespace TelemetryDashboard.Services
{
    /// <summary>
    /// Handles serial port communication and telemetry data decoding
    /// </summary>
    public class TelemetryService : IDisposable
    {
        private SerialPort? _serialPort;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _readTask;
        private bool _isRunning;

        public event EventHandler<TelemetryData>? TelemetryReceived;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<string>? StatusChanged;

        public string PortName { get; private set; }
        public int BaudRate { get; private set; }
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public bool UseAsciiMode { get; set; } = true;

        public TelemetryService(string portName = "COM3", int baudRate = 115200)
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        /// <summary>
        /// Get available serial ports on the system
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Connect to the serial port
        /// </summary>
        public bool Connect()
        {
            try
            {
                _serialPort = new SerialPort
                {
                    PortName = PortName,
                    BaudRate = BaudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                StatusChanged?.Invoke(this, $"Connected to {PortName} at {BaudRate} baud");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Failed to connect: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the serial port
        /// </summary>
        public void Disconnect()
        {
            StopReading();

            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
                StatusChanged?.Invoke(this, "Disconnected from serial port");
            }

            _serialPort?.Dispose();
            _serialPort = null;
        }

        /// <summary>
        /// Start reading telemetry data in background
        /// </summary>
        public void StartReading()
        {
            if (_isRunning || _serialPort == null || !_serialPort.IsOpen)
                return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _readTask = Task.Run(() => ReadLoop(_cancellationTokenSource.Token));
            StatusChanged?.Invoke(this, "Started reading telemetry data");
        }

        /// <summary>
        /// Stop reading telemetry data
        /// </summary>
        public void StopReading()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // Task was cancelled
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _readTask = null;

            StatusChanged?.Invoke(this, "Stopped reading telemetry data");
        }

        /// <summary>
        /// Main reading loop
        /// </summary>
        private void ReadLoop(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TelemetryData? telemetry = UseAsciiMode
                        ? ReadAsciiData()
                        : ReadBinaryData();

                    if (telemetry != null)
                    {
                        TelemetryReceived?.Invoke(this, telemetry);
                    }

                    Thread.Sleep(10); // Small delay to prevent CPU spinning
                }
                catch (TimeoutException)
                {
                    // Normal timeout, continue
                }
                catch (Exception ex)
                {
                    if (_isRunning) // Only report errors if still running
                    {
                        ErrorOccurred?.Invoke(this, $"Read error: {ex.Message}");
                        Thread.Sleep(1000); // Wait before retrying
                    }
                }
            }
        }

        /// <summary>
        /// Read ASCII formatted telemetry data
        /// Expected format: "TEMP:25.5,PRESS:101.3,ALT:1234.5,VEL:100.2,ACCEL:9.8"
        /// </summary>
        private TelemetryData? ReadAsciiData()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return null;

            try
            {
                string line = _serialPort.ReadLine().Trim();
                if (string.IsNullOrEmpty(line))
                    return null;

                var data = new TelemetryData();
                var pairs = line.Split(',');

                foreach (var pair in pairs)
                {
                    var parts = pair.Split(':');
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    if (!float.TryParse(parts[1].Trim(), out float value))
                        continue;

                    switch (key.ToUpper())
                    {
                        case "TEMP":
                            data.Temperature = value;
                            break;
                        case "PRESS":
                            data.Pressure = value;
                            break;
                        case "ALT":
                            data.Altitude = value;
                            break;
                        case "VEL":
                            data.Velocity = value;
                            break;
                        case "ACCEL":
                            data.Acceleration = value;
                            break;
                    }
                }

                return data;
            }
            catch (TimeoutException)
            {
                throw; // Propagate timeout
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"ASCII parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Read binary telemetry data
        /// Packet format (24 bytes):
        /// - Temperature: float (4 bytes)
        /// - Pressure: float (4 bytes)
        /// - Altitude: float (4 bytes)
        /// - Velocity: float (4 bytes)
        /// - Acceleration: float (4 bytes)
        /// - Checksum: uint32 (4 bytes)
        /// Start marker: 0xFF 0xAA
        /// </summary>
        private TelemetryData? ReadBinaryData()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return null;

            try
            {
                // Look for start marker (0xFF 0xAA)
                while (true)
                {
                    int byte1 = _serialPort.ReadByte();
                    if (byte1 != 0xFF)
                        continue;

                    int byte2 = _serialPort.ReadByte();
                    if (byte2 != 0xAA)
                        continue;

                    // Found start marker, read packet (24 bytes)
                    byte[] buffer = new byte[24];
                    int bytesRead = _serialPort.Read(buffer, 0, 24);

                    if (bytesRead == 24)
                    {
                        return DecodeBinaryPacket(buffer);
                    }

                    break;
                }

                return null;
            }
            catch (TimeoutException)
            {
                throw; // Propagate timeout
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Binary read error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decode binary telemetry packet
        /// </summary>
        private TelemetryData? DecodeBinaryPacket(byte[] buffer)
        {
            try
            {
                if (buffer.Length < 24)
                    return null;

                float temperature = BitConverter.ToSingle(buffer, 0);
                float pressure = BitConverter.ToSingle(buffer, 4);
                float altitude = BitConverter.ToSingle(buffer, 8);
                float velocity = BitConverter.ToSingle(buffer, 12);
                float acceleration = BitConverter.ToSingle(buffer, 16);
                uint checksum = BitConverter.ToUInt32(buffer, 20);

                // Verify checksum (sum of first 20 bytes)
                uint calculatedChecksum = 0;
                for (int i = 0; i < 20; i++)
                {
                    calculatedChecksum += buffer[i];
                }

                if (calculatedChecksum != checksum)
                {
                    ErrorOccurred?.Invoke(this, "Checksum mismatch in binary packet");
                    return null;
                }

                return new TelemetryData(temperature, pressure, altitude, velocity, acceleration);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Binary decode error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Send command to device
        /// </summary>
        public bool SendCommand(string command)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return false;

            try
            {
                _serialPort.WriteLine(command);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Send error: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
