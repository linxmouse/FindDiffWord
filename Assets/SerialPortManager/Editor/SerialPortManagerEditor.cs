using System;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(SerialPortManager))]
public class SerialPortManagerEditor : Editor
{
    private SerialPortManager _manager;
    private bool _showDebugSection = false;
    private string _debugHexString = "AA BB CC";
    private string _logPath = "";

    // 自定义文本样式
    private GUIStyle _greenTextStyle;
    private GUIStyle _redTextStyle;

    private void OnEnable()
    {
        _manager = (SerialPortManager)target;
        _logPath = System.IO.Path.Combine(Application.persistentDataPath, "SerialPortLogs");
    }

    private void CreateTextStyles()
    {
        // 绿色文本样式
        _greenTextStyle = new GUIStyle(EditorStyles.label);
        _greenTextStyle.normal.textColor = Color.green;
        _greenTextStyle.fontSize = 12;
        _greenTextStyle.fontStyle = FontStyle.Bold;

        // 红色文本样式
        _redTextStyle = new GUIStyle(EditorStyles.label);
        _redTextStyle.normal.textColor = Color.red;
        _redTextStyle.fontSize = 12;
        _redTextStyle.fontStyle = FontStyle.Bold;
    }

    public override void OnInspectorGUI()
    {
        // 初始化文本样式
        CreateTextStyles();

        serializedObject.Update();

        // 记录日志设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("日志设置", EditorStyles.boldLabel);
        SerializedProperty enableFileLoggingProp = serializedObject.FindProperty("_enableFileLogging");
        EditorGUILayout.PropertyField(enableFileLoggingProp, new GUIContent("启用文件日志"));

        // 刷新串口列表按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("串口设置", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新串口列表", GUILayout.Width(100)))
        {
            _manager.RefreshPortList();
        }
        EditorGUILayout.EndHorizontal();

        // 串口选择
        EditorGUI.BeginChangeCheck();
        // 串口下拉列表
        int currentPortIndex = _manager.selectedPortIndex;
        string[] portNames = _manager.availablePorts.ToArray();
        if (portNames.Length == 0)
        {
            EditorGUILayout.HelpBox("未找到可用串口", MessageType.Warning);
        }
        else
        {
            int newPortIndex = EditorGUILayout.Popup("串口", currentPortIndex, portNames);
            // 波特率下拉列表
            int[] baudRates = _manager.availableBaudRates;
            string[] baudRateStrings = new string[baudRates.Length];
            for (int i = 0; i < baudRates.Length; i++)
            {
                baudRateStrings[i] = baudRates[i].ToString();
            }
            int newBaudRateIndex = EditorGUILayout.Popup("波特率", _manager.selectedBaudRateIndex, baudRateStrings);
            // 如果设置发生变化
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_manager, "Change Serial Port Settings");
                // 更新串口设置
                _manager.UpdatePortSettings(newPortIndex, newBaudRateIndex);
                EditorUtility.SetDirty(_manager);
            }
        }

        // 显示连接状态
        GUI.enabled = true;
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (_manager.IsConnected)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"已连接到{_manager.PortName} {_manager.BaudRate}波特率", _greenTextStyle);
            EditorGUILayout.EndHorizontal();
            //EditorGUILayout.HelpBox($"已连接到{_manager.PortName}, {_manager.BaudRate}波特率", MessageType.None);
            if (GUILayout.Button("断开连接", GUILayout.Width(100)))
            {
                _manager.ClosePort();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("⚠ 未连接", _redTextStyle);
            EditorGUILayout.EndHorizontal();
            //EditorGUILayout.HelpBox("未连接", MessageType.None);
            if (GUILayout.Button("连接", GUILayout.Width(100)))
            {
                _manager.TryConnectToPort();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 调试区域
        EditorGUILayout.Space();
        _showDebugSection = EditorGUILayout.Foldout(_showDebugSection, "串口调试");
        if (_showDebugSection)
        {
            GUI.enabled = _manager.IsConnected;
            EditorGUILayout.LabelField("发送十六进制数据 (格式: AA BB CC):");
            _debugHexString = EditorGUILayout.TextField(_debugHexString);
            if (GUILayout.Button("发送"))
            {
                _manager.SendHexString(_debugHexString);
            }
            GUI.enabled = true;
        }

        // 打开日志文件夹
        EditorGUILayout.Space();
        if (GUILayout.Button("打开日志文件夹"))
        {
            OpenLogFolder();
        }

        // 显示应用状态描述
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label($"ℹ️ 配置调试完成拖入Hierarchy中使用", _greenTextStyle);
        EditorGUILayout.EndHorizontal();
        //EditorGUILayout.HelpBox("配置调试完成拖入Hierarchy中使用", MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    private void OpenLogFolder()
    {
        if (!System.IO.Directory.Exists(_logPath))
        {
            System.IO.Directory.CreateDirectory(_logPath);
        }
        // 打开日志文件夹
        EditorUtility.RevealInFinder(_logPath);
    }
}
#endif