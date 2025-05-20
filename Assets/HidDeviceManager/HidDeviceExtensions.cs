using UnityEngine;

public static class HidDeviceExtensions
{
    // 定义唯一前缀标识
    public const string LogPrefix = "[HidDevice] ";

    public static void LogInfo(this HidDeviceManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.Log(formattedMessage);
    }

    public static void LogWarning(this HidDeviceManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.LogWarning(formattedMessage);
    }

    public static void LogError(this HidDeviceManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.LogError(formattedMessage);
    }
}
