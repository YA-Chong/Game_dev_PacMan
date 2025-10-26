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

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Main Camera not found! Ghost label will not billboard.");
        }

        ShowLabel(true);
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            ghostCanvas.transform.LookAt(ghostCanvas.transform.position + mainCameraTransform.rotation * Vector3.forward,
                                         mainCameraTransform.rotation * Vector3.up);
        }
    }

    public void ShowLabel(bool isVisible)
    {
        if (ghostCanvas != null)
        {
            ghostCanvas.SetActive(isVisible);
        }
    }
}