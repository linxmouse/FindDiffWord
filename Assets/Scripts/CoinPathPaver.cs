using UnityEngine;
using DG.Tweening;
using SWS;
using System.Collections.Generic;

/// <summary>
/// 金币路径铺设器 - 使用SWS splineMove的时间插值实现精确等距金币铺设
/// 
/// 功能说明：
/// - 利用SWS splineMove的时间插值机制，通过等时间间隔获取等距位置
/// - 比手动计算更精确，支持所有SWS路径类型（直线、曲线等）
/// - 使用DoTween实现金币出现动画
/// - 自动处理内存管理，避免动画泄漏
/// 
/// 核心原理：
/// - splineMove以匀速运动，等时间间隔 = 等距离间隔
/// - 通过控制虚拟splineMove的时间进度获取路径上的精确位置
/// - 无需手动计算路径长度和线性插值
/// 
/// 使用方法：
/// 1. 在场景中创建 PathManager 并设置路径点
/// 2. 创建金币预制体
/// 3. 将此脚本添加到空 GameObject 上
/// 4. 配置参数并运行
/// 
/// </summary>
public class CoinPathPaver : MonoBehaviour
{
    #region 公共配置参数
    [Header("路径配置")]
    [Tooltip("Simple Waypoint System 的路径管理器")]
    public PathManager pathManager;

    [Header("金币配置")]
    [Tooltip("金币预制体")]
    public GameObject coinPrefab;

    [Tooltip("金币之间的间距(Unity单位)")]
    [Range(0.1f, 10f)]
    public float coinSpacing = 2f;

    [Header("动画配置")]
    [Tooltip("整个铺设动画的总持续时间(秒)")]
    [Range(0.5f, 10f)]
    public float paveDuration = 3f;

    [Tooltip("单个金币出现动画的持续时间(秒)")]
    [Range(0.1f, 2f)]
    public float coinAppearDuration = 1f;

    [Tooltip("金币出现时的缓动效果")]
    public Ease appearEase = Ease.OutBack;

    [Header("路径设置")]
    [Tooltip("路径类型：直线或曲线")]
    public PathType pathType = PathType.Linear;

    [Tooltip("路径总移动时间(用于计算等距位置)")]
    [Range(1f, 20f)]
    public float pathDuration = 10f;

