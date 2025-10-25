using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("Ghost Settings")]
    public int ghostNumber = 1; // 幽灵编号 1-4
    public float moveSpeed = 1.8f; // 90% of PacStudent speed
    
    [Header("Animation")]
    public Animator animator;
    
    [Header("Game Manager")]
    public GameManager gameManager;
    
    // 幽灵状态
    public enum GhostState
    {
        Normal,
        Scared,
        Recovering,
        Dead
    }
    
    private GhostState currentState = GhostState.Normal;
    private bool isMoving = false;
    
    // 移动相关
    private Vector2 currentGridPosition;
    private Vector2 targetGridPosition;
    private bool isLerping = false;
    private float lerpProgress = 0f;
    private Vector2 lastDirection = Vector2.zero;
    
    // 网格设置
    private float gridSize = 1f;
    
    void Start()
    {
        // 自动获取组件引用（从子对象获取Animator）
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (gameManager == null)
            gameManager = GameManager.Instance;
            
        // 调试信息
        if (animator == null)
        {
            Debug.LogError($"Ghost {ghostNumber}: 无法找到Animator组件！");
        }
        else
        {
            Debug.Log($"Ghost {ghostNumber}: 成功获取Animator组件");
        }
            
        // 初始化网格位置
        Vector2 worldPos = new Vector2(transform.position.x, transform.position.y);
        currentGridPosition = WorldToGridPosition(worldPos);
        targetGridPosition = currentGridPosition;
        
        // 设置初始状态
        SetGhostState(GhostState.Normal);
        
        // 设置初始朝向（向右）
        if (animator != null)
        {
            animator.SetFloat("MoveX", 1f); // 向右
            animator.SetFloat("MoveY", 0f);
        }
    }
    
    void Update()
    {
        // 更新动画参数
        UpdateAnimationParameters();
        
        
        // 处理移动
        if (isLerping)
        {
            LerpToTarget();
        }
        else if (isMoving)
        {
            // 根据状态决定移动方向
            Vector2 nextDirection = GetNextDirection();
            if (nextDirection != Vector2.zero)
            {
                TryMove(nextDirection);
            }
        }
    }
    
    // 设置幽灵状态
    public void SetGhostState(GhostState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        // 更新动画参数
        if (animator != null)
        {
            // 根据当前状态和目标状态，设置正确的参数
            switch (newState)
            {
                case GhostState.Normal:
                    // Normal状态：只设置IsNormal=true，其他为false
                    animator.SetBool("IsNormal", true);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", false);
                    // 确保Blend Tree有正确的方向参数
                    animator.SetFloat("MoveX", 1f);
                    animator.SetFloat("MoveY", 0f);
                    break;
                    
                case GhostState.Scared:
                    // Scared状态：使用状态机控制
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", true);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", false);
                    break;
                    
                case GhostState.Recovering:
                    // Recovering状态：使用状态机控制
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", true);
                    animator.SetBool("IsDead", false);
                    break;
                    
                case GhostState.Dead:
                    // Dead状态：使用状态机控制
                    animator.SetBool("IsNormal", false);
                    animator.SetBool("IsScared", false);
                    animator.SetBool("IsRecovering", false);
                    animator.SetBool("IsDead", true);
                    break;
            }
            
        }
        else
        {
            Debug.LogError($"Ghost {ghostNumber}: Animator为空，无法设置状态 {newState}");
        }
        
        // 根据状态调整移动速度（暂时禁用移动，等待85%档实现）
        switch (newState)
        {
            case GhostState.Normal:
                moveSpeed = 1.8f; // 90% PacStudent speed
                isMoving = false; // 暂时禁用移动
                break;
            case GhostState.Scared:
                moveSpeed = 0.9f; // 50% normal speed
                isMoving = false; // 暂时禁用移动
                break;
            case GhostState.Recovering:
                moveSpeed = 0.9f; // Same as scared
                isMoving = false; // 暂时禁用移动
                break;
            case GhostState.Dead:
                moveSpeed = 0.9f; // Same as scared
                isMoving = false; // 暂时禁用移动
                break;
        }
        
        Debug.Log($"Ghost {ghostNumber} 状态变为: {newState}");
    }
    
    // 获取下一个移动方向（根据状态和AI行为）
    private Vector2 GetNextDirection()
    {
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
    
    // 正常状态AI（暂时随机移动）
    private Vector2 GetNormalStateDirection()
    {
        // 简单的随机移动，避免反向
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 validDirection = Vector2.zero;
        
        foreach (Vector2 dir in directions)
        {
            if (dir != -lastDirection && CanMoveTo(currentGridPosition + dir))
            {
                validDirection = dir;
                break;
            }
        }
        
        return validDirection;
    }
    
    // 恐惧状态AI（远离PacStudent）
    private Vector2 GetScaredStateDirection()
    {
        // 简单的远离PacStudent逻辑
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 validDirection = Vector2.zero;
        
        foreach (Vector2 dir in directions)
        {
            if (dir != -lastDirection && CanMoveTo(currentGridPosition + dir))
            {
                validDirection = dir;
                break;
            }
        }
        
        return validDirection;
    }
    
    // 死亡状态AI（朝向重生点）
    private Vector2 GetDeadStateDirection()
    {
        // 朝向重生点的简单逻辑
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 validDirection = Vector2.zero;
        
        foreach (Vector2 dir in directions)
        {
            if (dir != -lastDirection && CanMoveTo(currentGridPosition + dir))
            {
                validDirection = dir;
                break;
            }
        }
        
        return validDirection;
    }
    
    // 尝试移动
    private void TryMove(Vector2 direction)
    {
        Vector2 nextGridPos = currentGridPosition + direction;
        
        if (CanMoveTo(nextGridPos))
        {
            targetGridPosition = nextGridPos;
            lastDirection = direction;
            StartLerpToTarget();
        }
    }
    
    // 开始插值移动
    private void StartLerpToTarget()
    {
        isLerping = true;
        lerpProgress = 0f;
    }
    
    // 插值移动到目标
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
    
    // 检查是否可以移动到指定位置
    private bool CanMoveTo(Vector2 gridPos)
    {
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        // 检查边界
        if (Mathf.Abs(gridPos.x) > 30 || Mathf.Abs(gridPos.y) > 30)
            return false;
            
        // 检查墙壁碰撞
        Collider2D wall = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        return wall == null;
    }
    
    // 更新动画参数
    private void UpdateAnimationParameters()
    {
        if (animator != null)
        {
            // 只在Normal状态下更新移动方向参数（用于Blend Tree）
            if (currentState == GhostState.Normal && lastDirection != Vector2.zero)
            {
                animator.SetFloat("MoveX", lastDirection.x);
                animator.SetFloat("MoveY", lastDirection.y);
            }
            
            // 设置移动状态参数（如果Animator中有IsMoving参数）
            // animator.SetBool("IsMoving", isMoving);
        }
    }
    
    // 网格坐标转世界坐标
    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        float worldX = -12.5f + (gridPos.x - 1) * gridSize;
        float worldY = 13.5f - (gridPos.y - 1) * gridSize;
        return new Vector2(worldX, worldY);
    }
    
    // 世界坐标转网格坐标
    private Vector2 WorldToGridPosition(Vector2 worldPos)
    {
        float offsetX = worldPos.x - (-12.5f);
        float offsetY = 13.5f - worldPos.y;
        
        int gridX = Mathf.RoundToInt(1 + offsetX / gridSize);
        int gridY = Mathf.RoundToInt(1 + offsetY / gridSize);
        
        return new Vector2(gridX, gridY);
    }
    
    // 设置幽灵位置
    public void SetPosition(Vector2 gridPos)
    {
        currentGridPosition = gridPos;
        targetGridPosition = gridPos;
        Vector2 worldPos = GridToWorldPosition(gridPos);
        transform.position = new Vector3(worldPos.x, worldPos.y, -1);
        isLerping = false;
    }
    
    // 重生幽灵
    public void RespawnGhost()
    {
        if (animator != null)
        {
            // 先重置所有状态参数
            animator.SetBool("IsNormal", false);
            animator.SetBool("IsScared", false);
            animator.SetBool("IsRecovering", false);
            animator.SetBool("IsDead", false);
            
            // 使用Respawn触发器
            animator.SetTrigger("Respawn");
            Debug.Log($"Ghost {ghostNumber}: 使用Respawn触发器重生");
        }
        SetGhostState(GhostState.Normal);
    }
    
    // 获取当前状态
    public GhostState GetCurrentState()
    {
        return currentState;
    }
}
