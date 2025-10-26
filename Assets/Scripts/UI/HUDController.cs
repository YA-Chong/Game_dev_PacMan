using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject[] heartIcons;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI ghostTimerText;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private Button exitButton;
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private TextMeshProUGUI gameOverText;


    private int currentLives;
    private int currentScore;
    private float ghostFrightenedTime;

    void Start()
    {
        currentLives = heartIcons.Length;
        currentScore = 0;
        ghostFrightenedTime = 0f;

        UpdateLivesDisplay(currentLives);
        UpdateScoreDisplay(currentScore);
        UpdateGameTimerDisplay(0f);
        UpdateGhostTimerDisplay();

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ReturnToStartScene);
        }

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
        if (ghostFrightenedTime > 0)
        {
            ghostFrightenedTime -= Time.deltaTime;
            UpdateGhostTimerDisplay();
        }
    }


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

    public void UpdateScoreDisplay(int score)
    {
        currentScore = score;
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("000000");
        }
    }

    public void UpdateGameTimerDisplay(float time)
    {
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
            gameTimerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }

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


    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLivesDisplay(currentLives);
            
            if (currentLives <= 0)
            {
                GameOver();
            }
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreDisplay(currentScore);
    }

    public void StartGhostFrightenedMode(float duration)
    {
        ghostFrightenedTime = duration;
        UpdateGhostTimerDisplay();
    }

    public void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
    }
    
    public void ShowGameOver()
    {
        //Debug.Log("HUDController: show Game Over UI");
        
        if (gameOverOverlay != null)
        {
            gameOverOverlay.SetActive(true);
            //Debug.Log("HUDController: overlay is activated");
        }
        else
        {
            Debug.LogWarning("HUDController: gameOverOverlay is not assigned!");
        }
        
        if (gameOverText != null)
        {
            gameOverText.text = "Game Over";
            gameOverText.gameObject.SetActive(true);
            //Debug.Log("HUDController: Game Over text is displayed");
        }
        else
        {
            Debug.LogWarning("HUDController: gameOverText is not assigned!");
        }
            
        if (exitButton != null)
            exitButton.interactable = false;
    }
    
    public void HideGameOver()
    {
        if (gameOverOverlay != null)
            gameOverOverlay.SetActive(false);
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
            
        if (exitButton != null)
            exitButton.interactable = true;
    }

    public void PauseGameTimer()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGameTimer();
        }
    }

    public void ResumeGameTimer()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGameTimer();
        }
    }

    private void OnGhostsFrightenedChanged(bool isFrightened)
    {
        if (isFrightened)
        {
            StartGhostFrightenedMode(10f);
        }
        else
        {
            ghostFrightenedTime = 0f;
            UpdateGhostTimerDisplay();
        }
    }
}
