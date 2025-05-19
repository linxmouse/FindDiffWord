using System;
using System.Runtime.InteropServices;

namespace SerialPortSharp
{
    /// <summary>
    /// This class contains the native methods for serial port communication.
    /// </summary>
    /// <remarks>
    /// The methods in this class are used to interact with the serial port at a low level.
    /// </remarks>
    public class NativeMethods
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string LibraryName = "libserialport.dll";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        private const string LibraryName = "libserialport.so";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string LibraryName = "libserialport.dylib";
#elif UNITY_ANDROID
        private const string LibraryName = "libserialport";
#else
        throw new PlatformNotSupportedException();
#endif

        /* Port Operations */
        [DllImport(LibraryName)]
        public static extern IntPtr sp_get_port_by_name(string portName);

        [DllImport(LibraryName)]
        public static extern int sp_open(IntPtr port, int flags);

        [DllImport(LibraryName)]
        public static extern int sp_close(IntPtr port);

        [DllImport(LibraryName)]
        public static extern void sp_free_port(IntPtr port);

        /* Configuration */
        [DllImport(LibraryName)]
        public static extern int sp_set_baudrate(IntPtr port, int baudrate);

        [DllImport(LibraryName)]
        public static extern int sp_set_bits(IntPtr port, int bits);

        [DllImport(LibraryName)]
        public static extern int sp_set_parity(IntPtr port, int parity);

        [DllImport(LibraryName)]
        public static extern int sp_set_stopbits(IntPtr port, int stopbits);

        /* Data Transfer */
        [DllImport(LibraryName)]
        public static extern int sp_blocking_write(IntPtr port, byte[] data, int len);

        [DllImport(LibraryName)]
        public static extern int sp_blocking_read(IntPtr port, byte[] data, int len, int timeoutMs);

        /* Port List */
        [DllImport(LibraryName)]
        public static extern int sp_list_ports(out IntPtr portList);

        [DllImport(LibraryName)]
        public static extern void sp_free_port_list(IntPtr portList);

        [StructLayout(LayoutKind.Sequential)]
        public struct sp_port
        {
            public IntPtr name;
            public IntPtr description;
            public IntPtr transport;
        }
    }
}