using UnityEngine;
using TMPro;

public class CountdownController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    private void Start()
    {
        // 初始隐藏
        HideCountdown();
        
        // 立即订阅事件
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        // 订阅倒计时事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCountdownChanged += UpdateCountdown;
            GameManager.Instance.OnCountdownFinished += HideCountdown;
            
            // 检查是否已经有倒计时在进行
            if (GameManager.Instance.IsCountdownActive())
            {
                UpdateCountdown(GameManager.Instance.GetCountdownValue());
            }
        }
        else
        {
            Debug.LogError("CountdownController: GameManager.Instance为null，无法订阅事件");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅
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
        
        // 显示倒计时UI
        countdownOverlay.SetActive(true);
        countdownText.gameObject.SetActive(true);
    }
    
    private void HideCountdown()
    {
        countdownOverlay.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }
}
