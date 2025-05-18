using DG.Tweening;
using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using Unity.Logging;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;
using HidSharp;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    public Transform textContainer;         // 字符父节点
    public GameObject characterPrefab;      // 字符预制体
    public SpriteNumberDisplay scoreNumber; // 得分显示

    [Header("Game Settings")]
    public float restartDelay = 1.0f;   // 重新开始延迟时间
    private int score;                  // 当前得分
    public int totalCharacters = 75;    // 总字符数
    private string currentNormalChar;   // 普通字符
    private string currentDiffChar;     // 差异字符

    // 运行时数据
    private List<TextMeshProUGUI> characters = new List<TextMeshProUGUI>();
    private TextDifference currentDifference;
    private bool isScored = false;      // 避免重复加分

    public static GameController Instance;

    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public string Hobby { get; set; }

        public override string ToString()
        {
            return $"Age = {Age}, Name = {Name}, Hobby = {Hobby}";
        }
    }

    private void Awake()
    {
        #region Newtonsoft.Json测试
        Debug.Log("Newtonsoft.Json Serialize/Deserialize test:");
        string jstr = JsonConvert.SerializeObject(new Person { Age = 18, Name = "zhansan", Hobby = "Programing" });
        Log.Debug(jstr);
        var p = JsonConvert.DeserializeObject<Person>(jstr);
        Log.Debug(p.ToString());
        #endregion

        #region Unity.Logging测试
        // 生成随机姓名
        var name = Faker.Name.FullName();
        Log.Warning(name);
        name = Faker.Name.FullName();
        Log.Warning(name);
        name = Faker.Name.FullName();
        Log.Warning(name);
        // 生成随机电子邮箱地址
        var email = Faker.Internet.Email();
        Log.Info(email);
        email = Faker.Internet.Email();
        Log.Info(email);
        email = Faker.Internet.Email();
        Log.Info(email);
        // 生成随机电话号码
        var phone = Faker.Phone.Number();
        Log.Info(phone);
        phone = Faker.Phone.Number();
        Log.Info(phone);
        phone = Faker.Phone.Number();
        Log.Info(phone);
        phone = Faker.Phone.Number();
        Log.Info(phone);
        // 生成随机地址
        var addr = Faker.Address.ZipCode();
        Log.Info(addr);
        addr = Faker.Address.ZipCode();
        Log.Info(addr);
        addr = Faker.Address.ZipCode();
        Log.Info(addr);
        #endregion

        #region HIDSharp测试
        var list = DeviceList.Local;
        list.Changed += (sender, e) =>
        {
            Log.Warning("Device list changed.");
        };
        var all = DeviceList.Local.GetAllDevices();
        foreach (var dev in all) Log.Info(dev.ToString());
        //// 查找设备
        //var deviceList = DeviceList.Local;
        //var hidDevice = deviceList.GetHidDevices()
        //    .FirstOrDefault(d => d.VendorID == 1133 && d.ProductID == 49948);
        //if (hidDevice != null)
        //{
        //    Log.Debug(hidDevice.GetFriendlyName());
        //    using (var stream = hidDevice.Open())
        //    {
        //        //// 写入数据（自动处理 Report ID）
        //        //byte[] data = new byte[] { 0x01, 0x02, 0x03 };
        //        //stream.Write(data);
        //        ////stream.WriteAsync(data);

        //        // 读取数据
        //        byte[] buffer = new byte[64];
        //        int bytesRead = stream.Read(buffer);
        //        Log.Info($"read from hid data len: {bytesRead}");
        //        //int bytesRead = await stream.ReadAsync(buffer);
        //    }
        //}
        #endregion

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
        if (Random.value > 0.5f) GenerateChinesePair();
        else GenerateAlphabetPair();
    }

    // 生成字母差异对
    private void GenerateAlphabetPair()
    {
        // 随机生成大写字母（A-Z）
        char upperChar = (char)Random.Range('A', 'Z' + 1);
        currentNormalChar = upperChar.ToString();
        currentDiffChar = upperChar.ToString().ToLower();
    }
    // 生成汉字差异对
    private void GenerateChinesePair()
    {
        var pair = ChinesePair.ChinesePairs[Random.Range(0, ChinesePair.ChinesePairs.Count)];
        currentNormalChar = pair[0];
        currentDiffChar = pair[1];
        // 随机交换正确位置（增强认知）
        if (Random.value > 0.5f) (currentNormalChar, currentDiffChar) = (currentDiffChar, currentNormalChar);
    }

    private void RestartGame()
    {
        // 生成新字符对
        GenerateNewCharacterPair();
        // 重置所有字符显示
        foreach (var tmp in characters)
        {
            tmp.text = currentNormalChar;
        }
        // 创建新差异点
        CreateDifference();
        
        isScored = false;
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
    
    public void OnCharacterClicked(int index)
    {
        if (isScored) return;
        if (index == currentDifference.charIndex)
        {
            isScored = true;
            HighlightCharacter(index);
            // 得分逻辑
            score += 100;
            scoreNumber.SetNumber(score);
            // 延迟后重新开始游戏
            Invoke("RestartGame", restartDelay);
        }
    }

    void HighlightCharacter(int index)
    {
        TextMeshProUGUI tmp = characters[index];
        // 播放动画
        PlayStrokeAnimation(tmp);
    }

    private void PlayStrokeAnimation(TextMeshProUGUI tmp)
    {
        // 强制生成最新网格数据
        tmp.ForceMeshUpdate(true);
        // 记录原始数据（位置+颜色）
        var originalData = new
        {
            positions = new List<Vector3>(),
            colors = new List<Color32>(),
            localPosition = tmp.transform.localPosition
        };
        TMP_TextInfo textInfo = tmp.textInfo;
        // 遍历所有字符的每个顶点
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int materialIndex = charInfo.materialReferenceIndex;
            // 记录顶点位置
            Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;
            for (int j = 0; j < 4; j++)
            {
                originalData.positions.Add(verts[charInfo.vertexIndex + j]);
            }
            // 记录顶点颜色
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            for (int j = 0; j < 4; j++)
            {
                originalData.colors.Add(colors[charInfo.vertexIndex + j]);
            }
        }
        
        // 创建动画序列
        DOTween.Sequence()
            .Append(tmp.transform.DOShakePosition(
                duration: 0.5f,
                strength: 3f,
                vibrato: 10,
                randomness: 90f,
                snapping: false
            ))
            .Join(tmp.DOColor(Color.green, 0.3f))
            .OnComplete(() =>
            {
                // 重置所有状态
                ResetVertices(tmp, originalData.positions, originalData.colors);
                tmp.transform.localPosition = originalData.localPosition;
            });
    }

    private void ResetVertices(TextMeshProUGUI tmp, List<Vector3> positions, List<Color32> colors)
    {
        TMP_TextInfo textInfo = tmp.textInfo;
        int posIndex = 0;
        int colorIndex = 0;
        // 遍历所有字符
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int materialIndex = charInfo.materialReferenceIndex;
            // 重置顶点位置
            Vector3[] verts = textInfo.meshInfo[materialIndex].vertices;
            for (int j = 0; j < 4; j++)
            {
                if (posIndex < positions.Count)
                {
                    verts[charInfo.vertexIndex + j] = positions[posIndex++];
                }
            }
            // 重置顶点颜色
            Color32[] colorArray = textInfo.meshInfo[materialIndex].colors32;
            for (int j = 0; j < 4; j++)
            {
                if (colorIndex < colors.Count)
                {
                    colorArray[charInfo.vertexIndex + j] = colors[colorIndex++];
                }
            }
        }
        // 更新所有数据
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        tmp.color = Color.white;
    }

    private void OnDestroy()
    {
        // 关闭HID设备
        DeviceList.Local.Shutdown();
    }
}
