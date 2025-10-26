using UnityEngine;
using TMPro;

public class CountdownController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    private void Start()
    {
        HideCountdown();
        
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged += UpdateCountdown;
            GameManager.Instance.OnCountdownFinished += HideCountdown;
            
            if (GameManager.Instance.IsCountdownActive())
            {
                UpdateCountdown(GameManager.Instance.GetCountdownValue());
            }
        }
        else
        {
            Debug.LogError("CountdownController: GameManager.Instance is null, cannot subscribe to events");
        }
    }
    
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged -= UpdateCountdown;
            GameManager.Instance.OnCountdownFinished -= HideCountdown;
        }
    }
    
    private void UpdateCountdown(int value)
    {
        if (value > 0)
        {
            countdownText.text = value.ToString();
        }
        else if (value == 0)
        {
            countdownText.text = "GO!";
        }
        
        countdownOverlay.SetActive(true);
        countdownText.gameObject.SetActive(true);
    }
    
    private void HideCountdown()
    {
        countdownOverlay.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }
}
