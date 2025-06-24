using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 使用DOTween实现金币等距铺设
/// 
/// 核心原理：
/// - 使用DOTween的DOPath创建路径动画
/// - 使用DOTween内置的PathGetPoint方法获取路径上等距点
/// - 使用SetSpeedBased()确保匀速运动
/// 
/// 优势：
/// - 代码更简洁
/// - 使用DOTween的原生API
/// - 性能更好
/// </summary>
public class DOTweenCoinPaver : MonoBehaviour
{
    [Header("路径配置")]
    [Tooltip("路径点数组")]
    public Transform[] waypoints;

    [Header("金币配置")]
    [Tooltip("金币预制体")]
    public GameObject coinPrefab;

    [Tooltip("金币之间的间距(Unity单位)")]
    [Range(0.1f, 10f)]
    public float coinSpacing = 2f;

    [Header("路径设置")]
    [Tooltip("路径类型")]
    public PathType pathType = PathType.CatmullRom;

    [Tooltip("路径模式")]
    public PathMode pathMode = PathMode.Full3D;

    [Tooltip("路径分辨率")]
    [Range(50, 200)]
    public int pathResolution = 50;

    [Header("动画配置")]
    [Tooltip("整个铺设动画的总持续时间(秒)")]
    [Range(0.5f, 30f)]
    public float paveDuration = 3f;

    [Tooltip("单个金币出现动画的持续时间(秒)")]
    [Range(0.1f, 2f)]
    public float coinAppearDuration = 1f;

    [Tooltip("金币出现时的缓动效果")]
    public Ease appearEase = Ease.OutBack;

