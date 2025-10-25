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
            Debug.Log("CountdownController: 已订阅倒计时事件");
            
            // 检查是否已经有倒计时在进行
            if (GameManager.Instance.IsCountdownActive())
            {
                Debug.Log("CountdownController: 检测到倒计时已在进行，立即更新UI");
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
        Debug.Log($"CountdownController: 更新倒计时 {value}");
        
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
        Debug.Log("CountdownController: 隐藏倒计时UI");
        countdownOverlay.SetActive(false);
        countdownText.gameObject.SetActive(false);
    }
}
