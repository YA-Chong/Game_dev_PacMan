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
    
    [Header("Countdown")]
    [SerializeField] private bool isCountdownActive = false;
    [SerializeField] private int countdownValue = 3;
    [SerializeField] private float countdownTimer = 0f;
    [SerializeField] private float goDisplayTimer = 0f; // GO!显示计时器
    
    [Header("Game Timer")]
    [SerializeField] private bool isGameTimerActive = false; // 游戏计时器是否激活

    // 事件
    public System.Action<int> OnLivesChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action<float> OnGameTimeChanged;
    public System.Action<bool> OnGhostsFrightenedChanged;
    public System.Action<int> OnCountdownChanged;
    public System.Action OnCountdownFinished;

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
        
        // 强制初始化游戏状态
        isGameRunning = false;
        isGameTimerActive = false;
        gameTime = 0f;
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
        // 处理倒计时
        if (isCountdownActive)
        {
            countdownTimer += Time.deltaTime;
            
            if (countdownTimer >= 1f)
            {
                countdownValue--;
                countdownTimer = 0f;
                
                if (countdownValue > 0)
                {
                    OnCountdownChanged?.Invoke(countdownValue);
                }
                else if (countdownValue == 0)
                {
                    OnCountdownChanged?.Invoke(0); // 显示"GO!"
                    countdownValue = -1; // 标记为GO!状态
                    goDisplayTimer = 0f; // 重置GO!显示计时器
                    Debug.Log("GameManager: 显示GO!");
                }
            }
            
            // GO!显示计时器独立累加
            if (countdownValue == -1)
            {
                goDisplayTimer += Time.deltaTime;
                if (goDisplayTimer >= 0.8f) // 显示0.8秒后结束
                {
                    Debug.Log("GameManager: GO!显示0.8秒，结束倒计时");
                    FinishCountdown();
                }
            }
        }
        
        if (isGameRunning && isGameTimerActive)
        {
            // 更新游戏时间
            gameTime += Time.deltaTime;
            OnGameTimeChanged?.Invoke(gameTime);

            // 更新幽灵恐惧计时器
            if (ghostsFrightened && frightenedTimer > 0)
            {
                frightenedTimer -= Time.deltaTime;
                
                // 剩余3秒时切换到恢复状态
                if (frightenedTimer <= 3f && frightenedTimer > 0f)
                {
                    // 只在第一次进入恢复状态时切换
                    if (!IsInRecoveringState())
                    {
                        SetAllGhostsState("IsRecovering", true);
                        SetAllGhostsState("IsScared", false);
                        SetAllGhostsState("IsNormal", false);
                    }
                }
                
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
        isGameRunning = false; // 倒计时期间游戏不运行
        isGameTimerActive = false; // 倒计时期间计时器不运行
        gameTime = 0f; // 重置游戏时间
        currentLives = 3;
        currentScore = 0;
        ghostsFrightened = false;
        frightenedTimer = 0f;
        
        
        OnLivesChanged?.Invoke(currentLives);
        OnScoreChanged?.Invoke(currentScore);
        OnGameTimeChanged?.Invoke(gameTime);
        OnGhostsFrightenedChanged?.Invoke(ghostsFrightened);
        
        // 延迟启动倒计时，确保UI已准备好
        StartCoroutine(StartCountdownDelayed());
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
        
        // 显示Game Over UI
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            hudController.ShowGameOver();
        }
        
        // 保存最高分和时间
        SaveHighScore();
        
        // 延迟3秒后返回开始场景
        StartCoroutine(ReturnToStartSceneDelayed());
    }
    
    // 检查是否所有豆子都被吃完
    public void CheckAllPelletsEaten()
    {
        // 使用LayerMask检查普通豆子（Layer 8: Pellet）
        GameObject[] pellets = FindGameObjectsByLayer(8);
        // 使用LayerMask检查能量豆（Layer 10: PowerPill）
        GameObject[] powerPills = FindGameObjectsByLayer(10);
        
        // Debug.Log($"检查豆子：普通豆子剩余 {pellets.Length} 个，能量豆剩余 {powerPills.Length} 个");
        
        // 显示剩余的豆子对象名称
        // if (pellets.Length > 0)
        // {
        //     Debug.Log("剩余的普通豆子：");
        //     foreach (GameObject pellet in pellets)
        //     {
        //         Debug.Log($"- {pellet.name} (位置: {pellet.transform.position})");
        //     }
        // }
        
        // if (powerPills.Length > 0)
        // {
        //     Debug.Log("剩余的能量豆：");
        //     foreach (GameObject powerPill in powerPills)
        //     {
        //         Debug.Log($"- {powerPill.name} (位置: {powerPill.transform.position})");
        //     }
        // }
        
        if (pellets.Length == 0 && powerPills.Length == 0)
        {
            Debug.Log("所有豆子都被吃完了！游戏胜利！");
            GameOver();
        }
    }
    
    // 根据Layer查找GameObject
    private GameObject[] FindGameObjectsByLayer(int layer)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        System.Collections.Generic.List<GameObject> layerObjects = new System.Collections.Generic.List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
            {
                layerObjects.Add(obj);
            }
        }
        
        return layerObjects.ToArray();
    }
    
    // 延迟返回开始场景
    private System.Collections.IEnumerator ReturnToStartSceneDelayed()
    {
        yield return new WaitForSeconds(3f);
        ReturnToStartScene();
    }

    // 返回开始场景
    public void ReturnToStartScene()
    {
        Debug.Log("GameManager: 正在加载StartScene...");
        SceneManager.LoadScene("StartScene");
    }
    
    // 延迟启动倒计时
    private System.Collections.IEnumerator StartCountdownDelayed()
    {
        // 等待几帧确保所有UI组件都已初始化
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("GameManager: 延迟启动倒计时");
        StartCountdown();
    }
    
    // 开始倒计时
    private void StartCountdown()
    {
        isCountdownActive = true;
        countdownValue = 3;
        countdownTimer = 0f;
        
        // 立即显示"3"
        OnCountdownChanged?.Invoke(countdownValue);
        
        Debug.Log("GameManager: 开始倒计时，显示3");
    }
    
    // 倒计时完成
    private void FinishCountdown()
    {
        Debug.Log("GameManager: 倒计时完成，隐藏UI");
        isCountdownActive = false;
        isGameRunning = true;
        isGameTimerActive = true; // 开始游戏计时器
        OnCountdownFinished?.Invoke();
    }
    

    // 音频相关方法
    public void EnterScared()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchToScaredBGM();
        
        // 设置所有幽灵为恐惧状态
        SetAllGhostsState("IsScared", true);
    }

    public void ExitScared()
    {
        if (AudioManager.Instance == null)
            return;
        
        // 检查是否有幽灵处于Dead状态
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        bool hasDeadGhost = false;
        
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.GetCurrentState() == GhostController.GhostState.Dead)
            {
                hasDeadGhost = true;
                break;
            }
        }
        
        // 只有当没有Dead幽灵时，才切换BGM
        if (!hasDeadGhost)
        {
            AudioManager.Instance.SwitchBackToNormalBGM();
            Debug.Log("GameManager: Scared结束，切换到Normal BGM");
        }
        else
        {
            Debug.Log("GameManager: Scared结束，但仍有Dead幽灵，保持Dead BGM");
        }
        
        // 重置所有幽灵状态为正常（Dead幽灵会被SetAllGhostsState跳过）
        SetAllGhostsState("IsNormal", true);
        SetAllGhostsState("IsScared", false);
        SetAllGhostsState("IsRecovering", false);
        SetAllGhostsState("IsDead", false);
    }
    
    // 设置所有幽灵的动画状态
    private void SetAllGhostsState(string stateName, bool value)
    {
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        
        foreach (GhostController ghost in ghosts)
        {
            // 跳过Dead状态的幽灵（它们正在返回初始位置，不应被打断）
            if (ghost.GetCurrentState() == GhostController.GhostState.Dead)
            {
                continue;
            }
            
            // 根据状态名称调用对应的SetGhostState方法
            if (stateName == "IsScared" && value)
            {
                ghost.SetGhostState(GhostController.GhostState.Scared);
            }
            else if (stateName == "IsRecovering" && value)
            {
                ghost.SetGhostState(GhostController.GhostState.Recovering);
            }
            else if (stateName == "IsNormal" && value)
            {
                ghost.SetGhostState(GhostController.GhostState.Normal);
            }
            else if (stateName == "IsDead" && value)
            {
                ghost.SetGhostState(GhostController.GhostState.Dead);
            }
            else
            {
                // 直接设置Animator参数（用于false值）
                Animator animator = ghost.animator != null ? ghost.animator : ghost.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetBool(stateName, value);
                }
            }
        }
    }
    
    // 检查是否在恢复状态
    private bool IsInRecoveringState()
    {
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (GhostController ghost in ghosts)
        {
            Animator animator = ghost.animator != null ? ghost.animator : ghost.GetComponentInChildren<Animator>();
            if (animator != null && animator.GetBool("IsRecovering"))
            {
                return true;
            }
        }
        return false;
    }

    public void EnterGhostDie()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchToDeadBGM();
    }

    public void ExitGhostDie()
    {
        Debug.Log("GameManager: ExitGhostDie() 被调用");
        
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("GameManager: AudioManager.Instance 为空，无法切换BGM");
            return;
        }
        
        // 检查是否还有其他幽灵处于Dead状态
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        bool hasDeadGhost = false;
        
        foreach (GhostController ghost in ghosts)
        {
            GhostController.GhostState state = ghost.GetCurrentState();
            Debug.Log($"GameManager: 检查幽灵 {ghost.ghostNumber}，状态={state}");
            
            if (state == GhostController.GhostState.Dead)
            {
                hasDeadGhost = true;
                break;
            }
        }
        
        Debug.Log($"GameManager: hasDeadGhost={hasDeadGhost}, ghostsFrightened={ghostsFrightened}");
        
        // 只有当没有幽灵处于Dead状态时，才恢复正常BGM
        if (!hasDeadGhost)
        {
            // 如果幽灵仍然处于Scared/Recovering状态，切换到Scared BGM
            if (ghostsFrightened)
            {
                AudioManager.Instance.SwitchToScaredBGM();
                Debug.Log("GameManager: 所有幽灵重生，恢复Scared BGM");
            }
            else
            {
                AudioManager.Instance.SwitchBackToNormalBGM();
                Debug.Log("GameManager: 所有幽灵重生，恢复Normal BGM");
            }
        }
        else
        {
            Debug.Log("GameManager: 仍有Dead幽灵，保持Dead BGM");
        }
    }

    // 获取当前状态
    public int GetCurrentLives() => currentLives;
    public int GetCurrentScore() => currentScore;
    public float GetGameTime() => gameTime;
    public bool IsGameRunning() => isGameRunning;
    public bool AreGhostsFrightened() => ghostsFrightened;
    public float GetFrightenedTimer() => frightenedTimer;
    
    // 倒计时相关方法
    public bool IsCountdownActive() => isCountdownActive;
    public int GetCountdownValue() => countdownValue;
    
    // 最高分相关方法
    public void SaveHighScore()
    {
        // 保存Level 1的最高分（当前关卡）
        int level1HighScore = PlayerPrefs.GetInt("Level1_HighScore", 0);
        float level1BestTime = PlayerPrefs.GetFloat("Level1_BestTime", float.MaxValue);
        
        // 如果当前分数更高，或者分数相同但时间更短
        if (currentScore > level1HighScore || 
            (currentScore == level1HighScore && gameTime < level1BestTime))
        {
            PlayerPrefs.SetInt("Level1_HighScore", currentScore);
            PlayerPrefs.SetFloat("Level1_BestTime", gameTime);
            PlayerPrefs.Save();
            Debug.Log($"新的Level 1最高分！分数: {currentScore}, 时间: {gameTime:F2}秒");
        }
        else
        {
            Debug.Log($"未打破Level 1记录。当前: {currentScore}, 最高: {level1HighScore}");
        }
    }
    
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("Level1_HighScore", 0);
    }
    
    public float GetBestTime()
    {
        return PlayerPrefs.GetFloat("Level1_BestTime", 0f);
    }
    
    // 游戏计时器控制方法
    public void PauseGameTimer()
    {
        isGameTimerActive = false;
    }
    
    public void ResumeGameTimer()
    {
        isGameTimerActive = true;
    }
    
    public bool IsGameTimerActive()
    {
        return isGameTimerActive;
    }
}
