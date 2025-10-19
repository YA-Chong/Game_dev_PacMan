using UnityEngine;
using TMPro;

public class GhostLabelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI ghostLabel;
    [SerializeField] private GameObject ghostCanvas;

    private Transform mainCameraTransform;

    void Start()
    {
        // 确保引用不为空
        if (ghostLabel == null)
        {
            Debug.LogError("GhostLabel is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }
        if (ghostCanvas == null)
        {
            Debug.LogError("GhostCanvas is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        // 获取主摄像机
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Main Camera not found! Ghost label will not billboard.");
        }

        // 显示标签（保持您设置的数字1-4）
        ShowLabel(true);
    }

    void LateUpdate()
    {
        // 实现Billboard效果：标签始终面向摄像机
        if (mainCameraTransform != null)
        {
            ghostCanvas.transform.LookAt(ghostCanvas.transform.position + mainCameraTransform.rotation * Vector3.forward,
                                         mainCameraTransform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// 显示或隐藏标签
    /// </summary>
    /// <param name="isVisible">是否可见</param>
    public void ShowLabel(bool isVisible)
    {
        if (ghostCanvas != null)
        {
            ghostCanvas.SetActive(isVisible);
        }
    }
}