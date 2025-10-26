using UnityEngine;
using UnityEngine.UI;

public class AnimatedBorder : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;
    
    private Image[] borderDots;
    
    void Start()
    {
        borderDots = new Image[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            borderDots[i] = transform.GetChild(i).GetComponent<Image>();
        }
    }
    
    void Update()
    {
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        
        foreach (Image dot in borderDots)
        {
            if (dot != null)
            {
                Color color = dot.color;
                color.a = alpha;
                dot.color = color;
            }
        }
    }
}