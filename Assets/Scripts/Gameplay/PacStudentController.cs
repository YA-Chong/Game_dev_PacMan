using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    
    [Header("Audio")]
    public AudioManager audioManager;
    
    [Header("Animation")]
    public Animator animator;
    

    private Vector2[] gridPathPoints = new Vector2[]
    {
        new Vector2(1, 1),
        new Vector2(2, 1),
        new Vector2(3, 1),
        new Vector2(4, 1),
        new Vector2(5, 1),
        new Vector2(6, 1),
        new Vector2(6, 2),
        new Vector2(6, 3),
        new Vector2(6, 4),
        new Vector2(6, 5),
        new Vector2(5, 5),
        new Vector2(4, 5),
        new Vector2(3, 5),
        new Vector2(2, 5),
        new Vector2(1, 5),
        new Vector2(1, 4),
        new Vector2(1, 3),
        new Vector2(1, 2),
        new Vector2(1, 1)
    };
    

    private int currentPathIndex = 0;
    private int nextPathIndex = 1;
    private float pathProgress = 0f;
    private bool isMoving = false;
    

    private int moveX = 0;
    private int moveY = 0;
    

    private int lastPlayedPathIndex = -1;
    
    void Start()
    {

        Vector2 startWorldPos = GridToWorldPosition(gridPathPoints[0]);
        transform.position = new Vector3(startWorldPos.x, startWorldPos.y, -2); 
        
    
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 2;
        }
        
        
        lastPlayedPathIndex = 0;
        
        
        StartMovement();
    }
    
    void Update()
    {
        if (isMoving)
        {
            MoveAlongPath();
        }
    }
    
    private void StartMovement()
    {
        isMoving = true;
        pathProgress = 0f;
        UpdateDirection();
        PlayMoveAnimation();
    }
    
    private void MoveAlongPath()
    {
    
        Vector2 currentGridPoint = gridPathPoints[currentPathIndex];
        Vector2 nextGridPoint = gridPathPoints[nextPathIndex];
        
     
        Vector2 currentWorldPoint = GridToWorldPosition(currentGridPoint);
        Vector2 nextWorldPoint = GridToWorldPosition(nextGridPoint);
        
    
        float segmentLength = Vector2.Distance(currentWorldPoint, nextWorldPoint);
        
        
        float progressIncrement = (moveSpeed * Time.deltaTime) / segmentLength;
        pathProgress += progressIncrement;
        
        
        Vector2 currentWorldPosition = Vector2.Lerp(currentWorldPoint, nextWorldPoint, pathProgress);
        transform.position = new Vector3(currentWorldPosition.x, currentWorldPosition.y, -2);
        
       
        UpdateDirection();
        PlayMoveAnimation();
        
       
        CheckForGridCrossing();
        
 
        if (pathProgress >= 1f)
        {
            OnReachPathPoint();
        }
    }
    
    private void OnReachPathPoint()
    {
    
        currentPathIndex = nextPathIndex;
        nextPathIndex = (nextPathIndex + 1) % gridPathPoints.Length;
        pathProgress = 0f;
        
   
        if (currentPathIndex == 0)
        {
            // Debug.Log("PacStudent completed one full cycle!");
        }
    }
    
    private void UpdateDirection()
    {
        Vector2 currentPoint = gridPathPoints[currentPathIndex];
        Vector2 nextPoint = gridPathPoints[nextPathIndex];
        
        Vector2 direction = (nextPoint - currentPoint).normalized;
        
   
        moveX = Mathf.RoundToInt(direction.x);
        moveY = -Mathf.RoundToInt(direction.y);
    }
    

    private void CheckForGridCrossing()
    {
     
        if (currentPathIndex != lastPlayedPathIndex)
        {
            if (audioManager != null)
            {
                audioManager.PlayMoveSFX();
                //Debug.Log($"Played move sound at path index: {currentPathIndex}, grid position: {gridPathPoints[currentPathIndex]}");
            }
            lastPlayedPathIndex = currentPathIndex;
        }
    }
    

    private int FindClosestPathIndex(Vector2 currentGridPos)
    {
        float minDistance = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < gridPathPoints.Length; i++)
        {
            float distance = Vector2.Distance(currentGridPos, gridPathPoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    
    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        
        if (gridPos == new Vector2(1, 1)) return new Vector2(-12.5f, 13.5f);
        if (gridPos == new Vector2(2, 1)) return new Vector2(-11.5f, 13.5f);
        if (gridPos == new Vector2(3, 1)) return new Vector2(-10.5f, 13.5f);
        if (gridPos == new Vector2(4, 1)) return new Vector2(-9.5f, 13.5f);
        if (gridPos == new Vector2(5, 1)) return new Vector2(-8.5f, 13.5f);
        if (gridPos == new Vector2(6, 1)) return new Vector2(-7.5f, 13.5f);
        if (gridPos == new Vector2(6, 2)) return new Vector2(-7.5f, 12.5f);
        if (gridPos == new Vector2(6, 3)) return new Vector2(-7.5f, 11.5f);
        if (gridPos == new Vector2(6, 4)) return new Vector2(-7.5f, 10.5f);
        if (gridPos == new Vector2(6, 5)) return new Vector2(-7.5f, 9.5f);
        if (gridPos == new Vector2(5, 5)) return new Vector2(-8.5f, 9.5f);
        if (gridPos == new Vector2(4, 5)) return new Vector2(-9.5f, 9.5f);
        if (gridPos == new Vector2(3, 5)) return new Vector2(-10.5f, 9.5f);
        if (gridPos == new Vector2(2, 5)) return new Vector2(-11.5f, 9.5f);
        if (gridPos == new Vector2(1, 5)) return new Vector2(-12.5f, 9.5f);
        if (gridPos == new Vector2(1, 4)) return new Vector2(-12.5f, 10.5f);
        if (gridPos == new Vector2(1, 3)) return new Vector2(-12.5f, 11.5f);
        if (gridPos == new Vector2(1, 2)) return new Vector2(-12.5f, 12.5f);
        
       
        return new Vector2(-12.5f, 13.5f);
    }
    
    
    private Vector2 WorldToGridPosition(Vector3 worldPos)
    {
        
        Vector2 pos = new Vector2(worldPos.x, worldPos.y);
        
        if (pos == new Vector2(-12.5f, 13.5f)) return new Vector2(1, 1);
        if (pos == new Vector2(-11.5f, 13.5f)) return new Vector2(2, 1);
        if (pos == new Vector2(-10.5f, 13.5f)) return new Vector2(3, 1);
        if (pos == new Vector2(-9.5f, 13.5f)) return new Vector2(4, 1);
        if (pos == new Vector2(-8.5f, 13.5f)) return new Vector2(5, 1);
        if (pos == new Vector2(-7.5f, 13.5f)) return new Vector2(6, 1);
        if (pos == new Vector2(-7.5f, 12.5f)) return new Vector2(6, 2);
        if (pos == new Vector2(-7.5f, 11.5f)) return new Vector2(6, 3);
        if (pos == new Vector2(-7.5f, 10.5f)) return new Vector2(6, 4);
        if (pos == new Vector2(-7.5f, 9.5f)) return new Vector2(6, 5);
        if (pos == new Vector2(-8.5f, 9.5f)) return new Vector2(5, 5);
        if (pos == new Vector2(-9.5f, 9.5f)) return new Vector2(4, 5);
        if (pos == new Vector2(-10.5f, 9.5f)) return new Vector2(3, 5);
        if (pos == new Vector2(-11.5f, 9.5f)) return new Vector2(2, 5);
        if (pos == new Vector2(-12.5f, 9.5f)) return new Vector2(1, 5);
        if (pos == new Vector2(-12.5f, 10.5f)) return new Vector2(1, 4);
        if (pos == new Vector2(-12.5f, 11.5f)) return new Vector2(1, 3);
        if (pos == new Vector2(-12.5f, 12.5f)) return new Vector2(1, 2);
        
        
        return new Vector2(1, 1);
    }
    
    private void PlayMoveAnimation()
    {
        if (animator != null)
        {
           
            animator.SetInteger("MoveX", moveX);
            animator.SetInteger("MoveY", moveY);
            
           
            //Debug.Log($"Animation: MoveX={moveX}, MoveY={moveY}, PathIndex={currentPathIndex}");
        }
    }
    
    
    private void OnTriggerEnter2D(Collider2D other)
    {
       
        if (other.CompareTag("Pellet"))
        {
           
            // Debug.Log("Ate a pellet!");
        }
        else if (other.CompareTag("PowerPellet"))
        {
           
            // Debug.Log("Ate a power pellet!");
        }
        else if (other.CompareTag("Wall"))
        {
           
            // Debug.Log("Hit a wall!");
        }
    }
    

    public void PauseMovement()
    {
        isMoving = false;
    }
    
    public void ResumeMovement()
    {
        isMoving = true;
    }
    

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
