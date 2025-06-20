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

    private float _spawnElapsedTime;

    private SpriteRenderer _renderer;
    private List<GameObject> _activedTrails = new List<GameObject>();
    private Queue<GameObject> _trailPool = new Queue<GameObject>();

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        // 如果残影层级为默认的-1则使用渲染器的层级-1
        if (orderInLayer == -1) orderInLayer = _renderer.sortingOrder - 1;
    }

    private void Update()
    {
        if (!enableTrail) return;

        DrawGhost();
        Fade();
        CleanDestoryedGhosts();
    }

    /// <summary>
    /// 清理已销毁的残影
    /// </summary>
    private void CleanDestoryedGhosts()
    {
        for (int i = _activedTrails.Count - 1; i >= 0; i--)
        {
            if (_activedTrails[i] == null)
            {
                _activedTrails.RemoveAt(i);
            }
        }
    }

    private void DrawGhost()
    {
        _spawnElapsedTime += Time.deltaTime;
        if (_spawnElapsedTime < spawnInterval) return;

        _spawnElapsedTime = 0;
        GameObject ghost = GetGhost();
        _activedTrails.Add(ghost);

        if (!enableFade)
        {
            // Destroy(ghost, durationTime);
            StartCoroutine(DestoryGhostAfterDelay(ghost, durationTime));
        }
    }

    /// <summary>
    /// 延迟销毁残影
    /// </summary>
    /// <param name="ghost">残影</param>
    /// <param name="durationTime">残影持续时间</param>
    private IEnumerator DestoryGhostAfterDelay(GameObject ghost, float durationTime)
    {
        yield return new WaitForSeconds(durationTime);
        if (ghost != null) Destroy(ghost);
    }

    private GameObject GetGhost()
    {
        GameObject ghost;
        if (_trailPool.Count > 0)
        {
            ghost = _trailPool.Dequeue();
            ghost.SetActive(true);
        }
        else
        {
            ghost = new GameObject("Ghost");
            ghost.AddComponent<SpriteRenderer>();
        }

        SetupGhost(ghost);
        return ghost;
    }

    private void SetupGhost(GameObject ghost)
    {
        ghost.transform.position = transform.position;
        ghost.transform.localScale = transform.localScale;
        ghost.transform.rotation = transform.rotation;

        SpriteRenderer gsr = ghost.GetComponent<SpriteRenderer>();
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