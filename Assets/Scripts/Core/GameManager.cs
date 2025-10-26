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
    [SerializeField] private float goDisplayTimer = 0f; 
    
    [Header("Game Timer")]
    [SerializeField] private bool isGameTimerActive = false; 

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
                    OnCountdownChanged?.Invoke(0);
                    countdownValue = -1;
                    goDisplayTimer = 0f;
                }
            }
            
            if (countdownValue == -1)
            {
                goDisplayTimer += Time.deltaTime;
                if (goDisplayTimer >= 0.8f)
                {
                    FinishCountdown();
                }
            }
        }
        
        if (isGameRunning && isGameTimerActive)
        {
            gameTime += Time.deltaTime;
            OnGameTimeChanged?.Invoke(gameTime);

            if (ghostsFrightened && frightenedTimer > 0)
            {
                frightenedTimer -= Time.deltaTime;
                
                if (frightenedTimer <= 3f && frightenedTimer > 0f)
                {
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

    public void StartGame()
    {
        isGameRunning = false;
        isGameTimerActive = false;
        gameTime = 0f;
        currentLives = 3;
        currentScore = 0;
        ghostsFrightened = false;
        frightenedTimer = 0f;
        
        
        OnLivesChanged?.Invoke(currentLives);
        OnScoreChanged?.Invoke(currentScore);
        OnGameTimeChanged?.Invoke(gameTime);
        OnGhostsFrightenedChanged?.Invoke(ghostsFrightened);
        
        StartCoroutine(StartCountdownDelayed());
    }

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

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

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

    public void GameOver()
    {
        isGameRunning = false;
        //Debug.Log("Game Over! Final Score: " + currentScore);
        
        HUDController hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            hudController.ShowGameOver();
        }
        
        SaveHighScore();
        
        StartCoroutine(ReturnToStartSceneDelayed());
    }
    
    public void CheckAllPelletsEaten()
    {
        GameObject[] pellets = FindGameObjectsByLayer(8);
        GameObject[] powerPills = FindGameObjectsByLayer(10);
        
        if (pellets.Length == 0 && powerPills.Length == 0)
        {
            //Debug.Log("win the game");
            GameOver();
        }
    }
    
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
    
    private System.Collections.IEnumerator ReturnToStartSceneDelayed()
    {
        yield return new WaitForSeconds(3f);
        ReturnToStartScene();
    }

    public void ReturnToStartScene()
    {
        //Debug.Log("GameManager: loading StartScene...");
        SceneManager.LoadScene("StartScene");
    }
    
    private System.Collections.IEnumerator StartCountdownDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        
        StartCountdown();
    }
    
    private void StartCountdown()
    {
        isCountdownActive = true;
        countdownValue = 3;
        countdownTimer = 0f;
        
        OnCountdownChanged?.Invoke(countdownValue);
    }
    
    private void FinishCountdown()
    {
        isCountdownActive = false;
        isGameRunning = true;
        isGameTimerActive = true;
        OnCountdownFinished?.Invoke();
    }
    

    public void EnterScared()
    {
        if (AudioManager.Instance == null)
            return;
        AudioManager.Instance.SwitchToScaredBGM();
        
        SetAllGhostsState("IsScared", true);
    }

    public void ExitScared()
    {
        if (AudioManager.Instance == null)
            return;
        
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
        
        if (!hasDeadGhost)
        {
            AudioManager.Instance.SwitchBackToNormalBGM();
            Debug.Log("GameManager: Scared end, switch to Normal BGM");
        }
        else
        {
            Debug.Log("GameManager: Scared end, have Dead ghost, keep Dead BGM");
        }
        
        SetAllGhostsState("IsNormal", true);
        SetAllGhostsState("IsScared", false);
        SetAllGhostsState("IsRecovering", false);
        SetAllGhostsState("IsDead", false);
    }
    
    private void SetAllGhostsState(string stateName, bool value)
    {
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.GetCurrentState() == GhostController.GhostState.Dead)
            {
                continue;
            }
            
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
                Animator animator = ghost.animator != null ? ghost.animator : ghost.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetBool(stateName, value);
                }
            }
        }
    }
    
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
        //Debug.Log("GameManager: ExitGhostDie() called");
        
        if (AudioManager.Instance == null)
        {
            //Debug.LogWarning("GameManager: AudioManager.Instance is null, cannot switch BGM");
            return;
        }
        
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        bool hasDeadGhost = false;
        
        foreach (GhostController ghost in ghosts)
        {
            GhostController.GhostState state = ghost.GetCurrentState();
            //Debug.Log($"GameManager: check ghost {ghost.ghostNumber}, state={state}");
            
            if (state == GhostController.GhostState.Dead)
            {
                hasDeadGhost = true;
                break;
            }
        }
        
        //Debug.Log($"GameManager: hasDeadGhost={hasDeadGhost}, ghostsFrightened={ghostsFrightened}");
        
        if (!hasDeadGhost)
        {
            if (ghostsFrightened)
            {
                AudioManager.Instance.SwitchToScaredBGM();
                //Debug.Log("GameManager: all ghosts respawn, switch to Scared BGM");
            }
            else
            {
                AudioManager.Instance.SwitchBackToNormalBGM();
                //Debug.Log("GameManager: all ghosts respawn, switch to Normal BGM");
            }
        }
        else
        {
            Debug.Log("GameManager: still have Dead ghost, keep Dead BGM");
        }
    }

    public int GetCurrentLives() => currentLives;
    public int GetCurrentScore() => currentScore;
    public float GetGameTime() => gameTime;
    public bool IsGameRunning() => isGameRunning;
    public bool AreGhostsFrightened() => ghostsFrightened;
    public float GetFrightenedTimer() => frightenedTimer;
    
    public bool IsCountdownActive() => isCountdownActive;
    public int GetCountdownValue() => countdownValue;
    
    public void SaveHighScore()
    {
        int level1HighScore = PlayerPrefs.GetInt("Level1_HighScore", 0);
        float level1BestTime = PlayerPrefs.GetFloat("Level1_BestTime", float.MaxValue);
        
        if (currentScore > level1HighScore || 
            (currentScore == level1HighScore && gameTime < level1BestTime))
        {
            PlayerPrefs.SetInt("Level1_HighScore", currentScore);
            PlayerPrefs.SetFloat("Level1_BestTime", gameTime);
            PlayerPrefs.Save();
            Debug.Log($"new Level 1 highest score! score: {currentScore}, time: {gameTime:F2} seconds");
        }
        else
        {
            Debug.Log($"not break Level 1 record. current: {currentScore}, highest: {level1HighScore}");
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
