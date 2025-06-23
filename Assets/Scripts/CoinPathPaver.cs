using UnityEngine;
using DG.Tweening;
using SWS;

public class CoinPathPaver : MonoBehaviour
{
    [Header("路径设置")]
    public PathManager pathManager; // 拖入你的路径管理器
    
    [Header("金币设置")]
    public GameObject coinPrefab; // 金币预制体（带序列帧动画）
    public float coinSpacing = 2f; // 金币之间的间距（单位）
    
    [Header("动画设置")]
    public float paveDuration = 3f; // 铺设动画的总时长
    public float coinAppearDuration = 0.3f; // 单个金币出现动画时长
    public Ease appearEase = Ease.OutBack; // 金币出现时的缓动效果
    
    [Header("调试")]
    public bool showDebugInfo = true;
    
    private Vector3[] pathPoints;
    private float totalPathLength;
    private int coinCount;
    
    void Start()
    {
        if (pathManager == null)
        {
            Debug.LogError("CoinPathPaver: 请设置 PathManager!");
            return;
        }
        
        if (coinPrefab == null)
        {
            Debug.LogError("CoinPathPaver: 请设置金币预制体!");
            return;
        }
        
        InitializePath();
        PaveCoinsAlongPath();
    }
    
    void InitializePath()
    {
        // 获取路径点
        pathPoints = pathManager.GetPathPoints();
        
        if (pathPoints.Length < 2)
        {
            Debug.LogError("CoinPathPaver: 路径点数量不足, 至少需要2个点!");
            return;
        }
        
        // 计算路径总长度
        totalPathLength = WaypointManager.GetPathLength(pathPoints);
        
        // 计算需要多少个金币（确保覆盖从起点到终点）
        // 如果路径长度能被间距整除，金币数量 = 路径长度/间距 + 1
        // 如果不能整除，金币数量 = 向上取整(路径长度/间距) + 1
        coinCount = Mathf.CeilToInt(totalPathLength / coinSpacing) + 1;
        
        if (showDebugInfo)
        {
            Debug.Log($"路径总长度: {totalPathLength:F2}, 金币间距: {coinSpacing:F2}, 金币数量: {coinCount}");
        }
    }
    
    void PaveCoinsAlongPath()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;
        
        // 创建动画序列
        Sequence paveSequence = DOTween.Sequence();
        
        // 计算每个金币出现的时间间隔
        float timeInterval = paveDuration / coinCount;
        
        for (int i = 0; i < coinCount; i++)
        {
            // 计算当前金币在路径上的距离
            float distanceAlongPath;
            
            if (i == 0)
            {
                // 第一个金币放在起点
                distanceAlongPath = 0f;
            }
            else if (i == coinCount - 1)
            {
                // 最后一个金币放在终点
                distanceAlongPath = totalPathLength;
            }
            else
            {
                // 中间的金币按间距分布
                distanceAlongPath = i * coinSpacing;
            }
            
            // 获取路径上的精确位置（使用弧长参数化）
            Vector3 coinPosition = GetPointOnPathByDistance(distanceAlongPath);
            
            // 实例化金币
            GameObject coin = Instantiate(coinPrefab, coinPosition, Quaternion.identity, transform);
            
            // 设置金币初始状态（缩放为0）
            coin.transform.localScale = Vector3.zero;
            
            // 将金币出现动画添加到序列中
            float startTime = i * timeInterval;
            paveSequence.Insert(startTime, 
                coin.transform.DOScale(1f, coinAppearDuration)
                    .SetEase(appearEase)
                    .SetTarget(coin) // 确保金币销毁时动画也会被清理
            );
        }
        
        // 序列完成后的回调
        paveSequence.OnComplete(() => {
            if (showDebugInfo)
            {
                Debug.Log("金币铺设完成!");
            }
        });
    }
    
    Vector3 GetPointOnPath(float percentage)
    {
        if (pathPoints.Length < 2) return Vector3.zero;
        
        // 如果路径只有2个点，直接线性插值
        if (pathPoints.Length == 2)
        {
            return Vector3.Lerp(pathPoints[0], pathPoints[1], percentage);
        }
        
        // 对于多点路径，使用 Simple Waypoint System 的曲线计算
        return WaypointManager.GetPoint(pathPoints, percentage);
    }
    
    // 新增：根据距离获取路径上的点（弧长参数化）
    Vector3 GetPointOnPathByDistance(float distance)
    {
        if (pathPoints.Length < 2) return Vector3.zero;
        
        // 如果路径只有2个点，直接线性插值
        if (pathPoints.Length == 2)
        {
            float distancePercentage = distance / totalPathLength;
            return Vector3.Lerp(pathPoints[0], pathPoints[1], distancePercentage);
        }
        
        // 对于多点路径，使用二分查找找到对应的百分比
        float targetDistance = Mathf.Clamp(distance, 0, totalPathLength);
        float percentage = FindPercentageByDistance(targetDistance);
        
        return WaypointManager.GetPoint(pathPoints, percentage);
    }
    
    // 新增：使用二分查找根据距离找到对应的百分比
    float FindPercentageByDistance(float targetDistance)
    {
        float left = 0f;
        float right = 1f;
        float epsilon = 0.001f; // 精度
        
        while (right - left > epsilon)
        {
            float mid = (left + right) * 0.5f;
            float midDistance = GetDistanceAtPercentage(mid);
            
            if (midDistance < targetDistance)
            {
                left = mid;
            }
            else
            {
                right = mid;
            }
        }
        
        return (left + right) * 0.5f;
    }
    
    // 新增：计算路径上某个百分比位置对应的距离
    float GetDistanceAtPercentage(float targetPercentage)
    {
        if (pathPoints.Length < 2) return 0f;
        
        // 对于简单路径，直接计算
        if (pathPoints.Length == 2)
        {
            return targetPercentage * totalPathLength;
        }
        
        // 对于复杂路径，使用数值积分
        int segments = 100; // 积分段数
        float accumulatedDistance = 0f;
        Vector3 prevPoint = pathPoints[0];
        
        for (int i = 1; i <= segments; i++)
        {
            float currentPercentage = (float)i / segments;
            if (currentPercentage > targetPercentage) break;
            
            Vector3 currentPoint = WaypointManager.GetPoint(pathPoints, currentPercentage);
            accumulatedDistance += Vector3.Distance(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        
        return accumulatedDistance;
    }
    
    // 编辑器中的调试绘制
    void OnDrawGizmos()
    {
        if (!showDebugInfo || pathManager == null) return;
        
        Vector3[] points = pathManager.GetPathPoints();
        if (points.Length < 2) return;
        
        // 绘制路径
        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
        
        // 绘制金币位置预览
        if (Application.isPlaying && coinCount > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < coinCount; i++)
            {
                float distanceAlongPath;
                
                if (i == 0)
                {
                    // 第一个金币放在起点
                    distanceAlongPath = 0f;
                }
                else if (i == coinCount - 1)
                {
                    // 最后一个金币放在终点
                    distanceAlongPath = totalPathLength;
                }
                else
                {
                    // 中间的金币按间距分布
                    distanceAlongPath = i * coinSpacing;
                }
                
                Vector3 coinPos = GetPointOnPathByDistance(distanceAlongPath);
                Gizmos.DrawWireSphere(coinPos, 0.3f);
            }
        }
    }
} 