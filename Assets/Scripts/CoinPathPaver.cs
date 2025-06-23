using UnityEngine;
using DG.Tweening;
using SWS;
using System.Collections.Generic;

/// <summary>
/// 金币路径铺设器 - 在指定路径上按等距间隔铺设金币动画
/// 
/// 功能说明：
/// - 基于 Simple Waypoint System 的路径管理器生成等距分布的金币
/// - 使用线性分段算法确保金币间距数学上精确相等
/// - 支持 DoTween 动画序列，实现平滑的金币出现效果
/// - 自动处理内存管理，避免动画泄漏
/// 
/// 核心算法：
/// 1. 将复杂路径分解为多段直线
/// 2. 在每段直线上使用线性插值计算精确位置
/// 3. 通过累积距离跟踪确保严格等距分布
/// 
/// 使用方法：
/// 1. 在场景中创建 PathManager 并设置路径点
/// 2. 创建金币预制体（需要包含 SpriteRenderer 和 Animator）
/// 3. 将此脚本添加到空 GameObject 上
/// 4. 配置相关参数并运行
/// 
/// 作者：AI Assistant
/// 版本：2.0 - 线性分段算法版本
/// 兼容：Unity 2019.4+, Simple Waypoint System, DoTween
/// </summary>
public class CoinPathPaver : MonoBehaviour
{
    #region 公共配置参数

    [Header("路径配置")]
    [Tooltip("Simple Waypoint System 的路径管理器。必须包含至少2个路径点。")]
    public PathManager pathManager;

    [Header("金币配置")]
    [Tooltip("金币预制体。应包含 SpriteRenderer（用于显示）和 Animator（用于序列帧动画）组件。")]
    public GameObject coinPrefab;
    
    [Tooltip("金币之间的间距（Unity单位）。值越小金币越密集，值越大金币越稀疏。")]
    [Range(0.1f, 10f)]
    public float coinSpacing = 2f;

    [Header("动画配置")]
    [Tooltip("整个铺设动画的总持续时间（秒）。所有金币在此时间内依次出现。")]
    [Range(0.5f, 10f)]
    public float paveDuration = 3f;
    
    [Tooltip("单个金币出现动画的持续时间（秒）。推荐值：0.3-0.8秒。")]
    [Range(0.1f, 2f)]
    public float coinAppearDuration = 0.3f;
    
    [Tooltip("金币出现时的缓动效果。OutBack 提供弹性效果，OutQuad 提供平滑效果。")]
    public Ease appearEase = Ease.OutBack;

    [Header("调试配置")]
    [Tooltip("是否在控制台输出调试信息，包括路径长度、金币数量等统计数据。")]
    public bool showDebugInfo = true;

    #endregion

    #region 私有变量

    /// <summary>路径点数组，从 PathManager 获取</summary>
    private Vector3[] pathPoints;
    
    /// <summary>路径总长度（所有线段长度之和）</summary>
    private float totalPathLength;
    
    /// <summary>计算出的金币总数量</summary>
    private int coinCount;
    
    /// <summary>
    /// 预计算的等距金币位置列表
    /// 这是核心数据结构，存储了所有金币的精确世界坐标
    /// </summary>
    private List<Vector3> equalSpacedPoints = new List<Vector3>();

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// Unity Start 方法 - 组件初始化入口点
    /// 执行顺序：参数验证 → 路径初始化 → 金币铺设
    /// </summary>
    void Start()
    {
        // 参数验证：确保必要组件已配置
        if (!ValidateComponents())
            return;
        
        // 核心流程
        InitializePath();
        PaveCoinsAlongPath();
    }

    #endregion

    #region 核心功能方法

