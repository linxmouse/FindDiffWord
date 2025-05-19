using SerialPortSharp;
using System.Collections;
using UnityEngine;

public class SerialPortTest : MonoBehaviour
{
    [Header("Connection Settings")]
    public string portName = "COM1";
    public int baudRate = 115200;

    [Header("Debug")]
    [SerializeField] private bool _autoConnectOnStart = true;
    [SerializeField] private bool _logReceivedData = true;

    private LibSerialPort _serialPort;
    private Coroutine _readCoroutine;

    void Start()
    {
        var ports = LibSerialPort.GetAvailablePorts();
        Debug.Log($"Available Ports: {string.Join(", ", ports)}");
        if (_autoConnectOnStart) OpenPort();
    }

    public void OpenPort()
    {
        try
        {
            _serialPort = new LibSerialPort(portName, baudRate);
            _serialPort.Open();

            _readCoroutine = StartCoroutine(ReadDataRoutine());
            Debug.Log($"Port {portName} opened successfully");
        }
        catch (SerialPortException e)
        {
            Debug.LogError($"Open failed: {e.Message}");
        }
    }

    private IEnumerator ReadDataRoutine()
    {
        while (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                byte[] data = _serialPort.Read(1024, 100);
                if (data.Length > 0 && _logReceivedData)
                {
                    Debug.Log($"Received: {System.Text.Encoding.ASCII.GetString(data)}");
                }
            }
            catch (SerialPortException e)
            {
                Debug.LogWarning($"Read error: {e.Message}");
            }
            yield return null;
        }
    }

    public void SendString(string message)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            Debug.LogWarning("Port not open");
            return;
        }

        byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
        _serialPort.Write(data);
        Debug.Log($"Sent: {message}");
    }

    void OnDestroy()
    {
        _serialPort?.Dispose();
        if (_readCoroutine != null)
            StopCoroutine(_readCoroutine);
    }
}
