using System;
using System.Threading;
using UnityEngine;

public class AwatableTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        // 等待下一帧执行
        await Awaitable.NextFrameAsync();
        // 等待1秒钟
        await Awaitable.WaitForSecondsAsync(1f);
        // 切换到后台线程执行
        await Awaitable.BackgroundThreadAsync();
        Debug.Log($"Thread Id = {Thread.CurrentThread.ManagedThreadId}");
        for (int i = 0; i < 10; i++)
        {
            //Thread.Sleep(TimeSpan.FromSeconds(1));
            for (int j = 0; j < 1000000; j++)
            {
                // 模拟一些计算工作
                var temp = Math.Sqrt(j);
            }
        }
        // 切换到主线程执行
        await Awaitable.MainThreadAsync();
        Debug.Log($"Thread Id = {Thread.CurrentThread.ManagedThreadId}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
