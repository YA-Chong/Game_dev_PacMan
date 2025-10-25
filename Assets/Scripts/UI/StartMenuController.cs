using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text highScoreTextLevel1;
    [SerializeField] private TMP_Text highScoreTextLevel2;
    
    [Header("Button References")] 
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        Debug.Log("StartMenuController Start() 被调用");
        
        // 初始化高分显示
        UpdateHighScoreDisplay();
        
        // 确保有GameManager
        if (GameManager.Instance == null)
        {
            InstantiateGameManager();
        }
        
        // 绑定按钮事件
        if (level1Button != null)
            level1Button.onClick.AddListener(StartLevel01);
            
        if (level2Button != null)  
            level2Button.onClick.AddListener(StartLevel02);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }
    
    private void OnDestroy()
    {
        // 清理事件绑定
        if (level1Button != null)
            level1Button.onClick.RemoveListener(StartLevel01);
            
        if (level2Button != null)  
            level2Button.onClick.RemoveListener(StartLevel02);
            
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    public void StartLevel01()
    {
        SceneManager.LoadScene("Level01", LoadSceneMode.Single);
    }
    
    public void StartLevel02()
    {
        // 先检查Level02是否存在，避免错误
        if (Application.CanStreamedLevelBeLoaded("Level02"))
        {
            SceneManager.LoadScene("Level02", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Level02 not found! Loading Level01 instead.");
            SceneManager.LoadScene("Level01", LoadSceneMode.Single);
        }
    }

    public void UpdateHighScoreDisplay()
    {
        // 从PlayerPrefs加载Level 1的最高分数据
        int level1HighScore = PlayerPrefs.GetInt("Level1_HighScore", 0);
        float level1BestTime = PlayerPrefs.GetFloat("Level1_BestTime", 0f);
        
        // 格式化时间显示 (mm:ss:msms)
        string level1TimeString = FormatTime(level1BestTime);
        
        if (highScoreTextLevel1 != null)
            highScoreTextLevel1.text = $"Best: {level1HighScore:D6} - {level1TimeString}";
            
        // 隐藏Level 2的最高分显示（因为不做100%）
        if (highScoreTextLevel2 != null)  
            highScoreTextLevel2.gameObject.SetActive(false);
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds % 1f) * 100f);
        
        return $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}";
    }
    
    private void InstantiateGameManager()
    {
        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}