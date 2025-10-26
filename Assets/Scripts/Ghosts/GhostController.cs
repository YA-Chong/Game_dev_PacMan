using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("Ghost Settings")]
    public int ghostNumber = 1;
    public float moveSpeed = 1.8f;
    
    [Header("Animation")]
    public Animator animator;
    
    [Header("Game Manager")]
    public GameManager gameManager;
    
    public enum GhostState
    {
        Normal,
        Scared,
        Recovering,
        Dead
    }
    
    private GhostState currentState = GhostState.Normal;
    private bool isMoving = false;
    
    private Vector2 currentGridPosition;
    private Vector2 targetGridPosition;
    private bool isLerping = false;
    private float lerpProgress = 0f;
    private Vector2 lastDirection = Vector2.zero;
    
    private Vector2 ghost3CurrentDirection = Vector2.zero;
    
    private Vector2 ghost4CurrentDirection = Vector2.zero;
    private bool ghost4ReachedOuterWall = false;
    private int ghost4CurrentCornerIndex = -1;
    private bool ghost4IsEscapingDeadEnd = false;
    private int ghost4EscapeStepCounter = 0;
    
    private readonly Vector2[] outerCorners = new Vector2[]
    {
        new Vector2(-12.5f, 13.5f),
        new Vector2(12.5f, 13.5f),
        new Vector2(12.5f, -12.5f),
        new Vector2(-12.5f, -12.5f)
    };
    
    private Vector2 initialWorldPosition;
    private bool isReturningToDead = false;
    
    
    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (gameManager == null)
            gameManager = GameManager.Instance;
            
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            moveSpeed = pacStudent.moveSpeed * 0.9f;
            // Debug.Log($"Ghost {ghostNumber}: set speed to {moveSpeed} (PacStudent speed * 0.9)");
        }
        
        Vector2 initialWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 initialGridPos = WorldToGridPosition(initialWorldPos);
        // Debug.Log($"Ghost {ghostNumber}: initial world position {initialWorldPos}, grid position {initialGridPos}");
            
        if (animator == null)
        {
            Debug.LogError($"Ghost {ghostNumber}: cannot find Animator component!");
        }
            
        Vector2 worldPos = new Vector2(transform.position.x, transform.position.y);
        currentGridPosition = WorldToGridPosition(worldPos);
        targetGridPosition = currentGridPosition;
        
        initialWorldPosition = worldPos;
        
        // Debug.Log($"Ghost {ghostNumber}: initialized - world position {worldPos}, grid position {currentGridPosition}, initial position saved");
        
        SetGhostState(GhostState.Normal);
        
        if (animator != null)
        {
            animator.SetFloat("MoveX", 1f);
            animator.SetFloat("MoveY", 0f);
        }
        
        StartMoving();
    }
    
    void Update()
    {
        if (gameManager != null && !gameManager.IsGameRunning())
        {
            return;
        }
        
        UpdateAnimationParameters();
        
        if (isReturningToDead)
        {
            return;
        }
        
        if (isLerping)
        {
            LerpToTarget();
        }
        else if (isMoving)
        {
            Vector2 nextDirection = GetNextDirection();
            if (nextDirection != Vector2.zero)
            {
                TryMove(nextDirection);
            }
        }
    }
    
    public void SetGhostState(GhostState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        if (animator != null)
        {
            switch (newState)
            {
                case GhostState.Normal:
                    animator.SetBool("IsNormal", true);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", false);
                    animator.SetFloat("MoveX", 1f);
                    animator.SetFloat("MoveY", 0f);
                    break;
                    
                case GhostState.Scared:
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", true);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", false);
                    break;
                    
                case GhostState.Recovering:
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", true);
                    animator.SetBool("IsDead", false);
                    break;
                    
                case GhostState.Dead:
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", true);
                    break;
            }
            
        }
        else
        {
            Debug.LogError($"Ghost {ghostNumber}: Animator is null, cannot set state {newState}");
        }
        
        switch (newState)
        {
            case GhostState.Normal:
                moveSpeed = 1.8f;
                
                if (ghostNumber == 4)
                {
                    ghost4ReachedOuterWall = false;
                    ghost4CurrentDirection = Vector2.zero;
                    ghost4CurrentCornerIndex = -1;
                    ghost4IsEscapingDeadEnd = false;
                    ghost4EscapeStepCounter = 0;
                    // Debug.Log("Ghost 4: reset path state when recovering to Normal state");
                }
                break;
            case GhostState.Scared:
                moveSpeed = 0.9f;
                break;
            case GhostState.Recovering:
                moveSpeed = 0.9f;
                break;
            case GhostState.Dead:
                moveSpeed = 0.9f;
                StartCoroutine(ReturnToInitialPosition());
                break;
        }
        
        if (!isMoving && newState != GhostState.Dead)
        {
            StartMoving();
        }
        
        // Debug.Log($"Ghost {ghostNumber} state changed to: {newState}, moving state: {isMoving}");
    }
    
    private Vector2 GetNextDirection()
    {
        bool inCenterRoom = IsInCenterRoom();
        
        if (inCenterRoom && currentState != GhostState.Dead)
        {
            // Debug.Log($"Ghost {ghostNumber}: in center room (position: {currentGridPosition}), prioritize exit");
            return GetExitCenterRoomDirection();
        }
        
        switch (currentState)
        {
            case GhostState.Normal:
                return GetNormalStateDirection();
            case GhostState.Scared:
            case GhostState.Recovering:
                return GetScaredStateDirection();
            case GhostState.Dead:
                return GetDeadStateDirection();
            default:
                return Vector2.zero;
        }
    }
    
    
    private void TryMove(Vector2 direction)
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 nextWorldPos = currentWorldPos + direction;
        
        // Debug.Log($"Ghost {ghostNumber}: try to move from {currentWorldPos} to {nextWorldPos} (direction: {direction})");
        
        if (CanMoveToWorldPos(nextWorldPos))
        {
            // Debug.Log($"Ghost {ghostNumber}: move successful");
            targetGridPosition = WorldToGridPosition(nextWorldPos);
            lastDirection = direction;
            StartLerpToTarget();
        }
        else
        {
            // Debug.Log($"Ghost {ghostNumber}: move failed, blocked");
        }
    }
    
    private bool CanMoveToWorldPos(Vector2 worldPos)
    {
        if (Mathf.Abs(worldPos.x) > 30 || Mathf.Abs(worldPos.y) > 30)
            return false;
            
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        Collider2D wall = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wall != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected wall at {worldPos}");
            return false;
        }
        
        Collider2D teleporter = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Teleporter"));
        if (teleporter != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected teleporter at {worldPos}, treated as wall");
            return false;
        }
        
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected TopDoor at {worldPos}");
            if (ghostNumber == 1 || ghostNumber == 3)
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor allowed through");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor not allowed through");
                return false;
            }
        }
        
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected BottomDoor at {worldPos}");
            if (ghostNumber == 2 || ghostNumber == 4)
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor allowed through");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor not allowed through");
                return false;
            }
        }
        
        return true;
    }
    
    private void StartLerpToTarget()
    {
        isLerping = true;
        lerpProgress = 0f;
    }
    
    private void LerpToTarget()
    {
        lerpProgress += moveSpeed * Time.deltaTime;
        
        if (lerpProgress >= 1f)
        {
            lerpProgress = 1f;
            isLerping = false;
            currentGridPosition = targetGridPosition;
        }
        
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        Vector2 targetWorldPos = GridToWorldPosition(targetGridPosition);
        Vector2 lerpedPos = Vector2.Lerp(currentWorldPos, targetWorldPos, lerpProgress);
        
        transform.position = new Vector3(lerpedPos.x, lerpedPos.y, -1);
    }
    
    private bool CanMoveTo(Vector2 gridPos)
    {
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        if (Mathf.Abs(gridPos.x) > 30 || Mathf.Abs(gridPos.y) > 30)
            return false;
            
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        Collider2D wall = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wall != null)
        {
            return false;
        }
        
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected TopDoor at {worldPos}");
            if (ghostNumber == 1 || ghostNumber == 3)
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor allowed through");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor not allowed through");
                return false;
            }
        }
        
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: detected BottomDoor at {worldPos}");
            if (ghostNumber == 2 || ghostNumber == 4)
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor allowed through");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor not allowed through");
                return false;
            }
        }
        
        // Debug.Log($"Ghost {ghostNumber}: no doors or walls detected at {worldPos}");
        
        return true;
    }
    
    private void UpdateAnimationParameters()
    {
        if (animator != null)
        {
            if (currentState == GhostState.Normal && lastDirection != Vector2.zero)
            {
                animator.SetFloat("MoveX", lastDirection.x);
                animator.SetFloat("MoveY", lastDirection.y);
            }
            
            // animator.SetBool("IsMoving", isMoving);
        }
    }
    
    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        float offsetX = (gridPos.x - 1) * 1.0f;
        float offsetY = -(gridPos.y - 1) * 1.0f;
        
        float baseX = -12.5f;
        float baseY = 13.5f;
        
        float worldX = baseX + offsetX;
        float worldY = baseY + offsetY;
        
        return new Vector2(worldX, worldY);
    }
    
    private Vector2 WorldToGridPosition(Vector2 worldPos)
    {
        float offsetX = worldPos.x - (-12.5f);
        float offsetY = 13.5f - worldPos.y;
        
        int gridX = Mathf.RoundToInt(1 + offsetX / 1.0f);
        int gridY = Mathf.RoundToInt(1 + offsetY / 1.0f);
        
        return new Vector2(gridX, gridY);
    }
    
    public void SetPosition(Vector2 gridPos)
    {
        currentGridPosition = gridPos;
        targetGridPosition = gridPos;
        Vector2 worldPos = GridToWorldPosition(gridPos);
        transform.position = new Vector3(worldPos.x, worldPos.y, -1);
        isLerping = false;
    }
    
    public void RespawnGhost()
    {
        if (animator != null)
        {
            animator.SetBool("IsNormal", false);
            animator.SetBool("IsScared", false);
            animator.SetBool("IsRecovering", false);
            animator.SetBool("IsDead", false);
            
            animator.SetTrigger("Respawn");
            //Debug.Log($"Ghost {ghostNumber}: using Respawn trigger to respawn");
        }
        SetGhostState(GhostState.Normal);
    }
    
    public GhostState GetCurrentState()
    {
        return currentState;
    }
    
    private void StartMoving()
    {
        if (!isMoving)
        {
            isMoving = true;
        }
    }
    
    private void StopMoving()
    {
        isMoving = false;
        isLerping = false;
    }
    
    private void MoveToDirection(Vector2 direction)
    {
        Debug.LogWarning($"Ghost {ghostNumber}: MoveToDirection called, this should not happen! Use TryMove instead");
    }
    
    private bool CanMoveToPosition(Vector2 worldPos)
    {
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        Collider2D wallCollider = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wallCollider != null)
        {
            if (IsCenterRoomDoor(worldPos))
            {
                return true;
            }
            return false;
        }
        
        return true;
    }
    
    private bool IsCenterRoomDoor(Vector2 worldPos)
    {
        Vector2 gridPos = WorldToGridPosition(worldPos);
        
        if (IsGhostDoor(worldPos))
        {
            return true;
        }
        
        return false;
    }
    
    private bool IsGhostDoor(Vector2 worldPos)
    {
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null && (ghostNumber == 1 || ghostNumber == 3))
        {
            return true;
        }
        
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null && (ghostNumber == 2 || ghostNumber == 4))
        {
            return true;
        }
        
        return false;
    }
    

    private Vector2 GetRandomValidDirection()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextPos = currentGridPosition + dir;
            Vector2 worldPos = GridToWorldPosition(nextPos);
            
            if (CanMoveToPosition(worldPos))
            {
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        return Vector2.zero;
    }
    
    
    private Vector2 GetNormalStateDirection()
    {
        switch (ghostNumber)
        {
            case 1:
                return GetGhost1Direction();
            case 2:
                return GetGhost2Direction();
            case 3:
                return GetGhost3Direction();
            case 4:
                return GetGhost4Direction();
            default:
                return GetRandomValidDirection();
        }
    }
    
    private Vector2 GetScaredStateDirection()
    {
        return GetGhost1Direction();
    }
    
    private System.Collections.IEnumerator ReturnToInitialPosition()
    {
        isReturningToDead = true;
        isMoving = false;
        isLerping = false;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(initialWorldPosition.x, initialWorldPosition.y, -1f);
        
        float deadSpeed = 0.9f;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float moveTime = distance / deadSpeed;
        float elapsedTime = 0f;
        
        // Debug.Log($"Ghost {ghostNumber}: starting to return to initial position {targetPosition}, distance={distance:F2}, speed={deadSpeed}, estimated time={moveTime:F2} seconds");
        
        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveTime;
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.position = newPosition;
            
            yield return null;
        }
        
        transform.position = targetPosition;
        
        // Debug.Log($"Ghost {ghostNumber}: reached initial position");
        
        GhostState respawnState = DetermineRespawnState();
        
        Debug.Log($"Ghost {ghostNumber}: preparing to respawn as {respawnState} state");
        
        isReturningToDead = false;
        
        currentGridPosition = WorldToGridPosition(new Vector2(transform.position.x, transform.position.y));
        targetGridPosition = currentGridPosition;
        
        currentState = respawnState;
        
        if (gameManager != null)
        {
            gameManager.ExitGhostDie();
        }
        
        // Debug.Log($"Ghost {ghostNumber}: state changed to {respawnState}, currentState = {currentState}");
        
        if (animator != null)
        {
            animator.SetBool("IsNormal", false);
            animator.SetBool("IsScared", false);
            animator.SetBool("IsRecovering", false);
            animator.SetBool("IsDead", false);
            
            // Debug.Log($"Ghost {ghostNumber}: cleared all Animator states");
            
            yield return null;
            
            switch (respawnState)
            {
                case GhostState.Normal:
                    animator.SetBool("IsNormal", true);
                    animator.SetFloat("MoveX", 1f);
                    animator.SetFloat("MoveY", 0f);
                    moveSpeed = 1.8f;
                    
                    if (ghostNumber == 4)
                    {
                        ghost4ReachedOuterWall = false;
                        ghost4CurrentDirection = Vector2.zero;
                        ghost4CurrentCornerIndex = -1;
                        ghost4IsEscapingDeadEnd = false;
                        ghost4EscapeStepCounter = 0;
                        // Debug.Log("Ghost 4: respawned as Normal state, reset path state");
                    }
                    
                    // Debug.Log($"Ghost {ghostNumber}: set to Normal state, IsNormal=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator state validation - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
                    
                case GhostState.Scared:
                    animator.SetBool("IsScared", true);
                    moveSpeed = 0.9f;
                    // Debug.Log($"Ghost {ghostNumber}: set to Scared state, IsScared=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator state validation - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
                    
                case GhostState.Recovering:
                    animator.SetBool("IsRecovering", true);
                    moveSpeed = 0.9f;
                    // Debug.Log($"Ghost {ghostNumber}: set to Recovering state, IsRecovering=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator state validation - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
            }
            
            yield return null;
            
            // Debug.Log($"Ghost {ghostNumber}: final Animator state - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
        }
        
        StartMoving();
        
        Debug.Log($"Ghost {ghostNumber}: respawn completed, final state {currentState}");
    }
    
    private Vector2 GetDeadStateDirection()
    {
        // This method is no longer used
        return Vector2.zero;
    }
    
    private GhostState DetermineRespawnState()
    {
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        
        foreach (GhostController ghost in ghosts)
        {
            if (ghost == this) continue;
            
            GhostState otherState = ghost.GetCurrentState();
            
            if (otherState == GhostState.Recovering)
            {
                // Debug.Log($"Ghost {ghostNumber}: detected other ghost in Recovering state");
                return GhostState.Recovering;
            }
            
            if (otherState == GhostState.Scared)
            {
                // Debug.Log($"Ghost {ghostNumber}: detected other ghost in Scared state");
                return GhostState.Scared;
            }
        }
        
        // Debug.Log($"Ghost {ghostNumber}: no ghost in Scared/Recovering state, respawn as Normal");
        return GhostState.Normal;
    }
    
    private Vector2 GetGhost1Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 pacStudentWorldPos = GetPacStudentWorldPosition();
        float currentDistance = Vector2.Distance(currentWorldPos, pacStudentWorldPos);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<Vector2> allValidDirections = new System.Collections.Generic.List<Vector2>();
        
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos))
            {
                if (currentState != GhostState.Dead && IsWorldPosInCenterRoom(nextWorldPos))
                {
                    continue;
                }
                
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                allValidDirections.Add(dir);
                
                float newDistance = Vector2.Distance(nextWorldPos, pacStudentWorldPos);
                if (newDistance >= currentDistance)
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        if (allValidDirections.Count > 0)
        {
            // Debug.Log($"Ghost 1: cannot move away, choose any valid direction (no backward)");
            return allValidDirections[Random.Range(0, allValidDirections.Count)];
        }
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                // Debug.Log($"Ghost 1: trapped, allow backward");
                return dir;
            }
        }
        
        return GetRandomValidDirection();
    }
    
    private Vector2 GetPacStudentWorldPosition()
    {
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            return new Vector2(pacStudent.transform.position.x, pacStudent.transform.position.y);
        }
        return Vector2.zero;
    }
    
    private bool IsWorldPosInCenterRoom(Vector2 worldPos)
    {
        return worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
               worldPos.y >= -1.5f && worldPos.y <= 2.5f;
    }
    
    private Vector2 GetGhost2Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 pacStudentWorldPos = GetPacStudentWorldPosition();
        float currentDistance = Vector2.Distance(currentWorldPos, pacStudentWorldPos);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<Vector2> allValidDirections = new System.Collections.Generic.List<Vector2>();
        
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos))
            {
                if (currentState != GhostState.Dead && IsWorldPosInCenterRoom(nextWorldPos))
                {
                    continue;
                }
                
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                allValidDirections.Add(dir);
                
                float newDistance = Vector2.Distance(nextWorldPos, pacStudentWorldPos);
                if (newDistance <= currentDistance)
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        if (allValidDirections.Count > 0)
        {
            // Debug.Log($"Ghost 2: cannot move closer, choose any valid direction (no backward)");
            return allValidDirections[Random.Range(0, allValidDirections.Count)];
        }
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                // Debug.Log($"Ghost 2: trapped, allow backward");
                return dir;
            }
        }
        
        return GetRandomValidDirection();
    }
    
    private Vector2 GetGhost3Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        Vector2 oppositeDirection = -ghost3CurrentDirection;
        
        foreach (Vector2 dir in directions)
        {
            if (ghost3CurrentDirection != Vector2.zero && dir == oppositeDirection)
            {
                continue;
            }
            
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            ghost3CurrentDirection = validDirections[Random.Range(0, validDirections.Count)];
            // Debug.Log($"Ghost 3: choose new direction {ghost3CurrentDirection}");
            return ghost3CurrentDirection;
        }
        
        validDirections.Clear();
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            ghost3CurrentDirection = validDirections[Random.Range(0, validDirections.Count)];
            //Debug.Log($"Ghost 3: trapped, allow backward {ghost3CurrentDirection}");
            return ghost3CurrentDirection;
        }
        
        return GetRandomValidDirection();
    }
    
    private Vector2 GetGhost4Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        
        if (!ghost4ReachedOuterWall)
        {
            float targetYTop = 13.5f;
            float targetYBottom = -12.5f;
            float currentY = currentWorldPos.y;
            
            bool targetTop = Mathf.Abs(currentY - targetYTop) < Mathf.Abs(currentY - targetYBottom);
            float targetY = targetTop ? targetYTop : targetYBottom;
            
            if (Mathf.Abs(currentY - targetY) > 0.5f)
            {
                Vector2 verticalDir = (targetY > currentY) ? Vector2.up : Vector2.down;
                Vector2 checkPos = currentWorldPos + verticalDir;
                
                if (CanMoveToWorldPos(checkPos) && !IsWorldPosInCenterRoom(checkPos))
                {
                    // Debug.Log($"Ghost 4: move to {(targetTop ? "top" : "bottom")} edge");
                    return verticalDir;
                }
                
                Vector2 checkPosLeft = currentWorldPos + Vector2.left;
                Collider2D teleporterLeft = Physics2D.OverlapPoint(checkPosLeft, LayerMask.GetMask("Teleporter"));
                if (teleporterLeft == null && CanMoveToWorldPos(checkPosLeft) && !IsWorldPosInCenterRoom(checkPosLeft))
                {
                    // Debug.Log($"Ghost 4: move left around");
                    return Vector2.left;
                }
                
                Vector2 checkPosRight = currentWorldPos + Vector2.right;
                if (CanMoveToWorldPos(checkPosRight) && !IsWorldPosInCenterRoom(checkPosRight))
                {
                    // Debug.Log($"Ghost 4: move right around");
                    return Vector2.right;
                }
            }
            else
            {
                if (ghost4CurrentCornerIndex == -1)
                {
                    if (targetTop)
                    {
                        float distToTopLeft = Vector2.Distance(currentWorldPos, outerCorners[0]);
                        float distToTopRight = Vector2.Distance(currentWorldPos, outerCorners[1]);
                        ghost4CurrentCornerIndex = (distToTopLeft < distToTopRight) ? 0 : 1;
                    }
                    else
                    {
                        float distToBottomLeft = Vector2.Distance(currentWorldPos, outerCorners[3]);
                        float distToBottomRight = Vector2.Distance(currentWorldPos, outerCorners[2]);
                        ghost4CurrentCornerIndex = (distToBottomLeft < distToBottomRight) ? 3 : 2;
                    }
                    Debug.Log($"Ghost 4: reached {(targetTop ? "top" : "bottom")} edge, choose corner {ghost4CurrentCornerIndex}");
                }
                
                Vector2 targetCorner = outerCorners[ghost4CurrentCornerIndex];
                
                if (Vector2.Distance(currentWorldPos, targetCorner) < 1.5f)
                {
                    ghost4ReachedOuterWall = true;
                    Debug.Log($"Ghost 4: reached corner {ghost4CurrentCornerIndex}, start circling");
                    return GetGhost4Direction();
                }
                
                float deltaX = targetCorner.x - currentWorldPos.x;
                
                if (Mathf.Abs(deltaX) > 0.5f)
                {
                    Vector2 horizontalDir = deltaX > 0 ? Vector2.right : Vector2.left;
                    Vector2 checkPos = currentWorldPos + horizontalDir;
                    
                    if (CanMoveToWorldPos(checkPos) && !IsWorldPosInCenterRoom(checkPos))
                    {
                        // Debug.Log($"Ghost 4: move along edge {horizontalDir}");
                        return horizontalDir;
                    }
                }
            }
            
            return GetRandomValidDirection();
        }
        
        int nextCornerIndex = (ghost4CurrentCornerIndex + 1) % outerCorners.Length;
        Vector2 nextCorner = outerCorners[nextCornerIndex];
        
        if (Vector2.Distance(currentWorldPos, nextCorner) < 1.5f)
        {
            ghost4CurrentCornerIndex = nextCornerIndex;
            Debug.Log($"Ghost 4: reached corner {ghost4CurrentCornerIndex}, continue clockwise");
            return GetGhost4Direction();
        }
        
        float currentDistance = Vector2.Distance(currentWorldPos, nextCorner);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            Vector2 bestDirection = validDirections[0];
            float bestDistance = Vector2.Distance(currentWorldPos + bestDirection, nextCorner);
            
            foreach (Vector2 dir in validDirections)
            {
                float newDistance = Vector2.Distance(currentWorldPos + dir, nextCorner);
                if (newDistance < bestDistance)
                {
                    bestDistance = newDistance;
                    bestDirection = dir;
                }
            }
            
            return bestDirection;
        }
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                return dir;
            }
        }
        
        return GetRandomValidDirection();
    }
    
    
    private Vector2 GetLeftDirection(Vector2 currentDir)
    {
        if (currentDir == Vector2.up) return Vector2.left;
        if (currentDir == Vector2.left) return Vector2.down;
        if (currentDir == Vector2.down) return Vector2.right;
        if (currentDir == Vector2.right) return Vector2.up;
        return Vector2.zero;
    }
    
    private Vector2 GetRightDirection(Vector2 currentDir)
    {
        if (currentDir == Vector2.up) return Vector2.right;
        if (currentDir == Vector2.right) return Vector2.down;
        if (currentDir == Vector2.down) return Vector2.left;
        if (currentDir == Vector2.left) return Vector2.up;
        return Vector2.zero;
    }
    
    private Vector2 GetPacStudentPosition()
    {
        if (gameManager != null)
        {
            PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
            if (pacStudent != null)
            {
                return pacStudent.GetCurrentGridPosition();
            }
        }
        
        return Vector2.zero;
    }
    

    private bool IsInCenterRoom()
    {
        Vector2 worldPos = new Vector2(transform.position.x, transform.position.y);
        
        bool inRoom = worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
                      worldPos.y >= -1.5f && worldPos.y <= 2.5f;
        
        return inRoom;
    }
    
    private bool IsInCenterRoomAt(Vector2 gridPos)
    {
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        return worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
               worldPos.y >= -1.5f && worldPos.y <= 2.5f;
    }
    
    private Vector2 GetExitCenterRoomDirection()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 exitDir = Vector2.zero;
        
        if (ghostNumber == 1 || ghostNumber == 3)
        {
            if (Mathf.Abs(currentWorldPos.x) > 0.5f)
            {
                exitDir = currentWorldPos.x > 0 ? Vector2.left : Vector2.right;
                // Debug.Log($"Ghost {ghostNumber}: first move to the center {exitDir}");
            }
            else
            {
                exitDir = Vector2.up;
                // Debug.Log($"Ghost {ghostNumber}: move to the top door");
            }
        }
        else if (ghostNumber == 2 || ghostNumber == 4)
        {
            if (Mathf.Abs(currentWorldPos.x) > 0.5f)
            {
                exitDir = currentWorldPos.x > 0 ? Vector2.left : Vector2.right;
                // Debug.Log($"Ghost {ghostNumber}: first move to the center {exitDir}");
            }
            else
            {
                exitDir = Vector2.down;
                // Debug.Log($"Ghost {ghostNumber}: move to the bottom door");
            }
        }
        
        return exitDir;
    }
    
    private Vector2 GetRandomValidDirectionWithCenterRoomAvoidance()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextPos = currentGridPosition + dir;
            Vector2 worldPos = GridToWorldPosition(nextPos);
            
            if (CanMoveToPosition(worldPos))
            {
                if (currentState != GhostState.Dead && IsInCenterRoomAt(nextPos))
                {
                    continue;
                }
                
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        return GetRandomValidDirection();
    }
}
