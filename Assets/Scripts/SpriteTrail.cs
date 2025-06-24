using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 残影效果组件
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteTrail : MonoBehaviour
{
    [Header("开启残影效果")]
    public bool enableTrail = true;

    [Header("启用淡出消失")]
    public bool enableFade = true;

    [Header("残影持续时间")]
    public float durationTime = 1f;
    [Header("残影生成间隔")]
    public float spawnInterval = 0.1f;
    [Header("残影颜色")]
    public Color trailColor = new Color(1, 1, 1, 0.5f);
    [Header("残影层级")]
    public int orderInLayer = -1;
    
    [Header("移动检测")]
    [Tooltip("位置变化阈值，小于此值认为没有移动")]
    public float positionThreshold = 0.01f;
    [Tooltip("旋转变化阈值，小于此值认为没有旋转")]
    public float rotationThreshold = 0.1f;

    private float _spawnElapsedTime;
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private bool _isMoving = false;

    private SpriteRenderer _renderer;
    private List<GameObject> _activedTrails = new List<GameObject>();
    private Queue<GameObject> _trailPool = new Queue<GameObject>();

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        // 如果残影层级为默认的-1则使用渲染器的层级-1
        if (orderInLayer == -1) orderInLayer = _renderer.sortingOrder - 1;       
        // 初始化位置和旋转记录
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    private void Update()
    {
        if (!enableTrail) return;
        // 检测是否在移动
        CheckMovement();       
        // 只有在移动时才生成残影
        if (_isMoving) DrawTrail();
        Fade();
        CleanDestoryedTrails();
    }

    /// <summary>
    /// 检测物体是否在移动或旋转
    /// </summary>
    private void CheckMovement()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;       
        // 检查位置变化
        bool positionChanged = Vector3.Distance(currentPosition, _lastPosition) > positionThreshold;
        // 检查旋转变化
        bool rotationChanged = Quaternion.Angle(currentRotation, _lastRotation) > rotationThreshold;  
        _isMoving = positionChanged || rotationChanged;   
        // 更新记录的位置和旋转
        if (_isMoving)
        {
            _lastPosition = currentPosition;
            _lastRotation = currentRotation;
        }
    }

    /// <summary>
    /// 清理已销毁的残影
    /// </summary>
    private void CleanDestoryedTrails()
    {
        for (int i = _activedTrails.Count - 1; i >= 0; i--)
        {
            if (_activedTrails[i] == null) _activedTrails.RemoveAt(i);
        }
    }

    private void DrawTrail()
    {
        _spawnElapsedTime += Time.deltaTime;
        if (_spawnElapsedTime < spawnInterval) return;
        _spawnElapsedTime = 0;
        GameObject trail = GetTrail();
        _activedTrails.Add(trail);
        if (!enableFade) StartCoroutine(DestoryTrailAfterDelay(trail, durationTime));
    }

    /// <summary>
    /// 延迟销毁残影
    /// </summary>
    /// <param name="trail">残影</param>
    /// <param name="durationTime">残影持续时间</param>
    private IEnumerator DestoryTrailAfterDelay(GameObject trail, float durationTime)
    {
        yield return new WaitForSeconds(durationTime);
        if (trail != null) Destroy(trail);
    }

    private GameObject GetTrail()
    {
        GameObject trail;
        if (_trailPool.Count > 0)
        {
            trail = _trailPool.Dequeue();
            trail.SetActive(true);
        }
        else
        {
            trail = new GameObject($"{name}-Trail");
            trail.AddComponent<SpriteRenderer>();
        }

        SetupTrail(trail);
        return trail;
    }

    private void SetupTrail(GameObject trail)
    {
        trail.transform.position = transform.position;
        trail.transform.localScale = transform.localScale;
        trail.transform.rotation = transform.rotation;

        SpriteRenderer gsr = trail.GetComponent<SpriteRenderer>();
        gsr.sprite = _renderer.sprite;
        gsr.sortingOrder = orderInLayer;
        gsr.color = trailColor;
    }

    private void Fade()
    {
        if (!enableFade) return;

        for (int i = _activedTrails.Count - 1; i >= 0; i--)
        {
            GameObject trail = _activedTrails[i];
            SpriteRenderer renderer = trail.GetComponent<SpriteRenderer>();

            Color tempColor = renderer.color;
            tempColor.a -= (trailColor.a / durationTime) * Time.deltaTime;
            renderer.color = tempColor;

            if (tempColor.a <= 0)
            {
                ReturnGhostToPool(trail);
                _activedTrails.RemoveAt(i);
            }
        }
    }

    private void ReturnGhostToPool(GameObject trail)
    {
        trail.SetActive(false);
        _trailPool.Enqueue(trail);
    }

    public void ResetAllGhosts()
    {
        for (int i = _activedTrails.Count - 1; i >= 0; i--)
        {
            GameObject trail = _activedTrails[i];
            if (trail != null) ReturnGhostToPool(trail);
        }
        _activedTrails.Clear();
    }

    private void OnDisable()
    {
        ResetAllGhosts();
    }

    private void OnDestroy()
    {
        ResetAllGhosts();
    }
}