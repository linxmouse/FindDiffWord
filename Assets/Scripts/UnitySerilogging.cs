using System.IO;
using Serilog;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

/// <summary>
/// 此脚本不需要挂载在任何GameObject上
/// 在编辑器和运行时都能拦截日志并写入文件
/// </summary>
#if UNITY_EDITOR
// 确保脚本加载或域重载后立刻触发静态构造
[InitializeOnLoad]      
#endif
public static class UnitySerilogging/* : MonoBehaviour*/
{
    private static bool _initialized = false;
    private static readonly object _initLock = new object();
    /// <summary>
    /// 检查日志系统是否初始化
    /// </summary>
    public static bool IsInitialized => _initialized;

    static UnitySerilogging() 
    {
        // 编辑器环境下：脚本编译完毕、打开项目、域重载后都会调用这里
        InitializeLogging();
#if UNITY_EDITOR
        // 监听从编辑模式到播放模式、播放回编辑模式的切换
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    /// <summary>
    /// RuntimeInitializeOnLoadMethod 确保在游戏运行时调用静态构造函数
    /// RuntimeInitializeLoadType.BeforeSceneLoad 在任何场景加载前完成调用静态构造函数
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnRuntime()
    {
        if (!_initialized)
        {
            InitializeLogging();
            // 在运行时注册应用程序退出事件
            Application.quitting += OnApplicationQuitting;
        }
    }

    private static void OnApplicationQuitting()
    {
        // 清理旧的日志器
        CleanupLogging();
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
            case PlayModeStateChange.EnteredEditMode:
                InitializeLogging();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                CleanupLogging();
                break;
        }
    }

    /// <summary>
    /// 打包后进行清理
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pathToBuiltProject"></param>
    [PostProcessBuild(1)]
    private static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // 清理旧的日志器
        CleanupLogging();
    }
#endif

    private static void InitializeLogging()
    {
        lock (_initLock)
        {
            if (_initialized) return;
            // 基础路径配置
            string basePath = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) : Application.persistentDataPath;
            // 创建日志目录
            string logsFolder = Path.Combine(basePath, "Logs");
            if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);

            Log.Logger = new LoggerConfiguration()
#if UNITY_EDITOR
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Unity", Serilog.Events.LogEventLevel.Debug)       // 过滤Unity的日志
            .MinimumLevel.Override("UnityEngine", Serilog.Events.LogEventLevel.Debug) // 过滤UnityEngine的日志
            .MinimumLevel.Override("UnityEditor", Serilog.Events.LogEventLevel.Debug) // 过滤UnityEditor的日志  
#else
            .MinimumLevel.Warning()
            .MinimumLevel.Override("Unity", Serilog.Events.LogEventLevel.Warning)       // 过滤Unity的日志
            .MinimumLevel.Override("UnityEngine", Serilog.Events.LogEventLevel.Warning) // 过滤UnityEngine的日志
            .MinimumLevel.Override("UnityEditor", Serilog.Events.LogEventLevel.Warning) // 过滤UnityEditor的日志  
#endif
            .WriteTo.File($"{logsFolder}/log-.txt", 
            rollingInterval: RollingInterval.Day, 
            rollOnFileSizeLimit: true, 
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 15, // 保留15天的日志文件
            outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}") // 日志文件路径和滚动设置
            .CreateLogger();

            Application.logMessageReceived += HandleUnityLog; // 订阅Unity日志回调
            _initialized = true;
        }
    }

    private static void HandleUnityLog(string message, string stackTrace, LogType type)
    {
        var severity = type switch
        {
            LogType.Assert => Serilog.Events.LogEventLevel.Debug,
            LogType.Log => Serilog.Events.LogEventLevel.Information,
            LogType.Warning => Serilog.Events.LogEventLevel.Warning,
            LogType.Error => Serilog.Events.LogEventLevel.Error,
            LogType.Exception => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Debug,
        };
        Log.Write(severity, "{message}\n{stackTrace}", message, stackTrace);
    }

    private static void CleanupLogging()
    {
        lock (_initLock)
        {
            if (!_initialized) return;
            // 取消订阅Unity日志回调
            Application.logMessageReceived -= HandleUnityLog;
            Application.quitting -= OnApplicationQuitting;
            // 关闭并刷新Serilog日志
            Log.CloseAndFlush();
            // 重置标志
            _initialized = false;
        }
    }

    /// <summary>
    /// 获取当前日志文件路径
    /// </summary>
    /// <returns>日志文件路径，如果未初始化则返回null</returns>
    public static string GetLogDirectory()
    {
        if (!_initialized) return null;
        string basePath = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) : Application.persistentDataPath;
        return Path.Combine(basePath, "Logs");
    }

    /// <summary>
    /// 手动刷新日志缓冲区
    /// </summary>
    public static void FlushLogs() { if (_initialized) Log.CloseAndFlush(); }
}
