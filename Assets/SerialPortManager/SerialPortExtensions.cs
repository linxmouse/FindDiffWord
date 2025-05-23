﻿using UnityEngine;

public static class SerialPortExtensions
{
    // 定义唯一前缀标识
    public const string LogPrefix = "[SerialPort] ";

    public static void LogInfo(this SerialPortManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.Log(formattedMessage);
    }

    public static void LogWarning(this SerialPortManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.LogWarning(formattedMessage);
    }

    public static void LogError(this SerialPortManager manager, string message)
    {
        string formattedMessage = $"{LogPrefix}{message}";
        Debug.LogError(formattedMessage);
    }
}