    /// <summary>
    /// 验证必要的组件和参数配置
    /// </summary>
    /// <returns>如果所有必要组件都已正确配置则返回 true</returns>
    private bool ValidateComponents()
    {
        if (pathManager == null)
        {
            Debug.LogError("CoinPathPaver: PathManager 未设置！请在 Inspector 中拖入一个包含路径点的 PathManager。");
            return false;
        }
        
        if (coinPrefab == null)
        {
            Debug.LogError("CoinPathPaver: 金币预制体未设置！请在 Inspector 中拖入金币预制体。");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 初始化路径数据并计算等距分布点
    /// 这是算法的核心入口点
    /// </summary>
    void InitializePath()
    {
        // 从 PathManager 获取路径点
        pathPoints = pathManager.GetPathPoints();
        
        // 验证路径点数量
        if (pathPoints.Length < 2)
        {
            Debug.LogError("CoinPathPaver: 路径点数量不足！至少需要2个点才能构成有效路径。");
            return;
        }

        // 核心算法：生成等距分布的金币位置
        GenerateEqualSpacedPoints();
        
        // 输出统计信息
        if (showDebugInfo)
        {
            Debug.Log($"[CoinPathPaver] 路径分析完成：" +
                     $"总长度={totalPathLength:F2}单位, " +
                     $"设定间距={coinSpacing:F2}单位, " +
                     $"生成金币={coinCount}个");
        }
    }
    
    /// <summary>
    /// 核心算法：线性分段等距分布算法
    /// 
    /// 算法原理：
    /// 1. 将复杂路径分解为若干条直线段
    /// 2. 计算每段直线的长度，累积得到总路径长度
    /// 3. 根据指定间距计算应放置的金币数量
    /// 4. 从起点开始，沿路径每隔指定距离放置一个金币
    /// 5. 使用线性插值在每段直线上精确定位金币位置
    /// 
    /// 时间复杂度：O(n + m)，其中 n 为路径点数，m 为金币数
    /// 空间复杂度：O(m)，主要用于存储金币位置
    /// </summary>
    void GenerateEqualSpacedPoints()
    {
        // 清空之前的计算结果
        equalSpacedPoints.Clear();
        
        // 第一步：计算路径总长度
        totalPathLength = CalculateTotalPathLength();
        
        // 第二步：计算金币数量
        coinCount = Mathf.FloorToInt(totalPathLength / coinSpacing) + 1;
        
        // 第三步：放置起点金币
        equalSpacedPoints.Add(pathPoints[0]);
        
        // 特殊情况：如果只需要一个金币，直接返回
        if (coinCount == 1)
        {
            return;
        }
        
        // 第四步：核心算法 - 等距分布计算
        PlaceCoinsAlongPath();
        
        // 第五步：确保终点有金币
        EnsureEndPointCoin();
        
        // 第六步：更新最终金币数量
        coinCount = equalSpacedPoints.Count;
    }

    /// <summary>
    /// 计算路径总长度
    /// 遍历所有相邻路径点，累加直线距离
    /// </summary>
    /// <returns>路径总长度</returns>
    private float CalculateTotalPathLength()
    {
        float length = 0f;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            length += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
        }
        return length;
    }

    /// <summary>
    /// 沿路径放置金币的核心算法
    /// 
    /// 算法详解：
    /// - currentTargetDistance: 当前要放置金币的目标距离
    /// - accumulatedDistance: 从起点到当前线段起点的累积距离
    /// - 对每个线段，检查是否有金币应该放置在该线段上
    /// - 如果有，使用线性插值计算精确位置
    /// </summary>
    private void PlaceCoinsAlongPath()
    {
        float currentTargetDistance = coinSpacing; // 第一个要放置的金币距离起点的距离
        float accumulatedDistance = 0f;            // 累积距离追踪器
        
        // 遍历每个路径线段
        for (int segmentIndex = 0; segmentIndex < pathPoints.Length - 1; segmentIndex++)
        {
            Vector3 segmentStart = pathPoints[segmentIndex];
            Vector3 segmentEnd = pathPoints[segmentIndex + 1];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            
            // 在当前线段上查找所有应该放置金币的位置
            while (ShouldPlaceCoinInSegment(currentTargetDistance, accumulatedDistance, segmentLength))
            {
                // 计算金币在当前线段上的精确位置
                Vector3 coinPosition = CalculateCoinPositionInSegment(
                    currentTargetDistance, accumulatedDistance, 
                    segmentStart, segmentEnd, segmentLength);
                
                equalSpacedPoints.Add(coinPosition);
                
                // 移动到下一个目标距离
                currentTargetDistance += coinSpacing;
            }
            
            // 更新累积距离
            accumulatedDistance += segmentLength;
        }
    }

    /// <summary>
    /// 判断是否应该在当前线段放置金币
    /// </summary>
    /// <param name="targetDistance">目标距离</param>
    /// <param name="accumulatedDistance">累积距离</param>
    /// <param name="segmentLength">当前线段长度</param>
    /// <returns>如果应该在此线段放置金币则返回 true</returns>
    private bool ShouldPlaceCoinInSegment(float targetDistance, float accumulatedDistance, float segmentLength)
    {
        return targetDistance <= accumulatedDistance + segmentLength && 
               equalSpacedPoints.Count < coinCount;
    }

    /// <summary>
    /// 计算金币在指定线段上的精确位置
    /// 使用线性插值确保数学精确性
    /// </summary>
    /// <param name="targetDistance">目标距离</param>
    /// <param name="accumulatedDistance">累积距离</param>
    /// <param name="segmentStart">线段起点</param>
    /// <param name="segmentEnd">线段终点</param>
    /// <param name="segmentLength">线段长度</param>
    /// <returns>金币的精确世界坐标</returns>
    private Vector3 CalculateCoinPositionInSegment(float targetDistance, float accumulatedDistance,
                                                   Vector3 segmentStart, Vector3 segmentEnd, float segmentLength)
    {
        // 计算目标距离在当前线段上的投影
        float distanceInSegment = targetDistance - accumulatedDistance;
        
        // 计算在线段上的百分比位置（0-1）
        float t = distanceInSegment / segmentLength;
        
        // 使用线性插值计算精确位置
        return Vector3.Lerp(segmentStart, segmentEnd, t);
    }

    /// <summary>
    /// 确保路径终点有金币
    /// 根据当前金币数量决定是否需要在终点添加金币
    /// </summary>
    private void EnsureEndPointCoin()
    {
        Vector3 endPoint = pathPoints[pathPoints.Length - 1];
        
        if (equalSpacedPoints.Count == coinCount - 1)
        {
            // 正好缺少最后一个金币，添加到终点
            equalSpacedPoints.Add(endPoint);
        }
        else if (equalSpacedPoints.Count < coinCount)
        {
            // 金币数量不足，强制在终点添加一个
            equalSpacedPoints.Add(endPoint);
        }
        // 注意：如果 equalSpacedPoints.Count >= coinCount，则不添加
        // 这种情况下最后一个金币可能不在严格的终点，但间距是精确的
    }
    
    /// <summary>
    /// 执行金币铺设动画
    /// 使用 DoTween 创建时间序列动画，让金币依次出现
    /// 
    /// 动画特点：
    /// - 金币初始缩放为0（不可见）
    /// - 按时间间隔依次播放缩放动画
    /// - 支持自定义缓动效果
    /// - 自动处理内存清理，避免动画泄漏
    /// </summary>
    void PaveCoinsAlongPath()
    {
        // 验证是否有金币位置数据
        if (equalSpacedPoints.Count == 0)
        {
            Debug.LogWarning("CoinPathPaver: 没有生成金币位置数据，跳过铺设。");
            return;
        }
        
        // 创建 DoTween 动画序列
        Sequence paveSequence = DOTween.Sequence();
        
        // 计算金币出现的时间间隔
        float timeInterval = paveDuration / coinCount;
        
        if (showDebugInfo)
        {
            Debug.Log($"[CoinPathPaver] 开始铺设动画：{coinCount}个金币，时间间隔={timeInterval:F3}秒");
        }
        
        // 为每个金币创建出现动画
        for (int i = 0; i < coinCount; i++)
        {
            CreateCoinAppearAnimation(paveSequence, i, timeInterval);
        }
        
        // 设置动画完成回调
        paveSequence.OnComplete(() => {
            if (showDebugInfo)
            {
                Debug.Log("[CoinPathPaver] 金币铺设动画完成！");
            }
        });
    }

    /// <summary>
    /// 为单个金币创建出现动画
    /// </summary>
    /// <param name="sequence">动画序列</param>
    /// <param name="coinIndex">金币索引</param>
    /// <param name="timeInterval">时间间隔</param>
    private void CreateCoinAppearAnimation(Sequence sequence, int coinIndex, float timeInterval)
    {
        // 获取预计算的金币位置
        Vector3 coinPosition = equalSpacedPoints[coinIndex];
        
        // 实例化金币对象
        GameObject coin = Instantiate(coinPrefab, coinPosition, Quaternion.identity, transform);
        
        // 设置初始状态：不可见（缩放为0）
        coin.transform.localScale = Vector3.zero;
        
        // 计算此金币的出现时间
        float startTime = coinIndex * timeInterval;
        
        // 创建缩放动画并插入到序列中
        // 使用 Insert 而不是 Append 可以创建重叠的动画效果
        Tween scaleTween = coin.transform
            .DOScale(1f, coinAppearDuration)    // 从0缩放到1
            .SetEase(appearEase)                // 应用缓动效果
            .SetTarget(coin);                   // 绑定到金币对象，确保对象销毁时动画也被清理
        
        sequence.Insert(startTime, scaleTween);
    }

    #endregion

    #region 调试和维护

    /// <summary>
    /// 运行时参数验证
    /// 在编辑器中修改参数时提供实时反馈
    /// </summary>
    void OnValidate()
    {
        // 确保间距在合理范围内
        coinSpacing = Mathf.Max(0.1f, coinSpacing);
        
        // 确保动画时间在合理范围内
        paveDuration = Mathf.Max(0.1f, paveDuration);
        coinAppearDuration = Mathf.Max(0.1f, coinAppearDuration);
    }

    /// <summary>
    /// 获取当前配置的统计信息
    /// 用于外部脚本查询或调试
    /// </summary>
    /// <returns>配置统计信息</returns>
    public string GetStatistics()
    {
        return $"路径长度: {totalPathLength:F2}, 金币数量: {coinCount}, 实际间距: {(coinCount > 1 ? totalPathLength / (coinCount - 1) : 0):F2}";
    }

    #endregion
} 