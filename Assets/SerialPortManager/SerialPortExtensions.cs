using UnityEngine;

public static class SerialPortExtensions
{
    // 定义唯一前缀标识
    public const string LogPrefix = "[SerialPort] ";

    public static void LogInfo(this SerialPortManager spm, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        if (spm.EnableFileLogging) SerialPortLogger.Log(formattedMessage);
        else Debug.Log(formattedMessage);
    }

    public static void LogWarning(this SerialPortManager spm, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        if (spm.EnableFileLogging) SerialPortLogger.LogWarning(formattedMessage);
        else Debug.LogWarning(formattedMessage);
    }

    public static void LogError(this SerialPortManager spm, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        if (spm.EnableFileLogging) SerialPortLogger.LogError(formattedMessage);
        else Debug.LogError(formattedMessage);
    }

}
