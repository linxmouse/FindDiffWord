using UnityEngine;
using Unity.Logging;
using System;
using Unity.Logging.Sinks;
using System.IO;

/// <summary>
/// 此脚本不需要挂载在任何GameObject上
/// </summary>
public class InitializeUnityLogging : MonoBehaviour
{
    static InitializeUnityLogging() { }

    /// <summary>
    /// RuntimeInitializeOnLoadMethod 确保在游戏运行时调用静态构造函数
    /// RuntimeInitializeLoadType.BeforeSceneLoad 在任何场景加载前完成调用静态构造函数
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() 
    {
        // 基础路径配置
        string basePath = Application.isEditor ?
            Path.GetFullPath(Path.Combine(Application.dataPath, "..")) :
            Application.persistentDataPath;
        // 创建日志目录
        string logsFolder = Path.Combine(basePath, "Logs");
        if (!Directory.Exists(logsFolder))
            Directory.CreateDirectory(logsFolder);
        // 生成带时间戳的文件名
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HH_mm_ss");
        string logFileName = $"log-{timestamp}.txt";
        // 完成日志路径
        string fullLogPath = Path.Combine(logsFolder, logFileName);
        // 配置日志系统
        Log.Logger = new Unity.Logging.Logger(new LoggerConfig()
#if UNITY_EDITOR
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Warning()
#endif
            .OutputTemplate("[{Timestamp} {Level}] {Message}{NewLine}{Stacktrace}")
            .WriteTo.File(fullLogPath)
            .WriteTo.UnityEditorConsole(outputTemplate: "{Message}{NewLine}{Stacktrace}"));
    }
}
