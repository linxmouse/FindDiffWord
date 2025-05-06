using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SpriteNumberDisplay : MonoBehaviour
{
    [Header("Sprite Asset配置")] public TMP_SpriteAsset spriteAsset; // 关联的数字图集
    public string spriteName = "scoreNum"; // 使用的sprite名称前缀

    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        // 设置spriteAsset
        textMesh.spriteAsset = spriteAsset;
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
        foreach (char c in number)
        {
            if (char.IsDigit(c)) sb.Append($"<sprite=\"{spriteName}\" name=\"{c}\">");
        }

        return sb.ToString();
    }
}