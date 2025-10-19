using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private int currentLives = 3;
    [SerializeField] private int currentScore = 0;
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private bool isGameRunning = false;

    [Header("Ghost State")]
    [SerializeField] private bool ghostsFrightened = false;
    [SerializeField] private float frightenedTimer = 0f;

    // 事件
    public System.Action<int> OnLivesChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action<float> OnGameTimeChanged;
    public System.Action<bool> OnGhostsFrightenedChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (isGameRunning)
        {
            // 更新游戏时间
            gameTime += Time.deltaTime;
            OnGameTimeChanged?.Invoke(gameTime);

            // 更新幽灵恐惧计时器
            if (ghostsFrightened && frightenedTimer > 0)
            {
                frightenedTimer -= Time.deltaTime;
                if (frightenedTimer <= 0)
                {
                    SetGhostsFrightened(false);
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level01")
        {
            StartCoroutine(CoStartLevel01MusicNextFrame());
            StartGame();
        }

        if (scene.name == "Level02")
        {
            StartCoroutine(CoStartLevel01MusicNextFrame());
            StartGame();
        }
    }

    private IEnumerator CoStartLevel01MusicNextFrame()
    {
        yield return null;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StartLevelMusic();
        }
    }

    // 开始游戏
    public void StartGame()
    {
        isGameRunning = true;
        gameTime = 0f;
        currentLives = 3;
        currentScore = 0;
        ghostsFrightened = false;
        frightenedTimer = 0f;
        
        OnLivesChanged?.Invoke(currentLives);
        OnScoreChanged?.Invoke(currentScore);
        OnGameTimeChanged?.Invoke(gameTime);
        OnGhostsFrightenedChanged?.Invoke(ghostsFrightened);
    }

    // 减少生命值
    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            OnLivesChanged?.Invoke(currentLives);
            
            if (currentLives <= 0)
            {
                GameOver();
            }
        }
    }

    // 增加分数
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    // 设置幽灵恐惧状态
    public void SetGhostsFrightened(bool frightened, float duration = 10f)
    {
        ghostsFrightened = frightened;
        if (frightened)
        {
            frightenedTimer = duration;
            EnterScared();
        }
        else
        {
            frightenedTimer = 0f;
            ExitScared();
        }
        OnGhostsFrightenedChanged?.Invoke(ghostsFrightened);
    }

    // 游戏结束
    public void GameOver()
    {
        isGameRunning = false;
        Debug.Log("Game Over! Final Score: " + currentScore);
    }

    // 返回开始场景
    public void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    // 音频相关方法
    public void EnterScared()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchToScaredBGM();
    }

    public void ExitScared()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchBackToNormalBGM();
    }

    public void EnterGhostDie()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchToDeadBGM();
    }

    public void ExitGhostDie()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchBackToNormalBGM();
    }

    // 获取当前状态
    public int GetCurrentLives() => currentLives;
    public int GetCurrentScore() => currentScore;
    public float GetGameTime() => gameTime;
    public bool IsGameRunning() => isGameRunning;
    public bool AreGhostsFrightened() => ghostsFrightened;
    public float GetFrightenedTimer() => frightenedTimer;
}
