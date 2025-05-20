using Cysharp.Threading.Tasks;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class HidDeviceManager : MonoBehaviour
{
    private static HidDeviceManager _instance;
    public static HidDeviceManager Instance => _instance;

    private HidStream _hidStream;
    private Thread _readThread;
    private CancellationTokenSource _reconnectCts;  // 用于取消自动重连任务

    [Header("HID Device Settings")]
    [SerializeField]
    private bool _useHexFormat = true;              // 使用十六进制格式显示数据
    [SerializeField]
    private string _vendorIdHex = "1234";           // 设备的供应商ID
    [SerializeField]
    private string _productIdHex = "5678";          // 设备的产品ID
    [SerializeField]
    private int _vendorIdDec;                       // 设备的供应商ID（十进制）
    [SerializeField]
    private int _productIdDec;                      // 设备的产品ID（十进制）
    [SerializeField]
    private bool _autoReconnect = false;             // 是否自动重连
    [SerializeField]
    private float _reconnectInterval = 5f;          // 重连延迟时间（秒）

    // 可用设备列表
    [HideInInspector]
    public List<HidDeviceInfo> availableDevices = new List<HidDeviceInfo>();
    [HideInInspector]
    public int selectedDeviceIndex = -1;

    // 属性
    public int VendorId => _useHexFormat ? Convert.ToInt32(_vendorIdHex, 16) : _vendorIdDec;
    public int ProductId => _useHexFormat ? Convert.ToInt32(_productIdHex, 16) : _productIdDec;
    public bool UseHexFormat => _useHexFormat;
    public bool IsConnected => _hidStream != null && _hidStream.CanRead;
    public string DeviceInfo => IsConnected ? $"Vendor ID: {VendorId}, Product ID: {ProductId}" : "Not connected";

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        // 刷新设备列表
        RefreshDeviceList();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 如果启用自动重连
        if (_autoReconnect) StartConnectionMonitoring();
    }

    public void RefreshDeviceList()
    {
        availableDevices.Clear();
        try
        {
            var deviceList = DeviceList.Local;
            var hidDevices = deviceList.GetHidDevices();
            foreach (var device in hidDevices)
            {
                availableDevices.Add(new HidDeviceInfo
                {
                    VendorId = device.VendorID,
                    ProductId = device.ProductID,
                    ProductName = device.GetProductName() ?? "未知设备",
                    Manufacturer = device.GetManufacturer() ?? "未知制造商"
                });
            }
            // 尝试找到匹配的设备
            selectedDeviceIndex = availableDevices.FindIndex(d => d.VendorId == VendorId && d.ProductId == ProductId);
            if (!IsConnected) CloseDevice();
            this.LogInfo($"发现 {availableDevices.Count} 个 HID 设备");
        }
        catch (Exception e) { this.LogError($"刷新设备列表失败: {e.Message}"); }
    }

    public void UpdateIdFormat(bool useHex)
    {
        _useHexFormat = useHex;
        try
        {
            if (useHex)
            {
                // 转换为16进制
                _vendorIdHex = VendorId.ToString("X4");
                _productIdHex = ProductId.ToString("X4");
            }
            else
            {
                // 转换为10进制
                _vendorIdDec = Convert.ToInt32(_vendorIdHex, 16);
                _productIdDec = Convert.ToInt32(_productIdHex, 16);
            }
        }
        catch (Exception e) { this.LogWarning($"Id格式转换失败: {e.Message}"); }
    }

    /// <summary>
    /// 更新设备设置
    /// </summary>
    public void UpdateDeviceSettings(string vendorId, string productId, bool useHex)
    {
        _useHexFormat = useHex;
        if (useHex)
        {
            _vendorIdHex = vendorId;
            _productIdHex = productId;
            // 同时更新十进制值
            try
            {
                _vendorIdDec = Convert.ToInt32(vendorId, 16);
                _productIdDec = Convert.ToInt32(productId, 16);
            }
            catch { }
        }
        else
        {
            try
            {
                _vendorIdDec = int.Parse(vendorId);
                _productIdDec = int.Parse(productId);
                // 同时更新十六进制值
                _vendorIdHex = _vendorIdDec.ToString("X");
                _productIdHex = _productIdDec.ToString("X");
            }
            catch (Exception e)
            {
                this.LogWarning($"设备ID解析失败: {e.Message}");
                return;
            }
        }
        // 断开当前连接并尝试重新连接
        CloseDevice();
    }

    /// <summary>
    /// 尝试连接到Hid设备
    /// </summary>
    /// <returns></returns>
    public bool TryConnectToDevice()
    {
        // 确保先关闭之前的连接
        CloseDevice();
        try
        {
            var deviceList = DeviceList.Local;
            var device = deviceList.GetHidDevices()
                .FirstOrDefault(d => d.VendorID == VendorId && d.ProductID == ProductId);
            if (device == null)
            {
                this.LogWarning($"未找到设备: VID={VendorId:X4}, PID={ProductId:X4}");
                return false;
            }

            _hidStream = device.Open();
            if (_hidStream == null)
            {
                this.LogWarning("无法打开HID设备流");
                return false;
            }

            // 启动读取线程
            _readThread = new Thread(ReadDeviceData) { IsBackground = true };
            _readThread.Start();

            this.LogInfo($"HID设备连接成功: VID={VendorId:X4}, PID={ProductId:X4}");
            return true;
        }
        catch (Exception e)
        {
            this.LogError($"HID设备连接失败: {e.Message}");
            // 失败清理资源
            CloseDevice();
            return false;
        }
    }

    /// <summary>
    /// 关闭设备连接
    /// </summary>
    public void CloseDevice()
    {
        // 关闭HidStream
        try
        {
            _hidStream?.Close();
            _hidStream?.Dispose();
        }
        catch (Exception e) { this.LogWarning($"关闭HID流时发生错误: {e.Message}"); }
        finally { _hidStream = null; }
        DeviceList.Local.Shutdown();
        // 首先停止读取线程
        if (_readThread != null && _readThread.IsAlive)
        {
            // 等待线程自行终止
            if (!_readThread.Join(2000))
            {
                this.LogWarning("读取线程未能在指定时间内终止");
                // 在生产环境中不应使用Abort，这里仅作为最后手段
                try { _readThread.Abort(); }
                catch (Exception e) { this.LogError($"终止读取线程失败: {e.Message}"); }
            }
            _readThread = null;
        }
        this.LogInfo("HID设备已关闭");
    }

    private void ReadDeviceData()
    {
        if (_hidStream == null) return;
        byte[] buffer = new byte[_hidStream.Device.GetMaxInputReportLength()];

        while (IsConnected)
        {
            try
            {
                // 确保_hidStream仍然有效
                if (_hidStream == null || !_hidStream.CanRead) break;

                int bytesRead = _hidStream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    byte[] data = buffer.Take(bytesRead).ToArray();
                    string hexString = string.Join(" ", data.Select(b => b.ToString("X2")));
                    ProcessReceivedData(data, hexString);
                }
            }
            // 正常的超时异常，继续尝试读取
            catch (TimeoutException) { }
            // 流已被释放，退出循环
            catch (ObjectDisposedException) { break; }
            catch (Exception e)
            {
                // 只有当线程处于运行状态才记录错误
                if (IsConnected) { this.LogError($"HID设备读取失败: {e.Message}"); }
                // 发生错误时退出循环
                break;
            }

            // 短暂休眠以避免CPU使用率过高
            Thread.Sleep(5);
        }

        // 线程结束时，确保HidStream已关闭
        if (_hidStream != null)
        {
            // 如果线程是因为错误而退出
            this.LogWarning("读取线程异常退出，关闭HID连接");
            // 清理资源
            CloseDevice();
        }
    }

    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    private void ProcessReceivedData(byte[] data, string hexString)
    {
        if (this == null) return; // 确保组件仍然存在

        // 切换到主线程记录和处理数据
        UniTask.RunOnThreadPool(async () =>
        {
            try
            {
                await UniTask.SwitchToMainThread();
                if (this == null) return; // 再次检查组件是否存在

                this.LogInfo($"接收到数据: {hexString}");
                // 在这里添加自定义的数据处理逻辑
                // 例如: OnDataReceived?.Invoke(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"处理HID数据失败: {e.Message}");
            }
        }).Forget();
    }

    /// <summary>
    /// 发送数据到HID设备
    /// </summary>
    public bool SendData(byte[] data)
    {
        if (!IsConnected)
        {
            this.LogWarning("无法发送数据，HID设备未连接");
            return false;
        }

        try
        {
            if (_hidStream == null || !_hidStream.CanWrite)
            {
                this.LogWarning("HID流无法写入");
                return false;
            }

            _hidStream.Write(data, 0, data.Length);
            string hexString = string.Join(" ", data.Select(b => b.ToString("X2")));

            // 使用UniTask切换到主线程记录信息
            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    await UniTask.SwitchToMainThread();
                    if (this == null) return; // 检查组件是否仍然存在

                    this.LogInfo($"发送数据: {hexString}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"记录HID发送数据失败: {e.Message}");
                }
            }).Forget();

            return true;
        }
        catch (Exception e)
        {
            this.LogError($"HID设备发送失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送十六进制字符串
    /// </summary>
    /// <param name="hexString">格式如: "AA BB CC DD"</param>
    public bool SendHexString(string hexString)
    {
        try
        {
            // 移除所有空格，然后每两个字符转换成一个字节
            hexString = hexString.Replace(" ", "");
            if (hexString.Length % 2 != 0)
            {
                this.LogWarning("十六进制字符串格式错误: 长度必须为偶数");
                return false;
            }
            byte[] data = new byte[hexString.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return SendData(data);
        }
        catch (Exception e)
        {
            this.LogWarning($"解析十六进制字符串失败: {e.Message}");
            return false;
        }
    }

    private async UniTask SendAsync(byte[] data)
    {
        try
        {
            if (_hidStream == null || !_hidStream.CanWrite)
                throw new InvalidOperationException("HID流不可用或无法写入");

            await UniTask.RunOnThreadPool(() => _hidStream.Write(data, 0, data.Length));
        }
        catch (Exception e)
        {
            this.LogError($"异步发送数据失败: {e.Message}");
            throw;
        }
    }

    private async UniTask<byte[]> ReadAsync()
    {
        try
        {
            if (_hidStream == null || !_hidStream.CanRead)
                throw new InvalidOperationException("HID流不可用或无法读取");

            byte[] buffer = new byte[_hidStream.Device.GetMaxInputReportLength()];
            int bytesRead = 0;

            await UniTask.RunOnThreadPool(() => bytesRead = _hidStream.Read(buffer, 0, buffer.Length));
            return buffer.Take(bytesRead).ToArray();
        }
        catch (Exception e)
        {
            this.LogError($"异步读取数据失败: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 启动连接监控 (自动重连)
    /// </summary>
    private void StartConnectionMonitoring()
    {
        // 取消之前的重连任务（如果有）
        StopConnectionMonitoring();
        // 创建新的取消令牌源
        _reconnectCts = new CancellationTokenSource();
        // 启动监控任务
        UniTask.Void(async () =>
        {
            try
            {
                CancellationToken ct = _reconnectCts.Token;
                while (!ct.IsCancellationRequested && this != null && _autoReconnect)
                {
                    if (!IsConnected)
                    {
                        this.LogInfo("检测到连接断开，尝试重新连接...");
                        TryConnectToDevice();
                    }

                    try { await UniTask.Delay(TimeSpan.FromSeconds(_reconnectInterval), cancellationToken: ct); }
                    // 任务被取消
                    catch (OperationCanceledException) { break; }
                }
            }
            catch (Exception e)
            {
                if (this != null)
                {
                    this.LogError($"连接监控任务异常: {e.Message}");
                }
            }
        });
    }

    /// <summary>
    /// 停止连接监控
    /// </summary>
    private void StopConnectionMonitoring()
    {
        if (_reconnectCts != null)
        {
            try
            {
                _reconnectCts.Cancel();
                _reconnectCts.Dispose();
            }
            catch (Exception e) { this.LogWarning($"取消重连任务时出错: {e.Message}"); }
            finally { _reconnectCts = null; }
        }
    }

    private void OnDisable()
    {
        // 当组件被禁用时停止重连
        StopConnectionMonitoring();
    }

    private void OnDestroy()
    {
        // 停止自动重连
        StopConnectionMonitoring();
        // 关闭设备连接
        CloseDevice();
        _instance = null;
    }

    private void OnApplicationQuit()
    {
        // 应用退出时清理资源
        StopConnectionMonitoring();
        CloseDevice();
        _instance = null;
    }
}

/// <summary>
/// Hid设备信息
/// </summary>
[Serializable]
public class HidDeviceInfo
{
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string Manufacturer { get; set; }

    public override string ToString() => $"Vendor ID: {VendorId:X4}, Product ID: {ProductId:X4}, Name: {ProductName}, Manufacturer: {Manufacturer}";
}