    [Header("调试配置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = true;
    #endregion

    #region 私有变量
    /// <summary>虚拟的splineMove用于计算路径位置</summary>
    private splineMove virtualSplingMover;
    /// <summary>缓存的tween引用</summary>
    private Tweener cachedTween;
    /// <summary>计算出的金币位置列表</summary>
    private List<Vector3> coinPositions = new List<Vector3>();
    /// <summary>金币总数量</summary>
    private int coinCount;
    #endregion

    #region Unity 生命周期
    void Start()
    {
        if (!ValidateComponents())
            return;

        CalculateCoinPositions();
        PaveCoinsAlongPath();
    }

    void OnDestroy()
    {
        // 清理虚拟对象
        if (virtualSplingMover != null)
        {
            virtualSplingMover.Stop();
            DestroyImmediate(virtualSplingMover);
        }
    }
    #endregion

    /// <summary>
    /// 验证必要组件
    /// </summary>
    private bool ValidateComponents()
    {
        if (pathManager == null)
        {
            Debug.LogError("CoinPathPaver: PathManager 未设置!");
            return false;
        }

        if (coinPrefab == null)
        {
            Debug.LogError("CoinPathPaver: 金币预制体未设置!");
            return false;
        }

        if (pathManager.GetPathPoints().Length < 2)
        {
            Debug.LogError("CoinPathPaver: 路径点数量不足, 至少需要2个点!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 使用SWS splineMove计算等距金币位置
    /// 核心思路：利用splineMove的匀速运动特性，等时间间隔获取等距位置
    /// </summary>
    private void CalculateCoinPositions()
    {
        coinPositions.Clear();
        // 创建虚拟对象用于路径计算
        CreateInVisableSplingMover();
        // 启动虚拟移动（但立即暂停）
        virtualSplingMover.StartMove();
        virtualSplingMover.Pause();
        // 缓存tween引用（类似PathInputDemo的方法）
        if (virtualSplingMover.tween == null)
        {
            Debug.LogError("CoinPathPaver: 虚拟移动对象的tween创建失败!");
            return;
        }
        cachedTween = virtualSplingMover.tween;
        // 使用tween的实际持续时间
        float actualDuration = cachedTween.Duration();
        // 估算路径长度（通过采样计算）
        float estimatedPathLength = EstimatePathLength(actualDuration);
        // 计算金币数量
        coinCount = Mathf.FloorToInt(estimatedPathLength / coinSpacing) + 1;
        // 通过时间插值获取等距位置
        for (int i = 0; i < coinCount; i++)
        {
            float timeProgress = (float)i / (coinCount - 1); // 0 到 1
            float targetTime = timeProgress * actualDuration;
            // 使用安全的方法设置位置
            if (SetTweenPosition(targetTime, actualDuration)) coinPositions.Add(virtualSplingMover.transform.position);
            else
            {
                Debug.LogError($"CoinPathPaver: 无法设置金币位置第{i}次!");
                break;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[CoinPathPaver] 使用SWS时间插值生成: 路径长度≈{estimatedPathLength:F2}单位, " +
                     $"间距={coinSpacing:F2}单位, 金币数量={coinCount}个");
        }
    }

    /// <summary>
    /// 创建用于计算位置的虚拟移动对象
    /// </summary>
    private void CreateInVisableSplingMover()
    {
        // 创建虚拟对象
        var go = new GameObject("SplingMover");
        go.transform.SetParent(transform);
        go.SetActive(false); // 隐藏，只用于计算   
        // 添加splineMove组件
        virtualSplingMover = go.AddComponent<splineMove>();
        // 配置splineMove
        virtualSplingMover.pathContainer = pathManager;
        virtualSplingMover.onStart = false; // 手动控制启动
        virtualSplingMover.speed = 1f; // 速度由pathDuration控制
        virtualSplingMover.timeValue = splineMove.TimeValue.time;
        virtualSplingMover.easeType = Ease.Linear; // 匀速运动确保等距
        virtualSplingMover.pathType = pathType;
        virtualSplingMover.loopType = splineMove.LoopType.none;
        // 计算所需速度以在pathDuration内完成路径
        // 这里我们让splineMove用时间模式，总时间为pathDuration
        virtualSplingMover.speed = pathDuration;
    }

    /// <summary>
    /// 通过采样估算路径长度
    /// </summary>
    private float EstimatePathLength(float duration)
    {
        float totalLength = 0f;
        Vector3 lastPos = virtualSplingMover.transform.position;
        // 采样100个点来估算路径长度
        int sampleCount = 100;
        for (int i = 1; i <= sampleCount; i++)
        {
            float timeProgress = (float)i / sampleCount;
            float targetTime = timeProgress * duration;
            // 使用安全的方法设置位置
            if (SetTweenPosition(targetTime, duration))
            {
                Vector3 currentPos = virtualSplingMover.transform.position;
                totalLength += Vector3.Distance(lastPos, currentPos);
                lastPos = currentPos;
            }
            else
            {
                Debug.LogError($"CoinPathPaver: 无法设置采样位置第{i}次!");
                break;
            }
        }
        // 重置到起点
        SetTweenPosition(0f, duration);
        return totalLength;
    }

    /// <summary>
    /// 执行金币铺设动画
    /// </summary>
    private void PaveCoinsAlongPath()
    {
        if (coinPositions.Count == 0)
        {
            Debug.LogWarning("CoinPathPaver: 没有金币位置数据");
            return;
        }
        // 创建动画序列
        Sequence paveSequence = DOTween.Sequence();
        // 计算时间间隔
        float timeInterval = paveDuration / coinCount;
        if (showDebugInfo)
        {
            Debug.Log($"[CoinPathPaver] 开始铺设动画：{coinCount}个金币, 时间间隔={timeInterval:F3}秒");
        }
        // 为每个金币创建动画
        for (int i = 0; i < coinPositions.Count; i++)
        {
            CreateCoinAppearAnimation(paveSequence, i, timeInterval);
        }
        // 设置完成回调
        paveSequence.OnComplete(() =>
        {
            if (showDebugInfo) Debug.Log("[CoinPathPaver] 金币铺设完成！");
            // 清理虚拟对象
            if (virtualSplingMover != null) DestroyImmediate(virtualSplingMover);
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
        // 为金币的序列帧动画添加随机偏移，避免所有金币同步播放
        RandomizeAnimationOffset(coin);
        // 计算出现时间
        float startTime = coinIndex * timeInterval;
        // 创建缩放动画
        Tween scaleTween = coin.transform
            .DOScale(1f, coinAppearDuration)
            .SetEase(appearEase);
        sequence.Insert(startTime, scaleTween);
    }

    /// <summary>
    /// 为金币添加随机的动画偏移，使序列帧动画不同步
    /// </summary>
    private void RandomizeAnimationOffset(GameObject coin)
    {
        // 使用 Animator.Play() 设置标准化时间
        Animator animator = coin.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            float randomOffset = Random.Range(0f, 1f);
            animator.Play(0, 0, randomOffset);
            return;
        }

        // 使用 Animation 组件(旧版动画系统)
        Animation animation = coin.GetComponent<Animation>();
        if (animation != null && animation.clip != null)
        {
            float randomTime = Random.Range(0f, animation.clip.length);
            animation[animation.clip.name].time = randomTime;
            animation.Sample(); // 立即应用时间设置
        }
    }

    /// <summary>
    /// 安全地设置tween位置
    /// </summary>
    private bool SetTweenPosition(float targetTime, float duration)
    {
        // 确保tween引用存在，如果丢失则恢复
        if (virtualSplingMover.tween == null)
        {
            if (cachedTween != null) virtualSplingMover.tween = cachedTween;
            else return false;
        }
        // 限制时间范围并设置位置
        float clampedTime = Mathf.Clamp(targetTime, 0f, duration);
        virtualSplingMover.tween.fullPosition = clampedTime;
        return true;
    }
}