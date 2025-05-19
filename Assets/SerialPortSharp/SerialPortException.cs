using System;

namespace SerialPortSharp
{
    public class SerialPortException : Exception
    {
        public SerialPortException(string message) : base(message) { }
        public SerialPortException(string message, int errorCode)
            : base($"{message} (Error Code: {errorCode})") { }

    }
}