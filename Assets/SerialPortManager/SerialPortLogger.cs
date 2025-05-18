using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SerialPortLogger
{
    private static string _logFilePath;
    private static readonly object FileLock = new object();
    private static bool _initialized = false;
    private static int _currentDay = -1;
    private static string _logDirectory = Path.Combine(Application.persistentDataPath, "SerialPortLogs");

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    /// <param name="logDirectory">日志文件目录</param>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        // 创建日志目录
        if (!Directory.Exists(_logDirectory))
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception e)
            {
                Debug.LogError($"创建日志目录失败: {e.Message}");
                return;
            }
        }

        // 设置当前日志文件
        UpdateLogFile();
        // 订阅Unity日志回调
        Application.logMessageReceived += HandleUnityLog;
        Debug.Log("SerialPortLogger订阅Unity日志回调");
    }

    public static void UInitialize()
    {
        if (!_initialized) return;
        _initialized = false;
        // 取消订阅Unity日志回调
        Application.logMessageReceived -= HandleUnityLog;
        Debug.Log("SerialPortLogger取消订阅Unity日志回调");
    }

    /// <summary>
    /// 更新日志文件路径（日期滚动）
    /// </summary>
    private static void UpdateLogFile()
    {
        int today = DateTime.Now.Day;
        // 如果日期变化，更新日志文件
        if (_currentDay != today)
        {
            _currentDay = today;
            string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
            _logFilePath = Path.Combine(_logDirectory, fileName);
        }
    }

    /// <summary>
    /// 处理Unity日志消息
    /// </summary>
    private static void HandleUnityLog(string message, string stackTrace, LogType type)
    {
        // 只处理带有[SerialPort]标签的日志
        if (!message.StartsWith(SerialPortExtensions.LogPrefix)) return;
        // 去除前缀
        message = message.Substring(SerialPortExtensions.LogPrefix.Length);
        // 确保日志文件是最新的
        UpdateLogFile();
        // 构建日志消息
        StringBuilder logBuilder = new StringBuilder();
        logBuilder.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] {message}");
        // 对于错误和异常，添加堆栈跟踪
        if (type == LogType.Error || type == LogType.Exception)
        {
            logBuilder.AppendLine();
            logBuilder.Append(stackTrace);
        }
        // 写入文件
        lock (FileLock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logBuilder.ToString() + Environment.NewLine);
            }
            catch (Exception e)
            {
                // 在这里我们无法使用Debug.Log，否则会导致递归调用
                Console.WriteLine($"写入日志文件失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 记录普通日志
    /// </summary>
    public static void Log(string message)
    {
        if (!_initialized) Initialize();
        UpdateLogFile();
        Debug.Log(message);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void LogWarning(string message)
    {
        if (!_initialized) Initialize();
        Debug.LogWarning(message);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void LogError(string message)
    {
        if (!_initialized) Initialize();
        UpdateLogFile();
        Debug.LogError(message);
    }

    /// <summary>
    /// 记录异常日志
    /// </summary>
    public static void LogException(Exception exception)
    {
        if (!_initialized) Initialize();
        UpdateLogFile();
        Debug.LogException(exception);
    }

    /// <summary>
    /// 记录自定义格式日志（如十六进制数据）
    /// </summary>
    public static void LogHex(string prefix, byte[] data, int length = -1)
    {
        if (data == null) return;

        int actualLength = length < 0 ? data.Length : Math.Min(length, data.Length);
        string hexString = BitConverter.ToString(data, 0, actualLength).Replace("-", " ");
        Log($"{prefix}: {hexString}");
    }
}
