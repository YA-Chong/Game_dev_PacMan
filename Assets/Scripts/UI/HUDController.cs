using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] heartIcons; // 生命值图标数组
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI ghostTimerText;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private Button exitButton;


    private int currentLives;
    private int currentScore;
    private float gameTime;
    private float ghostFrightenedTime;
    private bool isGameRunning;

    void Start()
    {
        // 初始化游戏状态
        currentLives = heartIcons.Length; // 生命值数量 = Heart数组长度
        currentScore = 0;
        gameTime = 0f;
        ghostFrightenedTime = 0f;
        isGameRunning = true;


        // 更新UI显示
        UpdateLivesDisplay(currentLives);
        UpdateScoreDisplay(currentScore);
        UpdateGameTimerDisplay(gameTime);
        UpdateGhostTimerDisplay();

        // 绑定退出按钮
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ReturnToStartScene);
        }

        // 订阅GameManager事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
            GameManager.Instance.OnScoreChanged += UpdateScoreDisplay;
            GameManager.Instance.OnGameTimeChanged += UpdateGameTimerDisplay;
            GameManager.Instance.OnGhostsFrightenedChanged += OnGhostsFrightenedChanged;
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
            GameManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            GameManager.Instance.OnGameTimeChanged -= UpdateGameTimerDisplay;
            GameManager.Instance.OnGhostsFrightenedChanged -= OnGhostsFrightenedChanged;
        }
    }

    void Update()
    {
        if (isGameRunning)
        {
            // 更新游戏计时器
            gameTime += Time.deltaTime;
            UpdateGameTimerDisplay(gameTime);

            // 更新幽灵恐惧计时器
            if (ghostFrightenedTime > 0)
            {
                ghostFrightenedTime -= Time.deltaTime;
                UpdateGhostTimerDisplay();
            }
        }
    }


    // 更新生命值显示
    public void UpdateLivesDisplay(int lives)
    {
        currentLives = lives;
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                heartIcons[i].SetActive(i < currentLives);
            }
        }
    }

    // 更新分数显示
    public void UpdateScoreDisplay(int score)
    {
        currentScore = score;
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("000000");
        }
    }

    // 更新游戏计时器显示
    public void UpdateGameTimerDisplay(float time)
    {
        gameTime = time;
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            int milliseconds = Mathf.FloorToInt((gameTime % 1f) * 100f);
            gameTimerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }

    // 更新幽灵恐惧计时器显示
    public void UpdateGhostTimerDisplay()
    {
        if (ghostTimerText != null)
        {
            if (ghostFrightenedTime > 0)
            {
                ghostTimerText.gameObject.SetActive(true);
                ghostTimerText.text = Mathf.CeilToInt(ghostFrightenedTime).ToString();
            }
            else
            {
                ghostTimerText.gameObject.SetActive(false);
            }
        }
    }


    // 减少生命值
    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLivesDisplay(currentLives);
            
            if (currentLives <= 0)
            {
                // 游戏结束逻辑
                GameOver();
            }
        }
    }

    // 增加分数
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay(currentScore);
    }

    // 开始幽灵恐惧状态
    public void StartGhostFrightenedMode(float duration)
    {
        ghostFrightenedTime = duration;
        UpdateGhostTimerDisplay();
    }

    // 返回开始场景
    public void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    // 游戏结束
    private void GameOver()
    {
        isGameRunning = false;
        Debug.Log("Game Over!");
        // 这里可以添加游戏结束的UI显示
    }

    // 暂停游戏计时器
    public void PauseGameTimer()
    {
        isGameRunning = false;
    }

    // 恢复游戏计时器
    public void ResumeGameTimer()
    {
        isGameRunning = true;
    }

    // 幽灵恐惧状态改变时的回调
    private void OnGhostsFrightenedChanged(bool isFrightened)
    {
        if (isFrightened)
        {
            StartGhostFrightenedMode(10f); // 10秒恐惧时间
        }
        else
        {
            ghostFrightenedTime = 0f;
            UpdateGhostTimerDisplay();
        }
    }
}
