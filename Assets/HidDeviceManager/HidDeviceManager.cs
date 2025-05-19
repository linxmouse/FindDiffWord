using HidSharp;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;
using Unity.Logging;
using System.Linq;

public class HidDeviceManager : MonoBehaviour
{
    [SerializeField]
    private int vendorId = 0x1234;
    [SerializeField]
    private int productId = 0x5678;

    private HidStream _hidStream;
    private CancellationTokenSource _cts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeAndStartListening().Forget();
    }

    private async UniTaskVoid InitializeAndStartListening()
    {
        try
        {
            await InitializeAsync(vendorId, productId);
            StartListening();
        }
        catch (Exception e)
        {
            Log.Error($"Initialization failed: {e.Message}");
        }
    }

    private async UniTask InitializeAsync(int vendorId, int productId)
    {
        await UniTask.SwitchToThreadPool();

        var deviceList = DeviceList.Local;
        var device = deviceList.GetHidDevices()
            .FirstOrDefault(d => d.VendorID == vendorId && d.ProductID == productId);
        if (device == null) throw new Exception("Device not found.");

        _hidStream = device.Open();
        _cts = new CancellationTokenSource();

        await UniTask.SwitchToMainThread();
    }

    public async UniTask WriteAsync(byte[] data)
    {
        await UniTask.RunOnThreadPool(() => _hidStream.Write(data, 0, data.Length));
    }

    public async UniTask<byte[]> ReadAsync()
    {
        byte[] buffer = new byte[_hidStream.Device.GetMaxInputReportLength()];
        int bytesRead = 0;

        await UniTask.RunOnThreadPool(() => bytesRead = _hidStream.Read(buffer, 0, buffer.Length));
        return buffer.Take(bytesRead).ToArray();
    }

    private void StartListening()
    {
        UniTask.Void(async (CancellationToken ct) =>
        {
            while(!ct.IsCancellationRequested)
            {
                try
                {
                    byte[] data = await ReadAsync();
                    var hexString = string.Join(" ", data.Select(b => b.ToString("X2")));
                    Log.Info($"Received: {hexString}");
                }
                catch (Exception e)
                {
                    Log.Error($"Read error: {e.Message}");
                }
            }
        }, cancellationToken: _cts.Token);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _hidStream?.Dispose();
        DeviceList.Local.Shutdown();
    }
}
