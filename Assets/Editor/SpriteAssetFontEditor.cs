using UnityEditor;
using TMPro;
using System.Text;

[CustomEditor(typeof(SpriteAssetFont))]
public class SpriteAssetFontEditor : Editor
{
    private string previewText = "";
    private SpriteAssetFont script;
    private TextMeshProUGUI textMesh;
    private string fontResourceName;

    private void OnEnable()
    {
        script = (SpriteAssetFont)target;
        textMesh = script.GetComponent<TextMeshProUGUI>();
        if (script.spriteAsset != null)
        {
            fontResourceName = script.spriteAsset.name;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (script.spriteAsset == null)
        {
            EditorGUILayout.HelpBox("请先在上方设置Sprite Asset", MessageType.Warning);
            return;
        }
        
        if (script.spriteAsset.name != fontResourceName)
        {
            fontResourceName = script.spriteAsset.name;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("编辑器预览", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        previewText = EditorGUILayout.TextField("输入预览文本", previewText);

        if (EditorGUI.EndChangeCheck())
        {
            if (textMesh != null)
            {
                textMesh.spriteAsset = script.spriteAsset;
                textMesh.text = ConvertToSpriteTags(previewText);
                // 标记场景已修改，这样可以保存更改
                EditorUtility.SetDirty(textMesh);
            }
        }
    }

    private string ConvertToSpriteTags(string number)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in number)
        {
            sb.Append($"<sprite=\"{fontResourceName}\" name=\"{c}\">");
        }
        return sb.ToString();
    }
} 