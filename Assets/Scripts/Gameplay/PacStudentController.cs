using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f; // 每秒移动的单元格数
    
    [Header("Audio")]
    public AudioManager audioManager;
    
    [Header("Animation")]
    public Animator animator;
    
    // 路径定义（按你的要求：从(1,1)开始，顺时针矩形路径，包含所有中间格子）
    // 这些是地图网格坐标，需要转换为世界坐标
    private Vector2[] gridPathPoints = new Vector2[]
    {
        new Vector2(1, 1), // 起点：左上角
        new Vector2(2, 1), // 向右
        new Vector2(3, 1),
        new Vector2(4, 1),
        new Vector2(5, 1),
        new Vector2(6, 1), // 右上角
        new Vector2(6, 2), // 向下
        new Vector2(6, 3),
        new Vector2(6, 4),
        new Vector2(6, 5), // 右下角
        new Vector2(5, 5), // 向左
        new Vector2(4, 5),
        new Vector2(3, 5),
        new Vector2(2, 5),
        new Vector2(1, 5), // 左下角
        new Vector2(1, 4), // 向上
        new Vector2(1, 3),
        new Vector2(1, 2),
        new Vector2(1, 1)  // 回到起点
    };
    
    // 移动状态
    private int currentPathIndex = 0;
    private int nextPathIndex = 1;
    private float pathProgress = 0f; // 当前路径段的进度 (0-1)
    private bool isMoving = false;
    
    // 方向参数（用于动画）
    private int moveX = 0;
    private int moveY = 0;
    
    // 音效控制
    private int lastPlayedPathIndex = -1; // 上次播放音效的路径索引
    
    void Start()
    {
        // 初始化位置到起点（网格坐标转世界坐标）
        Vector2 startWorldPos = GridToWorldPosition(gridPathPoints[0]);
        transform.position = new Vector3(startWorldPos.x, startWorldPos.y, 0);
        
        // 初始化音效位置
        lastPlayedPathIndex = 0;
        
        // 开始移动
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
        // Frame-rate independent movement using programmatic tweening
        Vector2 currentGridPoint = gridPathPoints[currentPathIndex];
        Vector2 nextGridPoint = gridPathPoints[nextPathIndex];
        
        // 转换为世界坐标
        Vector2 currentWorldPoint = GridToWorldPosition(currentGridPoint);
        Vector2 nextWorldPoint = GridToWorldPosition(nextGridPoint);
        
        // 计算路径段长度
        float segmentLength = Vector2.Distance(currentWorldPoint, nextWorldPoint);
        
        // 计算每帧的进度增量（frame-rate independent）
        float progressIncrement = (moveSpeed * Time.deltaTime) / segmentLength;
        pathProgress += progressIncrement;
        
        // 使用 Lerp 进行 programmatic tweening
        Vector2 currentWorldPosition = Vector2.Lerp(currentWorldPoint, nextWorldPoint, pathProgress);
        transform.position = new Vector3(currentWorldPosition.x, currentWorldPosition.y, 0);
        
        // 每帧更新动画方向
        UpdateDirection();
        PlayMoveAnimation();
        
        // 检查是否跨过了网格边界（播放音效）
        CheckForGridCrossing();
        
        // 检查是否到达下一个路径点
        if (pathProgress >= 1f)
        {
            OnReachPathPoint();
        }
    }
    
    private void OnReachPathPoint()
    {
        // 移动到下一个路径段
        currentPathIndex = nextPathIndex;
        nextPathIndex = (nextPathIndex + 1) % gridPathPoints.Length;
        pathProgress = 0f;
        
        // 音效会在 CheckForGridCrossing 中处理
        // 动画会在 MoveAlongPath 中每帧更新
        
        // 如果回到起点，重新开始循环
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
        
        // 设置方向参数（用于动画）
        // 注意：Unity Y轴向上为正，但我们的路径Y轴向下为正，需要反转
        moveX = Mathf.RoundToInt(direction.x);
        moveY = -Mathf.RoundToInt(direction.y); // 反转Y方向
    }
    
    // 检查是否跨过了网格边界（每走一步播放音效）
    private void CheckForGridCrossing()
    {
        // 直接使用当前路径索引，每到达一个路径点播放音效
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
    
    // 找到最接近当前位置的路径点索引
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
    
    // 网格坐标转世界坐标 - 使用实际Scene坐标
    private Vector2 GridToWorldPosition(Vector2 gridPos)
    {
        // 使用你提供的实际Scene坐标
        if (gridPos == new Vector2(1, 1)) return new Vector2(-12.5f, 13.5f);   // 起始点
        if (gridPos == new Vector2(2, 1)) return new Vector2(-11.5f, 13.5f);   // 向右
        if (gridPos == new Vector2(3, 1)) return new Vector2(-10.5f, 13.5f);
        if (gridPos == new Vector2(4, 1)) return new Vector2(-9.5f, 13.5f);
        if (gridPos == new Vector2(5, 1)) return new Vector2(-8.5f, 13.5f);
        if (gridPos == new Vector2(6, 1)) return new Vector2(-7.5f, 13.5f);    // 右上角
        if (gridPos == new Vector2(6, 2)) return new Vector2(-7.5f, 12.5f);    // 向下
        if (gridPos == new Vector2(6, 3)) return new Vector2(-7.5f, 11.5f);
        if (gridPos == new Vector2(6, 4)) return new Vector2(-7.5f, 10.5f);
        if (gridPos == new Vector2(6, 5)) return new Vector2(-7.5f, 9.5f);     // 右下角
        if (gridPos == new Vector2(5, 5)) return new Vector2(-8.5f, 9.5f);     // 向左
        if (gridPos == new Vector2(4, 5)) return new Vector2(-9.5f, 9.5f);
        if (gridPos == new Vector2(3, 5)) return new Vector2(-10.5f, 9.5f);
        if (gridPos == new Vector2(2, 5)) return new Vector2(-11.5f, 9.5f);
        if (gridPos == new Vector2(1, 5)) return new Vector2(-12.5f, 9.5f);    // 左下角
        if (gridPos == new Vector2(1, 4)) return new Vector2(-12.5f, 10.5f);   // 向上
        if (gridPos == new Vector2(1, 3)) return new Vector2(-12.5f, 11.5f);
        if (gridPos == new Vector2(1, 2)) return new Vector2(-12.5f, 12.5f);
        
        // 默认情况（不应该到达）
        return new Vector2(-12.5f, 13.5f);
    }
    
    // 世界坐标转网格坐标 - 使用实际Scene坐标
    private Vector2 WorldToGridPosition(Vector3 worldPos)
    {
        // 使用你提供的实际Scene坐标进行反向映射
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
        
        // 默认情况（不应该到达）
        return new Vector2(1, 1);
    }
    
    private void PlayMoveAnimation()
    {
        if (animator != null)
        {
            // 设置动画参数（按你的 Animator 设置）
            animator.SetInteger("MoveX", moveX);
            animator.SetInteger("MoveY", moveY);
            
            // 调试输出
            //Debug.Log($"Animation: MoveX={moveX}, MoveY={moveY}, PathIndex={currentPathIndex}");
        }
    }
    
    // 预留的碰撞检测接口
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 后续实现：吃豆、撞墙等逻辑
        if (other.CompareTag("Pellet"))
        {
            // 吃豆逻辑
            // Debug.Log("Ate a pellet!");
        }
        else if (other.CompareTag("PowerPellet"))
        {
            // 吃能量豆逻辑
            // Debug.Log("Ate a power pellet!");
        }
        else if (other.CompareTag("Wall"))
        {
            // 撞墙逻辑
            // Debug.Log("Hit a wall!");
        }
    }
    
    // 公共方法：暂停/恢复移动
    public void PauseMovement()
    {
        isMoving = false;
    }
    
    public void ResumeMovement()
    {
        isMoving = true;
    }
    
    // 公共方法：设置移动速度
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
