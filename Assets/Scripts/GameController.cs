using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    public Transform textContainer;     // 字符父节点
    public GameObject characterPrefab;  // 字符预制体

    [Header("Game Settings")] 
    public float restartDelay = 1.0f;   // 重新开始延迟时间
    private int score;                  // 当前得分
    public int totalCharacters = 75;    // 总字符数
    private string currentNormalChar;   // 普通字符
    private string currentDiffChar;     // 差异字符
    
    // 运行时数据
    private List<TextMeshProUGUI> characters = new List<TextMeshProUGUI>();
    private TextDifference currentDifference;
    
    public static GameController Instance;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeCharacters();
        RestartGame();
    }
    
    // 初始化字符对象（只需要执行一次）
    private void InitializeCharacters()
    {
        foreach (Transform child in textContainer) Destroy(child.gameObject);
        characters.Clear();
        for (int i = 0; i < totalCharacters; i++)
        {
            GameObject charObj = Instantiate(characterPrefab, textContainer);
            charObj.name = $"Char_{i}";
            TextMeshProUGUI tmp = charObj.GetComponent<TextMeshProUGUI>();
            characters.Add(tmp);
            CharacterClickHandler clickHandler = charObj.GetComponent<CharacterClickHandler>();
            clickHandler.Init(i);
        }
    }
    
    private void GenerateNewCharacterPair()
    {
        // 随机生成大写字母（A-Z）
        char upperChar = (char)Random.Range('A', 'Z' + 1);
        currentNormalChar = upperChar.ToString();
        currentDiffChar = upperChar.ToString().ToLower();
    }
    
    private void RestartGame()
    {
        // 生成新字符对
        GenerateNewCharacterPair();
        // 重置所有字符显示
        foreach (var tmp in characters)
        {
            tmp.text = currentNormalChar;
            // tmp.ForceMeshUpdate(true);
            ResetCharacterColor(tmp);
        }
        // 创建新差异点
        CreateDifference();
    }

    private void ResetCharacterColor(TextMeshProUGUI tmp)
    {
        TMP_CharacterInfo charInfo = tmp.textInfo.characterInfo[0];
        int vertexIndex = charInfo.vertexIndex;
        Color32[] vertexColors = tmp.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
        if (vertexColors == null) return;
        Color32 defaultColor = new Color32(255, 255, 255, 255);
        vertexColors[vertexIndex] = defaultColor;
        vertexColors[vertexIndex + 1] = defaultColor;
        vertexColors[vertexIndex + 2] = defaultColor;
        vertexColors[vertexIndex + 3] = defaultColor;
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void CreateDifference()
    {
        // 随机选择差异位置
        int diffIndex = Random.Range(0, totalCharacters);
        characters[diffIndex].text = currentDiffChar;
        currentDifference = new TextDifference()
        {
            charIndex = diffIndex,
            correctChar = currentNormalChar,
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCharacterClicked(int index)
    {
        if (index == currentDifference.charIndex)
        {
            HighlightCharacter(index);
            // 得分逻辑
            score += 100;
            Debug.Log($"Score: {score}");
            // 延迟后重新开始游戏
            Invoke("RestartGame", restartDelay);
        }
    }

    void HighlightCharacter(int index)
    {
        TextMeshProUGUI tmp = characters[index];
        // 修改顶点颜色
        TMP_CharacterInfo charInfo = tmp.textInfo.characterInfo[0];
        int vertexIndex = charInfo.vertexIndex;
        // 获取颜色数组
        Color32[] vertexColors = tmp.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
        // 设置绿色高亮
        vertexColors[vertexIndex] = new Color32(0, 255, 0, 255);
        vertexColors[vertexIndex + 1] = new Color32(0, 255, 0, 255);
        vertexColors[vertexIndex + 2] = new Color32(0, 255, 0, 255);
        vertexColors[vertexIndex + 3] = new Color32(0, 255, 0, 255);
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
