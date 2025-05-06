using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridBorder : MonoBehaviour
{
    [Header("Border Settings")]
    public Color borderColor = Color.gray;
    public float borderWidth = 1f;
    public bool showOuterBorder = true;
    public bool showCellBorders;

    private RectTransform rectTransform;
    private GridLayoutGroup grid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        CreateBorders();
    }

    private void CreateBorders()
    {
        // 创建外边框
        if (showOuterBorder)
        {
            CreateBorderLine(BorderLines.TopBorder, new Vector2(0.5f, 1), new Vector2(rectTransform.rect.width, borderWidth));
            CreateBorderLine(BorderLines.BottomBorder, new Vector2(0.5f, 0), new Vector2(rectTransform.rect.width, borderWidth));
            CreateBorderLine(BorderLines.LeftBorder, new Vector2(0, 0.5f), new Vector2(rectTransform.rect.width, borderWidth));
            CreateBorderLine(BorderLines.RightBorder, new Vector2(1, 0.5f), new Vector2(rectTransform.rect.width, borderWidth));
        }

        // 创建单元格边框
        if (showCellBorders)
        {
            foreach (RectTransform child in transform)
            {
                AddCellBorders(child);
            }
        }
    }

    private void CreateBorderLine(BorderLines line, Vector2 anchor, Vector2 size)
    {
        GameObject border = new GameObject(name + line);
        border.transform.SetParent(transform.parent, false);
        
        Image image = border.AddComponent<Image>();
        image.color = borderColor;

        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.SetAsFirstSibling();
    }

    private void AddCellBorders(RectTransform cell)
    {
        // Cell Top
        CreateCellBorder(cell, new Vector2(0.5f, 1), new Vector2(cell.rect.width, borderWidth));
        // Cell Bottom
        CreateCellBorder(cell, new Vector2(0.5f, 0), new Vector2(cell.rect.width, borderWidth));
        // Cell Left
        CreateCellBorder(cell, new Vector2(0, 0.5f), new Vector2(borderWidth, cell.rect.height));
        // Cell Right
        CreateCellBorder(cell, new Vector2(1, 0.5f), new Vector2(borderWidth, cell.rect.height));
    }

    private void CreateCellBorder(RectTransform parent, Vector2 anchor, Vector2 size)
    {
        GameObject border = new GameObject("cellBorder");
        border.transform.SetParent(parent, false);

        Image image = border.AddComponent<Image>();
        image.color = borderColor;

        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    public void RefreshBorders()
    {
        foreach (Transform child in transform.parent)
        {
            if (child.name.EndsWith("Border")) Destroy(child.gameObject);
        }
        CreateBorders();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}