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

    private void Start(){
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

    private void UpdateHighScoreDisplay()
    {
        // 暂时显示默认值，后续连接真实数据
        if (highScoreTextLevel1 != null)
            highScoreTextLevel1.text = "Best: 000000 - 00:00:00";
            
        if (highScoreTextLevel2 != null)  
            highScoreTextLevel2.text = "Best: 000000 - 00:00:00";
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
}
