using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SerialPortSharp
{
    public enum Parity { None = 0, Odd = 1, Even = 2 }
    public enum StopBits { One = 1, Two = 2 }

    public class LibSerialPort : IDisposable
    {
        private IntPtr _portHandle;
        private bool _isOpen;

        public string PortName { get; private set; }
        public int BaudRate { get; private set; } = 9600;
        public int DataBits { get; private set; } = 8;
        public Parity Parity { get; private set; } = Parity.None;
        public StopBits StopBits { get; private set; } = StopBits.One;

        public bool IsOpen => _isOpen;

        public LibSerialPort(string portName, int baudRate)
        {
            PortName = portName;
            BaudRate = baudRate;
            _portHandle = NativeMethods.sp_get_port_by_name(PortName);
            if (_portHandle == IntPtr.Zero) throw new SerialPortException($"Port {PortName} not found");
        }

        public void Open()
        {
            if (_isOpen) return;
            int result = NativeMethods.sp_open(_portHandle, 0);
            CheckResult(result, "Failed to open port");

            ConfigurePort();
            _isOpen = true;
        }

        private void ConfigurePort()
        {
            CheckResult(NativeMethods.sp_set_baudrate(_portHandle, BaudRate), "Set baudrate failed");
            CheckResult(NativeMethods.sp_set_bits(_portHandle, DataBits), "Set data bits failed");
            CheckResult(NativeMethods.sp_set_parity(_portHandle, (int)Parity), "Set parity failed");
            CheckResult(NativeMethods.sp_set_stopbits(_portHandle, (int)StopBits), "Set stop bits failed");
        }

        public void Write(byte[] data)
        {
            if (!_isOpen) throw new SerialPortException("Port is not open");

            int result = NativeMethods.sp_blocking_write(_portHandle, data, data.Length);
            CheckResult(result, "Write failed");
        }

        public byte[] Read(int length, int timeoutMs = 1000)
        {
            if (!_isOpen) throw new SerialPortException("Port is not open");

            byte[] buffer = new byte[length];
            int result = NativeMethods.sp_blocking_read(
                _portHandle, buffer, length, timeoutMs
            );

            CheckResult(result, "Read failed");
            return buffer;
        }

        public static List<string> GetAvailablePorts()
        {
            IntPtr portList;
            int result = NativeMethods.sp_list_ports(out portList);
            CheckResult(result, "Failed to list ports");

            var ports = new List<string>();
            IntPtr current = portList;

            while (current != IntPtr.Zero)
            {
                NativeMethods.sp_port port = Marshal.PtrToStructure<NativeMethods.sp_port>(current);
                ports.Add(Marshal.PtrToStringUTF8(port.name));
                current = Marshal.ReadIntPtr(current, IntPtr.Size * 3); // 结构体偏移量
            }

            NativeMethods.sp_free_port_list(portList);
            return ports;
        }

        public void Dispose()
        {
            if (_portHandle != IntPtr.Zero)
            {
                if (_isOpen) NativeMethods.sp_close(_portHandle);
                NativeMethods.sp_free_port(_portHandle);
                _portHandle = IntPtr.Zero;
                _isOpen = false;
            }
        }

        private static void CheckResult(int result, string message = null)
        {
            if (result != 0)
                throw new SerialPortException(message ?? "Operation failed", result);
        }
    }
}
