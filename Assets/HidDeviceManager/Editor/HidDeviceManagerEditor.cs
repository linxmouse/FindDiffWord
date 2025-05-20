using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR
[CustomEditor(typeof(HidDeviceManager))]
public class HIDDeviceManagerEditor : Editor
{
    private HidDeviceManager _manager;
    private bool _showDebugSection = false;
    private bool _showDeviceList = false;
    private string _debugHexString = "AA BB CC";
    private string _tempVendorId = "";
    private string _tempProductId = "";

    // 自定义文本样式
    private GUIStyle _greenTextStyle;
    private GUIStyle _redTextStyle;
    private GUIStyle _yellowTextStyle;

    private void OnEnable()
    {
        _manager = (HidDeviceManager)target;
        // 初始化临时ID值
        if (_manager.UseHexFormat)
        {
            _tempVendorId = _manager.VendorId.ToString("X");
            _tempProductId = _manager.ProductId.ToString("X");
        }
        else
        {
            _tempVendorId = _manager.VendorId.ToString();
            _tempProductId = _manager.ProductId.ToString();
        }
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

        // 黄色文本样式
        _yellowTextStyle = new GUIStyle(EditorStyles.label);
        _yellowTextStyle.normal.textColor = Color.yellow;
        _yellowTextStyle.fontSize = 12;
        _yellowTextStyle.fontStyle = FontStyle.Bold;
    }

    public override void OnInspectorGUI()
    {
        // 初始化文本样式
        CreateTextStyles();

        serializedObject.Update();

        // 设备ID配置区域
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("设备ID配置", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新设备列表", GUILayout.Width(100)))
        {
            _manager.RefreshDeviceList();
        }
        EditorGUILayout.EndHorizontal();
        // 格式切换
        EditorGUI.BeginChangeCheck();
        SerializedProperty useHexFormatProp = serializedObject.FindProperty("_useHexFormat");
        bool newUseHex = EditorGUILayout.Toggle("HEX", useHexFormatProp.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_manager, "Toggle HEX Format");
            useHexFormatProp.boolValue = newUseHex;
            _manager.UpdateIdFormat(newUseHex);
            // 更新临时ID值
            if (newUseHex)
            {
                _tempVendorId = _manager.VendorId.ToString("X");
                _tempProductId = _manager.ProductId.ToString("X");
            }
            else
            {
                _tempVendorId = _manager.VendorId.ToString();
                _tempProductId = _manager.ProductId.ToString();
            }
            EditorUtility.SetDirty(_manager);
        }
        // VID/PID 输入
        EditorGUI.BeginChangeCheck();
        if (_manager.UseHexFormat)
        {
            EditorGUILayout.BeginHorizontal();
            _tempVendorId = EditorGUILayout.TextField("Vendor ID (16进制)", _tempVendorId);
            GUILayout.Label($"(10进制: {GetDecimalValue(_tempVendorId)})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _tempProductId = EditorGUILayout.TextField("Product ID (16进制)", _tempProductId);
            GUILayout.Label($"(10进制: {GetDecimalValue(_tempProductId)})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            _tempVendorId = EditorGUILayout.TextField("Vendor ID (10进制)", _tempVendorId);
            GUILayout.Label($"(16进制: {GetHexValue(_tempVendorId)})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            _tempProductId = EditorGUILayout.TextField("Product ID (10进制)", _tempProductId);
            GUILayout.Label($"(16进制: {GetHexValue(_tempProductId)})", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
        // 自动重连设置
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoReconnect"), new GUIContent("自动重连"));
        if (serializedObject.FindProperty("_autoReconnect").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_reconnectInterval"), new GUIContent("重连间隔(秒)"));
        }
        // 连接状态显示
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (_manager.IsConnected)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"已连接: {_manager.DeviceInfo}", _greenTextStyle);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("断开连接", GUILayout.Width(100)))
            {
                _manager.CloseDevice();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("⚠ 未连接", _redTextStyle);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("连接", GUILayout.Width(100)))
            {
                _manager.TryConnectToDevice();
            }
        }
        EditorGUILayout.EndHorizontal();
        // 可用设备列表
        EditorGUILayout.Space();
        _showDeviceList = EditorGUILayout.Foldout(_showDeviceList, $"可用设备列表 ({_manager.availableDevices.Count})");
        if (_showDeviceList)
        {
            if (_manager.availableDevices.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到HID设备", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < _manager.availableDevices.Count; i++)
                {
                    var device = _manager.availableDevices[i];
                    EditorGUILayout.BeginHorizontal();
                    // 设备信息显示
                    bool isSelected = (device.VendorId == _manager.VendorId && device.ProductId == _manager.ProductId);
                    GUIStyle labelStyle = isSelected ? _greenTextStyle : EditorStyles.label;
                    EditorGUILayout.LabelField(device.ToString(), labelStyle);
                    // 选择按钮
                    if (GUILayout.Button("选择", GUILayout.Width(50)))
                    {
                        _tempVendorId = _manager.UseHexFormat ? device.VendorId.ToString("X") : device.VendorId.ToString();
                        _tempProductId = _manager.UseHexFormat ? device.ProductId.ToString("X") : device.ProductId.ToString();

                        Undo.RecordObject(_manager, "Select Device");
                        _manager.UpdateDeviceSettings(_tempVendorId, _tempProductId, _manager.UseHexFormat);
                        EditorUtility.SetDirty(_manager);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }
        // 调试区域
        EditorGUILayout.Space();
        _showDebugSection = EditorGUILayout.Foldout(_showDebugSection, "HID设备调试");
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
        // 日志文件夹
        EditorGUILayout.Space();
        if (GUILayout.Button("打开日志文件夹"))
        {
            OpenLogFolder();
        }
        // 状态提示
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label("ℹ️ 配置完成后拖入Hierarchy中使用", _greenTextStyle);
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private string GetDecimalValue(string hexString)
    {
        try { return Convert.ToInt32(hexString, 16).ToString(); }
        catch { return "无效"; }
    }

    private string GetHexValue(string decString)
    {
        try { return int.Parse(decString).ToString("X"); }
        catch { return "无效"; }
    }

    private void OpenLogFolder()
    {
        if (UnitySerilogging.GetLogDirectory() == null) return;
        // 打开日志文件夹
        EditorUtility.RevealInFinder(UnitySerilogging.GetLogDirectory());
    }
}
#endif