    [Header("调试配置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;

    [Tooltip("是否显示路径曲线")]
    public bool showPathCurve = true;

    [Tooltip("是否显示路径点")]
    public bool showWaypoints = true;

    [Tooltip("是否显示金币位置")]
    public bool showCoinPositions = true;

    private Tweener pathTween;
    private List<Vector3> coinPositions = new List<Vector3>();
    private List<Vector3> gizmosPathPoints = new List<Vector3>();

    void Start()
    {
        if (ValidateComponents())
        {
            CalculateCoinPositions();
            PaveCoinsAlongPath();
        }
    }

    /// <summary>
    /// 验证必要组件
    /// </summary>
    private bool ValidateComponents()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("DirectDOTweenCoinPaver: 路径点数量不足, 至少需要2个点!");
            return false;
        }
        if (coinPrefab == null)
        {
            Debug.LogError("DirectDOTweenCoinPaver: 金币预制体未设置!");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 直接使用DOTween计算等距金币位置
    /// </summary>
    private void CalculateCoinPositions()
    {
        coinPositions.Clear();
        // 转换Transform数组为Vector3数组
        Vector3[] pathPoints = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++) pathPoints[i] = waypoints[i].position;
        // 创建一个临时的虚拟对象用于路径计算
        GameObject tempObj = new GameObject("TempPathCalculator");
        tempObj.transform.position = pathPoints[0];
        // 创建路径动画但立即暂停，仅用于计算
        pathResolution = Mathf.Max(waypoints.Length, pathResolution);
        pathTween = tempObj.transform.DOPath(pathPoints, 1f, pathType, pathMode, pathResolution)
            .SetSpeedBased() // 关键：使用速度模式确保匀速
            .SetEase(Ease.Linear) // 线性缓动确保匀速
            .Pause(); // 立即暂停      
        // 强制初始化路径
        pathTween.ForceInit();
        // 获取路径总长度
        float pathLength = GetPathLength();
        // 计算金币数量
        int coinCount = Mathf.FloorToInt(pathLength / coinSpacing) + 1;
        if (showDebugInfo)
        {
            Debug.Log($"[DirectDOTweenCoinPaver] 路径长度: {pathLength:F2}单位, 金币数量: {coinCount}个");
        }
        // 通过等间距获取路径上的点
        for (int i = 0; i < coinCount; i++)
        {
            float progress = (float)i / Mathf.Max(1, coinCount - 1); // 0 到 1
            Vector3 pointOnPath = GetPointOnPath(progress);
            coinPositions.Add(pointOnPath);
        }
        // 清理临时对象
        pathTween.Kill();
        DestroyImmediate(tempObj);
    }

    /// <summary>
    /// 获取路径总长度
    /// </summary>
    private float GetPathLength()
    {
        // 通过采样来估算路径长度
        float totalLength = 0f;
        int sampleCount = 100;
        Vector3 lastPoint = GetPointOnPath(0f);
        for (int i = 1; i <= sampleCount; i++)
        {
            float progress = (float)i / sampleCount;
            Vector3 currentPoint = GetPointOnPath(progress);
            totalLength += Vector3.Distance(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
        return totalLength;
    }

    /// <summary>
    /// 获取路径上指定进度的点
    /// </summary>
    private Vector3 GetPointOnPath(float progress)
    {
        progress = Mathf.Clamp01(progress);
        // 使用DOTween的PathGetPoint方法获取路径上的点
        return pathTween.PathGetPoint(progress);
    }

    /// <summary>
    /// 执行金币铺设动画
    /// </summary>
    private void PaveCoinsAlongPath()
    {
        if (coinPositions.Count == 0)
        {
            Debug.LogWarning("DirectDOTweenCoinPaver: 没有金币位置数据");
            return;
        }

        // 创建动画序列
        Sequence paveSequence = DOTween.Sequence();
        // 计算时间间隔
        float timeInterval = paveDuration / coinPositions.Count;
        if (showDebugInfo) Debug.Log($"[DirectDOTweenCoinPaver] 开始铺设: {coinPositions.Count}个金币, 时间间隔={timeInterval:F3}秒");
        // 为每个金币创建动画
        for (int i = 0; i < coinPositions.Count; i++) CreateCoinAppearAnimation(paveSequence, i, timeInterval);
        // 动画完成后5秒后执行回调
        paveSequence.InsertCallback(5f, () =>
        {
            if (showDebugInfo) Debug.Log("[DirectDOTweenCoinPaver] 金币铺设完成！");
            // TODO 金币铺设完成后的回调
        });
    }

    /// <summary>
    /// 为单个金币创建出现动画
    /// </summary>
    private void CreateCoinAppearAnimation(Sequence sequence, int coinIndex, float timeInterval)
    {
        Vector3 coinPosition = coinPositions[coinIndex];
        // 实例化金币
        GameObject coin = Instantiate(coinPrefab, coinPosition, Quaternion.identity, transform);
        coin.transform.localScale = Vector3.zero;
        // 为金币添加随机动画帧偏移
        RandomizeAnimationOffset(coin);
        // 计算出现时间
        float startTime = coinIndex * timeInterval;
        // 创建淡出和缩放动画
        SpriteRenderer spriteRenderer = coin.GetComponent<SpriteRenderer>();
        Tween fadeTween = spriteRenderer.DOFade(1f, 1f).SetEase(appearEase);
        Tween scaleTween = coin.transform.DOScale(Vector3.one, 1f).SetEase(appearEase);
        sequence.Insert(startTime, fadeTween);
        sequence.Insert(startTime, scaleTween);
    }

    /// <summary>
    /// 为金币添加随机的动画帧偏移
    /// </summary>
    private void RandomizeAnimationOffset(GameObject coin)
    {
        // Animator组件处理
        Animator animator = coin.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float randomOffset = Random.Range(0f, 1f);
            animator.Play(0, 0, randomOffset);
            return;
        }

        // Animation组件处理
        Animation animation = coin.GetComponent<Animation>();
        if (animation != null && animation.clip != null)
        {
            float randomTime = Random.Range(0f, animation.clip.length);
            animation[animation.clip.name].time = randomTime;
            animation.Sample();
        }
    }

    /// <summary>
    /// 在Scene视图中绘制路径预览 - 准确反映DOTween路径参数效果
    /// </summary>
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        // 绘制路径点 (waypoints)
        if (showWaypoints)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                    // 显示路径点序号
#if UNITY_EDITOR
                    Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"W{i}");
#endif
                }
            }
        }
        // 绘制真实的DOTween路径曲线
        if (showPathCurve) DrawDOTweenPath();
        // 绘制计算出的金币位置
        if (showCoinPositions && Application.isPlaying && coinPositions.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < coinPositions.Count; i++)
            {
                Gizmos.DrawWireSphere(coinPositions[i], 0.15f);
                // 显示金币序号
#if UNITY_EDITOR
                Handles.Label(coinPositions[i] + Vector3.up * 0.3f, $"C{i}");
#endif
            }
        }
        // 显示路径参数信息
        if (showDebugInfo && waypoints.Length >= 2)
        {
            Vector3 labelPos = waypoints[0].position + Vector3.up * 1.5f;
            string pathInfo = $"PathType: {pathType}\nPathMode: {pathMode}\nResolution: {pathResolution}";
#if UNITY_EDITOR
            Handles.Label(labelPos, pathInfo);
#endif
        }
    }

    /// <summary>
    /// 绘制真实的DOTween路径曲线
    /// </summary>
    private void DrawDOTweenPath()
    {
        // 更新Gizmos路径点
        UpdateGizmosPathPoints();
        if (gizmosPathPoints.Count < 2) return;
        // 绘制路径曲线
        Gizmos.color = Color.green;
        for (int i = 0; i < gizmosPathPoints.Count - 1; i++) Gizmos.DrawLine(gizmosPathPoints[i], gizmosPathPoints[i + 1]);
        // 绘制路径方向箭头
        DrawPathDirectionArrows();
    }

    /// <summary>
    /// 更新Gizmos用的路径点 - 反映真实的DOTween路径形状
    /// </summary>
    private void UpdateGizmosPathPoints()
    {
        gizmosPathPoints.Clear();
        // 转换Transform数组为Vector3数组
        Vector3[] pathPoints = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null) pathPoints[i] = waypoints[i].position;
        }
        // 创建临时对象来生成路径
        GameObject tempObj = new GameObject("TempGizmosPathCalculator");
        tempObj.transform.position = pathPoints[0];
        tempObj.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            // 创建DOTween路径 (使用相同的参数)
            Tweener tempTween = tempObj.transform.DOPath(pathPoints, 1f, pathType, pathMode, pathResolution)
                .SetSpeedBased()
                .SetEase(Ease.Linear)
                .Pause();
            tempTween.ForceInit();
            // 采样路径点用于Gizmos绘制
            for (int i = 0; i <= pathResolution; i++)
            {
                float progress = (float)i / pathResolution;
                Vector3 point = tempTween.PathGetPoint(progress);
                gizmosPathPoints.Add(point);
            }
            tempTween.Kill();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"DOTweenCoinPaver: 无法生成Gizmos路径预览: {e.Message}");
            // 降级到简单直线连接
            for (int i = 0; i < pathPoints.Length; i++) gizmosPathPoints.Add(pathPoints[i]);
        }
        finally { DestroyImmediate(tempObj); }
    }

    /// <summary>
    /// 绘制路径方向箭头
    /// </summary>
    private void DrawPathDirectionArrows()
    {
        if (gizmosPathPoints.Count < 2) return;   
        int arrowCount = Mathf.Min(5, gizmosPathPoints.Count / 10); 
        Gizmos.color = Color.yellow;
        for (int i = 0; i < arrowCount; i++)
        {
            // 均匀分布箭头位置
            float progress = (float)(i + 1) / (arrowCount + 1);
            int index = Mathf.RoundToInt(progress * (gizmosPathPoints.Count - 1));
            if (index > 0 && index < gizmosPathPoints.Count - 2)
            {
                Vector3 current = gizmosPathPoints[index];
                Vector3 next = gizmosPathPoints[index + 2];
                Vector3 direction = (next - current).normalized;
                if (direction.magnitude > 0.1f)
                {
                    // 简单的 -> 箭头
                    Vector3 arrowEnd = current + direction * 0.4f;
                    Vector3 arrowLeft = current + direction * 0.25f + Vector3.Cross(direction, Vector3.up).normalized * 0.1f;
                    Vector3 arrowRight = current + direction * 0.25f - Vector3.Cross(direction, Vector3.up).normalized * 0.1f;
                    // 绘制箭头主线和两个分叉
                    Gizmos.DrawLine(current, arrowEnd);
                    Gizmos.DrawLine(arrowEnd, arrowLeft);
                    Gizmos.DrawLine(arrowEnd, arrowRight);
                }
            }
        }
    }

}