"""
Telemetry Service - Serial Port Reader
Reads and decodes telemetry data from a serial port.
"""
import serial
import json
import struct
import time
from datetime import datetime
from typing import Dict, Any, Optional
import threading


class TelemetryData:
    """Represents a telemetry data packet"""

    def __init__(self, timestamp: str, temperature: float, pressure: float,
                 altitude: float, velocity: float, acceleration: float):
        self.timestamp = timestamp
        self.temperature = temperature
        self.pressure = pressure
        self.altitude = altitude
        self.velocity = velocity
        self.acceleration = acceleration

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        return {
            'timestamp': self.timestamp,
            'temperature': self.temperature,
            'pressure': self.pressure,
            'altitude': self.altitude,
            'velocity': self.velocity,
            'acceleration': self.acceleration
        }


class TelemetryService:
    """Handles serial port communication and telemetry data decoding"""

    def __init__(self, port: str = 'COM3', baudrate: int = 115200):
        """
        Initialize telemetry service

        Args:
            port: Serial port name (e.g., 'COM3' on Windows, '/dev/ttyUSB0' on Linux)
            baudrate: Serial communication speed
        """
        self.port = port
        self.baudrate = baudrate
        self.serial_connection: Optional[serial.Serial] = None
        self.is_running = False
        self.data_callback = None
        self.thread: Optional[threading.Thread] = None

    def connect(self) -> bool:
        """
        Connect to the serial port

        Returns:
            True if connection successful, False otherwise
        """
        try:
            self.serial_connection = serial.Serial(
                port=self.port,
                baudrate=self.baudrate,
                timeout=1.0,
                bytesize=serial.EIGHTBITS,
                parity=serial.PARITY_NONE,
                stopbits=serial.STOPBITS_ONE
            )
            print(f"Connected to {self.port} at {self.baudrate} baud")
            return True
        except serial.SerialException as e:
            print(f"Failed to connect: {e}")
            return False

    def disconnect(self):
        """Disconnect from the serial port"""
        self.stop_reading()
        if self.serial_connection and self.serial_connection.is_open:
            self.serial_connection.close()
            print("Disconnected from serial port")

    def decode_telemetry_packet(self, data: bytes) -> Optional[TelemetryData]:
        """
        Decode binary telemetry packet

        Packet format (24 bytes):
        - Temperature: float (4 bytes)
        - Pressure: float (4 bytes)
        - Altitude: float (4 bytes)
        - Velocity: float (4 bytes)
        - Acceleration: float (4 bytes)
        - Checksum: uint32 (4 bytes)

        Args:
            data: Raw binary data

        Returns:
            TelemetryData object or None if decoding fails
        """
        try:
            if len(data) < 24:
                return None

            # Unpack binary data (little-endian format)
            values = struct.unpack('<ffffI', data[:24])
            temperature = values[0]
            pressure = values[1]
            altitude = values[2]
            velocity = values[3]
            acceleration = values[4]
            checksum = values[5]

            # Simple checksum verification (sum of first 20 bytes)
            calculated_checksum = sum(data[:20]) & 0xFFFFFFFF
            if calculated_checksum != checksum:
                print("Checksum mismatch!")
                return None

            timestamp = datetime.now().isoformat()

            return TelemetryData(
                timestamp=timestamp,
                temperature=temperature,
                pressure=pressure,
                altitude=altitude,
                velocity=velocity,
                acceleration=acceleration
            )
        except struct.error as e:
            print(f"Error decoding packet: {e}")
            return None

    def read_line_ascii(self) -> Optional[TelemetryData]:
        """
        Read ASCII formatted telemetry data (alternative to binary)

        Expected format: "TEMP:25.5,PRESS:101.3,ALT:1234.5,VEL:100.2,ACCEL:9.8"

        Returns:
            TelemetryData object or None if parsing fails
        """
        try:
            if not self.serial_connection or not self.serial_connection.is_open:
                return None

            line = self.serial_connection.readline().decode('utf-8').strip()
            if not line:
                return None

            # Parse key-value pairs
            data_dict = {}
            for pair in line.split(','):
                if ':' in pair:
                    key, value = pair.split(':')
                    data_dict[key.strip()] = float(value.strip())

            timestamp = datetime.now().isoformat()

            return TelemetryData(
                timestamp=timestamp,
                temperature=data_dict.get('TEMP', 0.0),
                pressure=data_dict.get('PRESS', 0.0),
                altitude=data_dict.get('ALT', 0.0),
                velocity=data_dict.get('VEL', 0.0),
                acceleration=data_dict.get('ACCEL', 0.0)
            )
        except Exception as e:
            print(f"Error reading ASCII data: {e}")
            return None

    def read_binary_packet(self) -> Optional[TelemetryData]:
        """
        Read binary telemetry packet from serial port

        Returns:
            TelemetryData object or None if reading fails
        """
        try:
            if not self.serial_connection or not self.serial_connection.is_open:
                return None

            # Look for start marker (0xFF 0xAA)
            while True:
                byte1 = self.serial_connection.read(1)
                if not byte1 or byte1[0] != 0xFF:
                    continue

                byte2 = self.serial_connection.read(1)
                if not byte2 or byte2[0] != 0xAA:
                    continue

                # Found start marker, read packet
                packet_data = self.serial_connection.read(24)
                if len(packet_data) == 24:
                    return self.decode_telemetry_packet(packet_data)
                break

            return None
        except Exception as e:
            print(f"Error reading binary packet: {e}")
            return None

    def start_reading(self, callback, use_ascii: bool = True):
        """
        Start reading telemetry data in a background thread

        Args:
            callback: Function to call with each TelemetryData object
            use_ascii: If True, read ASCII format; if False, read binary format
        """
        self.data_callback = callback
        self.is_running = True

        def read_loop():
            while self.is_running:
                try:
                    if use_ascii:
                        telemetry = self.read_line_ascii()
                    else:
                        telemetry = self.read_binary_packet()

                    if telemetry and self.data_callback:
                        self.data_callback(telemetry)

                    time.sleep(0.01)  # Small delay to prevent CPU spinning
                except Exception as e:
                    print(f"Error in read loop: {e}")
                    time.sleep(1)

        self.thread = threading.Thread(target=read_loop, daemon=True)
        self.thread.start()
        print("Started telemetry reading thread")

    def stop_reading(self):
        """Stop the background reading thread"""
        self.is_running = False
        if self.thread:
            self.thread.join(timeout=2.0)
            print("Stopped telemetry reading thread")


def main():
    """Example usage"""
    def on_telemetry_received(telemetry: TelemetryData):
        print(f"Received: {json.dumps(telemetry.to_dict(), indent=2)}")

    service = TelemetryService(port='COM3', baudrate=115200)

    if service.connect():
        service.start_reading(on_telemetry_received, use_ascii=True)

        try:
            # Keep running until Ctrl+C
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("\nShutting down...")
        finally:
            service.disconnect()


if __name__ == '__main__':
    main()
