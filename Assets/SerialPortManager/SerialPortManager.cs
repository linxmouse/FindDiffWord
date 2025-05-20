using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
// 确保脚本在靠前的执行顺序中运行，以便在其他脚本之前初始化串口
[DefaultExecutionOrder(-100)]
public class SerialPortManager : MonoBehaviour
{
    private static SerialPortManager _instance;
    public static SerialPortManager Instance => _instance;

    private SerialPort _serialPort;
    private Thread _readThread;

    [Header("串口配置")]
    [SerializeField]
    private string _selectedPortName = "COM3";      // 串口名称
    [SerializeField]
    private int _selectedBaudRate = 9600;           // 波特率

    // 下拉列表选项
    [HideInInspector]
    public List<string> availablePorts = new List<string>();
    [HideInInspector]
    public int selectedPortIndex = 0;
    // 常用波特率列表
    [HideInInspector]
    public readonly int[] availableBaudRates = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 14400, 115200 };
    [HideInInspector]
    public int selectedBaudRateIndex = 3;

    public string PortName => _selectedPortName;
    public int BaudRate => _selectedBaudRate;
    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化可用的串口列表
        RefreshPortList();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 自动连接串口
        TryConnectToPort();
    }

    /// <summary>
    /// 刷新可用串口列表
    /// </summary>
    public void RefreshPortList()
    {
        availablePorts.Clear();
        availablePorts.AddRange(SerialPort.GetPortNames());

        // 如果列表为空，添加一些默认值
        if (availablePorts.Count == 0)
        {
            for (int i = 1; i <= 10; i++)
            {
                availablePorts.Add($"COM{i}");
            }
        }

        // 尝试找到已选择的端口在列表中的索引
        selectedPortIndex = availablePorts.FindIndex(p => p == _selectedPortName);
        if (selectedPortIndex < 0 && availablePorts.Count > 0)
        {
            selectedPortIndex = 0;
            _selectedPortName = availablePorts[0];
        }

        // 设置波特率索引
        selectedBaudRateIndex = Array.IndexOf(availableBaudRates, _selectedBaudRate);
        if (selectedBaudRateIndex < 0)
        {
            selectedBaudRateIndex = 3; // 默认使用9600
            _selectedBaudRate = 9600;
        }
    }

    /// <summary>
    /// 更新串口设置并尝试连接
    /// </summary>
    /// <param name="portIndex">端口索引</param>
    /// <param name="baudRateIndex">波特率索引</param>
    public void UpdatePortSettings(int portIndex, int baudRateIndex)
    {
        if (portIndex >= 0 && portIndex < availablePorts.Count)
        {
            selectedPortIndex = portIndex;
            _selectedPortName = availablePorts[portIndex];
        }

        if (baudRateIndex >= 0 && baudRateIndex < availableBaudRates.Length)
        {
            selectedBaudRateIndex = baudRateIndex;
            _selectedBaudRate = availableBaudRates[baudRateIndex];
        }

        // 关闭当前连接
        ClosePort();
        // 尝试使用新设置连接
        TryConnectToPort();
    }

    /// <summary>
    /// 尝试连接到串口
    /// </summary>
    public bool TryConnectToPort()
    {
        ClosePort(); // 确保先关闭之前的连接

        try
        {
            _serialPort = new SerialPort(_selectedPortName, _selectedBaudRate, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();

            _readThread = new Thread(ReadSerialData) { IsBackground = true };
            _readThread.Start();

            this.LogInfo($"串口连接成功: {_selectedPortName}, {_selectedBaudRate}波特率");
            return true;
        }
        catch (Exception e)
        {         
            this.LogWarning($"串口初始化失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 关闭串口连接
    /// </summary>
    public void ClosePort()
    {
        if (IsConnected)
        {
            try
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;

                // 等待读取线程结束
                if (_readThread != null && _readThread.IsAlive)
                {
                    _readThread.Join(1000);
                }

                this.LogInfo("串口已关闭");
            }
            catch (Exception e)
            {
                this.LogWarning($"关闭串口时发生错误: {e.Message}");
            }
        }
    }

    private void ReadSerialData()
    {
        byte[] buffer = new byte[512];
        while (IsConnected)
        {
            try
            {
                int actLen = _serialPort.Read(buffer, 0, buffer.Length);
                if (actLen > 0)
                {
                    string hexString = string.Join(" ", buffer.Take(actLen).Select(b => b.ToString("X2")));
                    ProcessReceivedData(buffer, actLen, hexString);
                }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                this.LogWarning($"串口读取失败: {e.Message}");
            }
            Thread.Sleep(5);
        }
    }

    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    private void ProcessReceivedData(byte[] buffer, int length, string hexString)
    {
        // 切换到主线程记录和处理数据
        UniTask.RunOnThreadPool(async () =>
        {
            await UniTask.SwitchToMainThread();
            this.LogInfo($"接收到数据: {hexString}");

            // 在这里添加自定义的数据处理逻辑
            // 例如: OnDataReceived?.Invoke(buffer, length);
        }).Forget();
    }

    public bool SendData(byte[] data)
    {
        if (!IsConnected)
        {
            this.LogWarning("无法发送数据，串口未连接");
            return false;
        }

        try
        {
            _serialPort.Write(data, 0, data.Length);
            string hexString = string.Join(" ", data.Select(b => b.ToString("X2")));

            // 使用UniTask切换到主线程记录信息
            UniTask.RunOnThreadPool(async () =>
            {
                await UniTask.SwitchToMainThread();
                // 在主线程中执行UI更新或其他操作
                // 例如: UpdateUI(hexString);
                this.LogInfo($"发送数据: {hexString}");
            }).Forget();    // Forget() 表示不需要等待这个任务完成
            return true;
        }
        catch (Exception e)
        {
            this.LogWarning($"串口发送失败: {e.Message}");
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

    private void OnDestroy()
    {
        ClosePort();
        _instance = null;
    }

    private void OnApplicationQuit()
    {
        ClosePort();
        _instance = null;
    }
}
