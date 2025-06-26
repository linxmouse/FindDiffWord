using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image的UV裁剪控制器
/// 实现精确的边界裁剪效果
/// </summary>
[RequireComponent(typeof(Image))]
[ExecuteAlways] // 支持编辑模式
public class ImageUVCrop : MonoBehaviour
{
    [Header("裁剪边界控制")]
    [Range(0f, 1f)]
    public float cropLeft = 0f;     // 从左边裁切的量
    [Range(0f, 1f)]
    public float cropBottom = 0f;      // 从上边裁切的量
    [Range(0f, 1f)]
    public float cropRight = 0f;    // 从右边裁切的量
    [Range(0f, 1f)]
    public float cropTop = 0f;   // 从下边裁切的量

    private Image targetImage;      // 目标Image组件
    private Material materialInstance; // 材质实例

    void Start()
    {
        InitializeMaterial();
        UpdateCrop();
    }

    /// <summary>
    /// 编辑器中参数改变时自动调用
    /// </summary>
    void OnValidate()
    {
        // 确保在编辑模式下材质已初始化
        if (!Application.isPlaying && materialInstance == null) InitializeMaterial();
        UpdateCrop();
    }

    /// <summary>
    /// 初始化材质实例
    /// </summary>
    private void InitializeMaterial()
    {
        targetImage = GetComponent<Image>();
        if (targetImage == null)
        {
            Debug.LogError($"ImageUVCrop: 在 {gameObject.name} 上未找到 Image 组件");
            return;
        }

        // 如果当前使用的是默认材质，创建自定义材质
        if (targetImage.material == null || targetImage.material.name.Contains("Default"))
        {
            // 尝试加载自定义的UVCrop Shader
            Shader uvCropShader = Shader.Find("Custom/UVCrop");
            if (uvCropShader == null)
            {
                Debug.LogError($"ImageUVCrop: 未找到 Custom/UVCrop Shader, 请确保Shader文件在项目中");
                return;
            }
            materialInstance = new Material(uvCropShader);
        }
        else materialInstance = new Material(targetImage.material); // 创建现有材质的实例            
        if (targetImage.sprite != null) materialInstance.mainTexture = targetImage.sprite.texture; // 设置纹理
        targetImage.material = materialInstance;
    }

    /// <summary>
    /// 更新裁剪区域
    /// </summary>
    public void UpdateCrop()
    {
        if (materialInstance == null) return;
        Vector4 uvRect = new Vector4(
            cropLeft,                   // x 从左边裁切的量
            cropBottom,                 // y 从下边裁切的量
            1.0f - cropRight,           // 1-z 从右边裁切的量
            1.0f - cropTop              // 1-w 从上边裁切的量
        );
        // 设置裁剪区域
        materialInstance.SetVector("_UVRect", uvRect);
    }

    /// <summary>
    /// 设置裁剪区域
    /// </summary>
    /// <param name="left">从左边裁切的量（0-1）</param>
    /// <param name="bottom">从下边裁切的量（0-1）</param>
    /// <param name="right">从右边裁切的量（0-1）</param>
    /// <param name="top">从上边裁切的量（0-1）</param>
    public void SetCropArea(float left, float bottom, float right, float top)
    {
        cropLeft = Mathf.Clamp01(left);
        cropBottom = Mathf.Clamp01(bottom);
        cropRight = Mathf.Clamp01(right);
        cropTop = Mathf.Clamp01(top);
        UpdateCrop();
    }

    /// <summary>
    /// 动画过渡到新的裁剪区域
    /// </summary>
    public void AnimateToCropArea(float targetLeft, float targetBottom, float targetRight, float targetTop, float duration = 1f)
    {
        StartCoroutine(AnimateCropCoroutine(targetLeft, targetBottom, targetRight, targetTop, duration));
    }
    /// <summary>
    /// 裁剪动画协程
    /// </summary>
    private IEnumerator AnimateCropCoroutine(float targetLeft, float targetBottom, float targetRight, float targetTop, float duration)
    {
        float startLeft = cropLeft;
        float startTop = cropBottom;
        float startRight = cropRight;
        float startBottom = cropTop;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            // 使用缓动函数让动画更平滑
            cropLeft = Mathf.Lerp(startLeft, targetLeft, progress);
            cropBottom = Mathf.Lerp(startTop, targetTop, progress);
            cropRight = Mathf.Lerp(startRight, targetRight, progress);
            cropTop = Mathf.Lerp(startBottom, targetBottom, progress);
            UpdateCrop();
            yield return null;
        }
        // 确保最终值准确
        cropLeft = targetLeft;
        cropBottom = targetTop;
        cropRight = targetRight;
        cropTop = targetBottom;
        UpdateCrop();
    }

    /// <summary>
    /// 清理材质实例，避免内存泄漏
    /// </summary>
    void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }
}