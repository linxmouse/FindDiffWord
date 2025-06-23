using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SpriteAssetFont : MonoBehaviour
{
    [Header("精灵字体资源")]
    public TMP_SpriteAsset spriteAsset; // 关联的数字图集

    private string fontResourceName; // 字体资源名称

    [HideInInspector]
    public TextMeshProUGUI textMesh; // 文本组件

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (spriteAsset == null)
        {
            Debug.LogWarning("SpriteAssetFont: spriteAsset is null, please set it in the inspector");
            return;
        }
        textMesh.spriteAsset = spriteAsset;
        fontResourceName = spriteAsset.name;
    }

    // 数字显示方法
    public void SetNumber(string number)
    {
        textMesh.text = Convert2SpriteTags(number);
    }

    public void SetNumber(int number)
    {
        SetNumber(number.ToString());
    }

    private string Convert2SpriteTags(string number)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in number) sb.Append($"<sprite=\"{fontResourceName}\" name=\"{c}\">");

        return sb.ToString();
    }
}