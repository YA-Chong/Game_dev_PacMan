using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("Ghost Settings")]
    public int ghostNumber = 1; // 幽灵编号 1-4
    public float moveSpeed = 1.8f; // 默认速度，会在Start中设置为PacStudent速度的90%
    
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
    private int recursionDepth = 0; // 防止无限递归
    
    // Ghost 3 专用：随机移动（记录上一次的方向）
    private Vector2 ghost3CurrentDirection = Vector2.zero;
    
    // Ghost 4 专用：顺时针绕外圈移动
    private Vector2 ghost4CurrentDirection = Vector2.zero;
    private bool ghost4ReachedOuterWall = false; // 是否已经到达外墙
    private int ghost4CurrentCornerIndex = -1; // 当前目标角的索引（0=左上, 1=右上, 2=右下, 3=左下）
    private bool ghost4IsEscapingDeadEnd = false; // 是否正在逃离死胡同
    private int ghost4EscapeStepCounter = 0; // 逃离步数计数器
    
    // 外圈四个角的坐标（顺时针顺序）
    private readonly Vector2[] outerCorners = new Vector2[]
    {
        new Vector2(-12.5f, 13.5f),  // 0: 左上角
        new Vector2(12.5f, 13.5f),   // 1: 右上角
        new Vector2(12.5f, -12.5f),  // 2: 右下角
        new Vector2(-12.5f, -12.5f)  // 3: 左下角
    };
    
    // 初始位置（用于死亡重生）
    private Vector2 initialWorldPosition;
    private bool isReturningToDead = false; // 是否正在返回初始位置（Dead状态）
    
    // 网格设置
    private float gridSize = 1f;
    
    void Start()
    {
        // 自动获取组件引用（从子对象获取Animator）
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (gameManager == null)
            gameManager = GameManager.Instance;
            
        // 设置速度为PacStudent速度的90%
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            moveSpeed = pacStudent.moveSpeed * 0.9f;
            // Debug.Log($"Ghost {ghostNumber}: 速度设置为 {moveSpeed} (PacStudent速度的90%)");
        }
        
        // 调试：显示初始位置信息
        Vector2 initialWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 initialGridPos = WorldToGridPosition(initialWorldPos);
        // Debug.Log($"Ghost {ghostNumber}: 初始世界坐标 {initialWorldPos}, 网格坐标 {initialGridPos}");
            
        // 调试信息
        if (animator == null)
        {
            Debug.LogError($"Ghost {ghostNumber}: 无法找到Animator组件！");
        }
        // else
        // {
        //     Debug.Log($"Ghost {ghostNumber}: 成功获取Animator组件");
        // }
            
        // 初始化网格位置
        Vector2 worldPos = new Vector2(transform.position.x, transform.position.y);
        currentGridPosition = WorldToGridPosition(worldPos);
        targetGridPosition = currentGridPosition;
        
        // 保存初始位置（用于死亡重生）
        initialWorldPosition = worldPos;
        
        // Debug.Log($"Ghost {ghostNumber}: 初始化 - 世界坐标 {worldPos}, 网格坐标 {currentGridPosition}, 初始位置已保存");
        
        // 设置初始状态
        SetGhostState(GhostState.Normal);
        
        // 设置初始朝向（向右）
        if (animator != null)
        {
            animator.SetFloat("MoveX", 1f); // 向右
            animator.SetFloat("MoveY", 0f);
        }
        
        // 开始移动
        StartMoving();
    }
    
    void Update()
    {
        // 检查游戏是否运行（倒计时期间不移动）
        if (gameManager != null && !gameManager.IsGameRunning())
        {
            return; // 倒计时期间不处理移动
        }
        
        // 更新动画参数
        UpdateAnimationParameters();
        
        // Dead状态下，使用协程直接移动到初始位置，不使用网格移动系统
        if (isReturningToDead)
        {
            return; // 协程正在处理移动
        }
        
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
        
        // 根据状态调整移动速度
        switch (newState)
        {
            case GhostState.Normal:
                moveSpeed = 1.8f; // 90% PacStudent speed
                
                // Ghost 4 特殊处理：恢复到Normal状态时，重置路径状态
                if (ghostNumber == 4)
                {
                    ghost4ReachedOuterWall = false;
                    ghost4CurrentDirection = Vector2.zero;
                    ghost4CurrentCornerIndex = -1;
                    ghost4IsEscapingDeadEnd = false;
                    ghost4EscapeStepCounter = 0;
                    // Debug.Log("Ghost 4: 恢复到Normal状态，重置路径状态");
                }
                break;
            case GhostState.Scared:
                moveSpeed = 0.9f; // 50% normal speed
                break;
            case GhostState.Recovering:
                moveSpeed = 0.9f; // Same as scared
                break;
            case GhostState.Dead:
                moveSpeed = 0.9f; // Same as scared
                // Dead状态：启动返回初始位置的协程
                StartCoroutine(ReturnToInitialPosition());
                break;
        }
        
        // 确保幽灵在状态切换后继续移动（Dead状态除外，因为它使用协程）
        if (!isMoving && newState != GhostState.Dead)
        {
            StartMoving();
        }
        
        // Debug.Log($"Ghost {ghostNumber} 状态变为: {newState}, 移动状态: {isMoving}");
    }
    
    // 获取下一个移动方向（根据状态和AI行为）
    private Vector2 GetNextDirection()
    {
        // 优先处理中心房间逻辑
        bool inCenterRoom = IsInCenterRoom();
        
        if (inCenterRoom && currentState != GhostState.Dead)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在中心房间 (位置: {currentGridPosition})，优先离开");
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
    
    
    // 尝试移动
    private void TryMove(Vector2 direction)
    {
        // 直接使用世界坐标移动，不使用网格坐标
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 nextWorldPos = currentWorldPos + direction;
        
        // Debug.Log($"Ghost {ghostNumber}: 尝试从 {currentWorldPos} 移动到 {nextWorldPos} (方向: {direction})");
        
        if (CanMoveToWorldPos(nextWorldPos))
        {
            // Debug.Log($"Ghost {ghostNumber}: 移动成功");
            targetGridPosition = WorldToGridPosition(nextWorldPos);
            lastDirection = direction;
            StartLerpToTarget();
        }
        else
        {
            // Debug.Log($"Ghost {ghostNumber}: 移动失败，被阻挡");
        }
    }
    
    // 检查是否可以移动到指定世界坐标
    private bool CanMoveToWorldPos(Vector2 worldPos)
    {
        // 检查边界
        if (Mathf.Abs(worldPos.x) > 30 || Mathf.Abs(worldPos.y) > 30)
            return false;
            
        // Dead状态可以穿墙
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        // 检查墙壁碰撞
        Collider2D wall = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wall != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到墙壁");
            return false;
        }
        
        // 检查传送门碰撞（幽灵不能通过传送门）
        Collider2D teleporter = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Teleporter"));
        if (teleporter != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到传送门，视为墙壁");
            return false;
        }
        
        // 检查门碰撞
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到TopDoor");
            // 只有Ghost 1&3可以通过上门
            if (ghostNumber == 1 || ghostNumber == 3)
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor允许通过");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor不允许通过");
                return false;
            }
        }
        
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到BottomDoor");
            // 只有Ghost 2&4可以通过下门
            if (ghostNumber == 2 || ghostNumber == 4)
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor允许通过");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor不允许通过");
                return false;
            }
        }
        
        return true;
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
            
        // Dead状态可以穿墙
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        // 检查墙壁碰撞
        Collider2D wall = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wall != null)
        {
            return false;
        }
        
        // 检查门碰撞
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到TopDoor");
            // 只有Ghost 1&3可以通过上门
            if (ghostNumber == 1 || ghostNumber == 3)
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor允许通过");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: TopDoor不允许通过");
                return false;
            }
        }
        
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null)
        {
            // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 检测到BottomDoor");
            // 只有Ghost 2&4可以通过下门
            if (ghostNumber == 2 || ghostNumber == 4)
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor允许通过");
                return true;
            }
            else
            {
                // Debug.Log($"Ghost {ghostNumber}: BottomDoor不允许通过");
                return false;
            }
        }
        
        // Debug.Log($"Ghost {ghostNumber}: 在 {worldPos} 没有检测到任何门或墙");
        
        return true;
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
    
    // 网格坐标转世界坐标（照抄PacStudent的坐标系统）
    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        // 使用统一的全局坐标系统，避免象限切换问题
        // 基于左上象限的坐标系统，但支持所有象限
        
        // 计算相对于左上象限原点的偏移
        float offsetX = (gridPos.x - 1) * 1.0f;
        float offsetY = -(gridPos.y - 1) * 1.0f;
        
        // 基础坐标（左上象限原点）
        float baseX = -12.5f;
        float baseY = 13.5f;
        
        // 计算最终世界坐标
        float worldX = baseX + offsetX;
        float worldY = baseY + offsetY;
        
        return new Vector2(worldX, worldY);
    }
    
    // 世界坐标转网格坐标（照抄PacStudent的坐标系统）
    private Vector2 WorldToGridPosition(Vector2 worldPos)
    {
        // 使用统一的全局坐标系统，避免象限切换问题
        // 基于左上象限的坐标系统，但支持所有象限
        
        // 计算相对于左上象限原点的偏移
        float offsetX = worldPos.x - (-12.5f);
        float offsetY = 13.5f - worldPos.y;
        
        // 转换为网格坐标
        int gridX = Mathf.RoundToInt(1 + offsetX / 1.0f);
        int gridY = Mathf.RoundToInt(1 + offsetY / 1.0f);
        
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
    
    // ========== 移动系统 ==========
    
    // 开始移动
    private void StartMoving()
    {
        if (!isMoving)
        {
            isMoving = true;
            // AI决策现在在Update()的GetNextDirection()中处理
        }
    }
    
    // 停止移动
    private void StopMoving()
    {
        isMoving = false;
        isLerping = false;
    }
    
    // 选择下一个移动方向（已弃用，使用GetNextDirection()代替）
    /*
    private void ChooseNextDirection()
    {
        // 这个方法已经被废弃，AI决策现在在GetNextDirection()中处理
    }
    */
    
    // 移动到指定方向（已弃用，使用TryMove代替）
    private void MoveToDirection(Vector2 direction)
    {
        // 这个方法已经被TryMove替代，不应该被调用
        Debug.LogWarning($"Ghost {ghostNumber}: MoveToDirection被调用，这不应该发生！使用TryMove代替");
    }
    
    // 检查是否可以移动到指定位置
    private bool CanMoveToPosition(Vector2 worldPos)
    {
        // Dead状态可以穿墙
        if (currentState == GhostState.Dead)
        {
            return true;
        }
        
        // 检查墙壁碰撞
        Collider2D wallCollider = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        if (wallCollider != null)
        {
            // 检查是否是中心房间的门
            if (IsCenterRoomDoor(worldPos))
            {
                return true; // 幽灵可以通过中心房间的门
            }
            return false; // 其他墙壁不能通过
        }
        
        return true;
    }
    
    // 检查是否是中心房间的门
    private bool IsCenterRoomDoor(Vector2 worldPos)
    {
        Vector2 gridPos = WorldToGridPosition(worldPos);
        
        // 检查是否是幽灵可以通行的门
        if (IsGhostDoor(worldPos))
        {
            return true;
        }
        
        return false;
    }
    
    // 检查是否是幽灵可以通行的门
    private bool IsGhostDoor(Vector2 worldPos)
    {
        // 检查上门（Ghost 1&3使用）
        Collider2D topDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("TopDoor"));
        if (topDoor != null && (ghostNumber == 1 || ghostNumber == 3))
        {
            return true;
        }
        
        // 检查下门（Ghost 2&4使用）
        Collider2D bottomDoor = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("BottomDoor"));
        if (bottomDoor != null && (ghostNumber == 2 || ghostNumber == 4))
        {
            return true;
        }
        
        return false;
    }
    
    // 移动到目标的协程（已弃用，使用Update()中的LerpToTarget()代替）
    /*
    private System.Collections.IEnumerator MoveToTarget()
    {
        // 这个方法已经被废弃，移动逻辑现在在Update()中处理
    }
    */
    
    // 获取随机有效方向
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
    
    
    // ========== AI行为系统 ==========
    
    // Normal状态的AI行为
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
    
    // Scared/Recovering状态的AI行为（使用Ghost 1的行为）
    private Vector2 GetScaredStateDirection()
    {
        return GetGhost1Direction();
    }
    
    // Dead状态：返回初始位置的协程（模仿PacStudent的死亡移动）
    private System.Collections.IEnumerator ReturnToInitialPosition()
    {
        isReturningToDead = true;
        isMoving = false;
        isLerping = false;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(initialWorldPosition.x, initialWorldPosition.y, -1f);
        
        // Dead状态速度和Scared/Recovering一样（0.9f，即50% normal speed）
        float deadSpeed = 0.9f; // 与Scared/Recovering相同
        float distance = Vector3.Distance(startPosition, targetPosition);
        float moveTime = distance / deadSpeed;
        float elapsedTime = 0f;
        
        // Debug.Log($"Ghost {ghostNumber}: 开始返回初始位置 {targetPosition}，距离={distance:F2}，速度={deadSpeed}，预计时间={moveTime:F2}秒");
        
        // 循环移动（无视障碍物，直线移动）
        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveTime;
            
            // 平滑移动到初始位置（无视障碍物）
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.position = newPosition;
            
            yield return null;
        }
        
        // 确保到达目标位置
        transform.position = targetPosition;
        
        // Debug.Log($"Ghost {ghostNumber}: 到达初始位置");
        
        // 检查其他幽灵的状态，决定重生后的状态
        GhostState respawnState = DetermineRespawnState();
        
        Debug.Log($"Ghost {ghostNumber}: 准备重生为 {respawnState} 状态");
        
        // 重置标志（必须在SetGhostState之前）
        isReturningToDead = false;
        
        // 更新网格位置
        currentGridPosition = WorldToGridPosition(new Vector2(transform.position.x, transform.position.y));
        targetGridPosition = currentGridPosition;
        
        // 直接修改状态，不调用SetGhostState（避免重复触发协程）
        // ⚠️ 必须在调用ExitGhostDie()之前修改状态，否则GameManager检查时会认为还是Dead状态
        currentState = respawnState;
        
        // 通知GameManager恢复BGM
        if (gameManager != null)
        {
            gameManager.ExitGhostDie();
        }
        
        // Debug.Log($"Ghost {ghostNumber}: 状态已修改为 {respawnState}，当前currentState = {currentState}");
        
        // 手动更新Animator状态
        if (animator != null)
        {
            // 先清除所有状态
            animator.SetBool("IsNormal", false);
            animator.SetBool("IsScared", false);
            animator.SetBool("IsRecovering", false);
            animator.SetBool("IsDead", false);
            
            // Debug.Log($"Ghost {ghostNumber}: 已清除所有Animator状态");
            
            // 等待一帧，确保状态清除生效
            yield return null;
            
            // 设置新状态
            switch (respawnState)
            {
                case GhostState.Normal:
                    animator.SetBool("IsNormal", true);
                    animator.SetFloat("MoveX", 1f);
                    animator.SetFloat("MoveY", 0f);
                    moveSpeed = 1.8f;
                    
                    // Ghost 4 特殊处理：恢复到Normal状态时，重置路径状态
                    if (ghostNumber == 4)
                    {
                        ghost4ReachedOuterWall = false;
                        ghost4CurrentDirection = Vector2.zero;
                        ghost4CurrentCornerIndex = -1;
                        ghost4IsEscapingDeadEnd = false;
                        ghost4EscapeStepCounter = 0;
                        // Debug.Log("Ghost 4: 重生为Normal状态，重置路径状态");
                    }
                    
                    // Debug.Log($"Ghost {ghostNumber}: 设置为Normal状态，IsNormal=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator状态验证 - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
                    
                case GhostState.Scared:
                    animator.SetBool("IsScared", true);
                    moveSpeed = 0.9f;
                    // Debug.Log($"Ghost {ghostNumber}: 设置为Scared状态，IsScared=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator状态验证 - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
                    
                case GhostState.Recovering:
                    animator.SetBool("IsRecovering", true);
                    moveSpeed = 0.9f;
                    // Debug.Log($"Ghost {ghostNumber}: 设置为Recovering状态，IsRecovering=true");
                    // Debug.Log($"Ghost {ghostNumber}: Animator状态验证 - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
                    break;
            }
            
            // 再等待一帧，确保新状态生效
            yield return null;
            
            // 最终验证
            // Debug.Log($"Ghost {ghostNumber}: 最终Animator状态 - IsNormal:{animator.GetBool("IsNormal")}, IsScared:{animator.GetBool("IsScared")}, IsRecovering:{animator.GetBool("IsRecovering")}, IsDead:{animator.GetBool("IsDead")}");
        }
        
        // 重新启动移动
        StartMoving();
        
        Debug.Log($"Ghost {ghostNumber}: 重生完成，最终状态 {currentState}");
    }
    
    // Dead状态的AI行为（已废弃，使用协程代替）
    private Vector2 GetDeadStateDirection()
    {
        // 这个方法不再使用，Dead状态使用ReturnToInitialPosition()协程
        return Vector2.zero;
    }
    
    // 根据其他幽灵的状态决定重生后的状态
    private GhostState DetermineRespawnState()
    {
        // 查找所有幽灵
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        
        // 检查是否有其他幽灵处于Scared或Recovering状态
        foreach (GhostController ghost in ghosts)
        {
            if (ghost == this) continue; // 跳过自己
            
            GhostState otherState = ghost.GetCurrentState();
            
            // 如果有幽灵处于Recovering状态，重生为Recovering
            if (otherState == GhostState.Recovering)
            {
                // Debug.Log($"Ghost {ghostNumber}: 检测到其他幽灵处于Recovering状态");
                return GhostState.Recovering;
            }
            
            // 如果有幽灵处于Scared状态，重生为Scared
            if (otherState == GhostState.Scared)
            {
                // Debug.Log($"Ghost {ghostNumber}: 检测到其他幽灵处于Scared状态");
                return GhostState.Scared;
            }
        }
        
        // 如果没有幽灵处于Scared/Recovering状态，重生为Normal
        // Debug.Log($"Ghost {ghostNumber}: 没有检测到Scared/Recovering幽灵，重生为Normal");
        return GhostState.Normal;
    }
    
    // Ghost 1: 远离PacStudent（智能版本，避免回退）
    private Vector2 GetGhost1Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 pacStudentWorldPos = GetPacStudentWorldPosition();
        float currentDistance = Vector2.Distance(currentWorldPos, pacStudentWorldPos);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<Vector2> allValidDirections = new System.Collections.Generic.List<Vector2>();
        
        // 计算反方向（不能往回走）
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos))
            {
                // 避免回到中心房间
                if (currentState != GhostState.Dead && IsWorldPosInCenterRoom(nextWorldPos))
                {
                    continue;
                }
                
                // 跳过反方向（除非是初始状态）
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                allValidDirections.Add(dir);
                
                float newDistance = Vector2.Distance(nextWorldPos, pacStudentWorldPos);
                if (newDistance >= currentDistance) // 距离更远或相等
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        // 优先选择能远离PacStudent的方向
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        // 如果没有能远离的方向，选择任意可行方向（避免卡住）
        if (allValidDirections.Count > 0)
        {
            // Debug.Log($"Ghost 1: 无法远离，选择任意可行方向（不回头）");
            return allValidDirections[Random.Range(0, allValidDirections.Count)];
        }
        
        // 如果真的被困，允许回头
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                // Debug.Log($"Ghost 1: 被困，允许回头");
                return dir;
            }
        }
        
        // 最后回退到完全随机
        return GetRandomValidDirection();
    }
    
    // 获取PacStudent的世界坐标
    private Vector2 GetPacStudentWorldPosition()
    {
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            return new Vector2(pacStudent.transform.position.x, pacStudent.transform.position.y);
        }
        return Vector2.zero;
    }
    
    // 检查世界坐标是否在中心房间
    private bool IsWorldPosInCenterRoom(Vector2 worldPos)
    {
        return worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
               worldPos.y >= -1.5f && worldPos.y <= 2.5f;
    }
    
    // Ghost 2: 追逐PacStudent（智能版本，避免回退）
    private Vector2 GetGhost2Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 pacStudentWorldPos = GetPacStudentWorldPosition();
        float currentDistance = Vector2.Distance(currentWorldPos, pacStudentWorldPos);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<Vector2> allValidDirections = new System.Collections.Generic.List<Vector2>();
        
        // 计算反方向（不能往回走）
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos))
            {
                // 避免回到中心房间
                if (currentState != GhostState.Dead && IsWorldPosInCenterRoom(nextWorldPos))
                {
                    continue;
                }
                
                // 跳过反方向（除非是初始状态）
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                allValidDirections.Add(dir);
                
                float newDistance = Vector2.Distance(nextWorldPos, pacStudentWorldPos);
                if (newDistance <= currentDistance) // 距离更近或相等
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        // 优先选择能接近PacStudent的方向
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        // 如果没有能接近的方向，选择任意可行方向（避免卡住）
        if (allValidDirections.Count > 0)
        {
            // Debug.Log($"Ghost 2: 无法接近，选择任意可行方向（不回头）");
            return allValidDirections[Random.Range(0, allValidDirections.Count)];
        }
        
        // 如果真的被困，允许回头
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                // Debug.Log($"Ghost 2: 被困，允许回头");
                return dir;
            }
        }
        
        // 最后回退到完全随机
        return GetRandomValidDirection();
    }
    
    // Ghost 3: 随机移动（每格换方向，不走回头路）
    private Vector2 GetGhost3Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        
        // 每次都选择新方向
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        // 计算反方向（不能往回走）
        Vector2 oppositeDirection = -ghost3CurrentDirection;
        
        foreach (Vector2 dir in directions)
        {
            // 跳过反方向
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
            // 随机选择一个新方向（不包括反方向）
            ghost3CurrentDirection = validDirections[Random.Range(0, validDirections.Count)];
            // Debug.Log($"Ghost 3: 选择新方向 {ghost3CurrentDirection}");
            return ghost3CurrentDirection;
        }
        
        // 如果没有有效方向（被困），允许往回走
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
            Debug.Log($"Ghost 3: 被困，允许回头 {ghost3CurrentDirection}");
            return ghost3CurrentDirection;
        }
        
        // 最后回退到完全随机
        return GetRandomValidDirection();
    }
    
    // Ghost 4: 顺时针绕外圈移动
    private Vector2 GetGhost4Direction()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        
        // 阶段1：先移动到上边缘或下边缘，然后沿边缘到角落
        if (!ghost4ReachedOuterWall)
        {
            // 目标：先到达上边缘（Y = 13.5）或下边缘（Y = -12.5）
            float targetYTop = 13.5f;
            float targetYBottom = -12.5f;
            float currentY = currentWorldPos.y;
            
            // 选择最近的边缘
            bool targetTop = Mathf.Abs(currentY - targetYTop) < Mathf.Abs(currentY - targetYBottom);
            float targetY = targetTop ? targetYTop : targetYBottom;
            
            // 如果还没到达目标边缘，优先纵向移动
            if (Mathf.Abs(currentY - targetY) > 0.5f)
            {
                Vector2 verticalDir = (targetY > currentY) ? Vector2.up : Vector2.down;
                Vector2 checkPos = currentWorldPos + verticalDir;
                
                if (CanMoveToWorldPos(checkPos) && !IsWorldPosInCenterRoom(checkPos))
                {
                    // Debug.Log($"Ghost 4: 向{(targetTop ? "上" : "下")}移动到边缘");
                    return verticalDir;
                }
                
                // 纵向被阻挡，尝试横向绕路
                Vector2 checkPosLeft = currentWorldPos + Vector2.left;
                Collider2D teleporterLeft = Physics2D.OverlapPoint(checkPosLeft, LayerMask.GetMask("Teleporter"));
                if (teleporterLeft == null && CanMoveToWorldPos(checkPosLeft) && !IsWorldPosInCenterRoom(checkPosLeft))
                {
                    // Debug.Log($"Ghost 4: 向左绕路");
                    return Vector2.left;
                }
                
                Vector2 checkPosRight = currentWorldPos + Vector2.right;
                if (CanMoveToWorldPos(checkPosRight) && !IsWorldPosInCenterRoom(checkPosRight))
                {
                    // Debug.Log($"Ghost 4: 向右绕路");
                    return Vector2.right;
                }
            }
            else
            {
                // 已经到达边缘，沿边缘移动到最近的角
                if (ghost4CurrentCornerIndex == -1)
                {
                    // 选择该边缘上最近的角
                    if (targetTop)
                    {
                        // 上边缘：选择左上（0）或右上（1）
                        float distToTopLeft = Vector2.Distance(currentWorldPos, outerCorners[0]);
                        float distToTopRight = Vector2.Distance(currentWorldPos, outerCorners[1]);
                        ghost4CurrentCornerIndex = (distToTopLeft < distToTopRight) ? 0 : 1;
                    }
                    else
                    {
                        // 下边缘：选择左下（3）或右下（2）
                        float distToBottomLeft = Vector2.Distance(currentWorldPos, outerCorners[3]);
                        float distToBottomRight = Vector2.Distance(currentWorldPos, outerCorners[2]);
                        ghost4CurrentCornerIndex = (distToBottomLeft < distToBottomRight) ? 3 : 2;
                    }
                    Debug.Log($"Ghost 4: 到达{(targetTop ? "上" : "下")}边缘，选择角 {ghost4CurrentCornerIndex}");
                }
                
                Vector2 targetCorner = outerCorners[ghost4CurrentCornerIndex];
                
                // 检查是否到达目标角
                if (Vector2.Distance(currentWorldPos, targetCorner) < 1.5f)
                {
                    ghost4ReachedOuterWall = true;
                    Debug.Log($"Ghost 4: 到达角 {ghost4CurrentCornerIndex}，开始绕外圈");
                    return GetGhost4Direction();
                }
                
                // 沿边缘移动到目标角（只需要横向移动）
                float deltaX = targetCorner.x - currentWorldPos.x;
                
                if (Mathf.Abs(deltaX) > 0.5f)
                {
                    Vector2 horizontalDir = deltaX > 0 ? Vector2.right : Vector2.left;
                    Vector2 checkPos = currentWorldPos + horizontalDir;
                    
                    if (CanMoveToWorldPos(checkPos) && !IsWorldPosInCenterRoom(checkPos))
                    {
                        // Debug.Log($"Ghost 4: 沿边缘移动 {horizontalDir}");
                        return horizontalDir;
                    }
                }
            }
            
            // 如果都不行，随机选择
            return GetRandomValidDirection();
        }
        
        // 阶段2：沿外圈顺时针移动（使用智能寻路）
        int nextCornerIndex = (ghost4CurrentCornerIndex + 1) % outerCorners.Length; // 顺时针下一个角
        Vector2 nextCorner = outerCorners[nextCornerIndex];
        
        // 检查是否到达下一个角
        if (Vector2.Distance(currentWorldPos, nextCorner) < 1.5f)
        {
            ghost4CurrentCornerIndex = nextCornerIndex;
            Debug.Log($"Ghost 4: 到达角 {ghost4CurrentCornerIndex}，继续顺时针");
            return GetGhost4Direction();
        }
        
        // 使用智能寻路（允许绕路）
        float currentDistance = Vector2.Distance(currentWorldPos, nextCorner);
        
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        System.Collections.Generic.List<Vector2> validDirections = new System.Collections.Generic.List<Vector2>();
        
        // 计算反方向（避免往回走）
        Vector2 oppositeDirection = -lastDirection;
        
        foreach (Vector2 dir in directions)
        {
            Vector2 nextWorldPos = currentWorldPos + dir;
            
            if (CanMoveToWorldPos(nextWorldPos) && !IsWorldPosInCenterRoom(nextWorldPos))
            {
                // 跳过反方向（除非是初始状态）
                if (lastDirection != Vector2.zero && dir == oppositeDirection)
                {
                    continue;
                }
                
                validDirections.Add(dir);
            }
        }
        
        // 如果有可行方向，选择能让距离最近的那个
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
        
        // 如果都被阻挡（包括反方向），允许往回走
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
    
    
    // 获取左边的方向（顺时针90度）
    private Vector2 GetLeftDirection(Vector2 currentDir)
    {
        if (currentDir == Vector2.up) return Vector2.left;
        if (currentDir == Vector2.left) return Vector2.down;
        if (currentDir == Vector2.down) return Vector2.right;
        if (currentDir == Vector2.right) return Vector2.up;
        return Vector2.zero;
    }
    
    // 获取右边的方向（逆时针90度）
    private Vector2 GetRightDirection(Vector2 currentDir)
    {
        if (currentDir == Vector2.up) return Vector2.right;
        if (currentDir == Vector2.right) return Vector2.down;
        if (currentDir == Vector2.down) return Vector2.left;
        if (currentDir == Vector2.left) return Vector2.up;
        return Vector2.zero;
    }
    
    // 获取PacStudent的位置
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
        
        return Vector2.zero; // 默认位置
    }
    
    // 从中心房间离开（已弃用，逻辑已整合到GetExitCenterRoomDirection()）
    /*
    private void ExitCenterRoom()
    {
        // 这个方法已经被废弃，逻辑现在在GetExitCenterRoomDirection()中处理
    }
    */
    
    // 检查是否在中心房间
    private bool IsInCenterRoom()
    {
        // 中心房间的世界坐标范围：从(-3.5, -1.5)到(3.5, 2.5)
        Vector2 worldPos = new Vector2(transform.position.x, transform.position.y);
        
        bool inRoom = worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
                      worldPos.y >= -1.5f && worldPos.y <= 2.5f;
        
        return inRoom;
    }
    
    // 检查指定位置是否在中心房间
    private bool IsInCenterRoomAt(Vector2 gridPos)
    {
        // 将网格坐标转换为世界坐标
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        return worldPos.x >= -3.5f && worldPos.x <= 3.5f && 
               worldPos.y >= -1.5f && worldPos.y <= 2.5f;
    }
    
    // 获取离开中心房间的方向
    private Vector2 GetExitCenterRoomDirection()
    {
        Vector2 currentWorldPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 exitDir = Vector2.zero;
        
        if (ghostNumber == 1 || ghostNumber == 3)
        {
            // Ghost 1&3 从上门离开
            // 上门位置：(-0.5, 2.5) 和 (0.5, 2.5)
            // 先向门的X坐标移动，然后向上
            if (Mathf.Abs(currentWorldPos.x) > 0.5f)
            {
                // 先向中间移动
                exitDir = currentWorldPos.x > 0 ? Vector2.left : Vector2.right;
                // Debug.Log($"Ghost {ghostNumber}: 先向中间移动 {exitDir}");
            }
            else
            {
                // 已经在中间，向上移动
                exitDir = Vector2.up;
                // Debug.Log($"Ghost {ghostNumber}: 向上门移动");
            }
        }
        else if (ghostNumber == 2 || ghostNumber == 4)
        {
            // Ghost 2&4 从下门离开
            // 下门位置：(-0.5, -1.5) 和 (0.5, -1.5)
            // 先向门的X坐标移动，然后向下
            if (Mathf.Abs(currentWorldPos.x) > 0.5f)
            {
                // 先向中间移动
                exitDir = currentWorldPos.x > 0 ? Vector2.left : Vector2.right;
                // Debug.Log($"Ghost {ghostNumber}: 先向中间移动 {exitDir}");
            }
            else
            {
                // 已经在中间，向下移动
                exitDir = Vector2.down;
                // Debug.Log($"Ghost {ghostNumber}: 向下门移动");
            }
        }
        
        return exitDir;
    }
    
    // 获取随机有效方向（避免中心房间）
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
                // 避免回到中心房间（除非是Dead状态）
                if (currentState != GhostState.Dead && IsInCenterRoomAt(nextPos))
                {
                    continue; // 跳过这个方向
                }
                
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        
        // 如果没有有效方向，回退到普通随机选择
        return GetRandomValidDirection();
    }
}
