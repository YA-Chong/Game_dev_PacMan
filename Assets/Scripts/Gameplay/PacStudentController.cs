using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    [Header("Audio")]
    public AudioManager audioManager;
    
    [Header("Particle Effects")]
    public ParticleSystem dustParticleSystem;
    [Tooltip("撞墙时播放的一次性碰撞粒子（与移动灰尘不同）")]
    public ParticleSystem wallCollisionParticle;
    
    private bool isWallCollisionPlaying = false;

    [Header("Animation")]
    public Animator animator;

    [Header("Grid Settings")]
    public float gridSize = 1f; // 网格大小

    // 移动状态
    private Vector2 currentGridPosition;
    private Vector2 targetGridPosition;
    private Vector2 lastInput;
    private Vector2 currentInput;
    private bool isLerping = false;
    private float lerpProgress = 0f;
    
    // 音效防重复播放
    private bool hasPlayedWallSound = false;
    private Vector2 lastWallCollisionPosition;

    // 移动方向
    private int moveX = 0;
    private int moveY = 0;

    void Start()
    {
        // 初始化网格位置（左上角开始位置）
        // 根据原来的代码，左上角是(1, 1)对应(-12.5, 13.5)
        currentGridPosition = new Vector2(1, 1);
        targetGridPosition = currentGridPosition;
        
        // 使用原来的GridToWorldPosition函数
        Vector2 startWorldPos = GridToWorldPosition(currentGridPosition);
        transform.position = new Vector3(startWorldPos.x, startWorldPos.y, -2);
        
        Debug.Log($"PacStudent初始位置设置为: {transform.position}");

        // 设置渲染顺序
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 2;
        }

        // 初始化移动状态
        isLerping = false;
        lastInput = Vector2.zero;
        currentInput = Vector2.zero;
        
        // 初始化撞墙粒子系统（确保开始时是停止状态）
        if (wallCollisionParticle != null)
        {
            wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Update()
    {
        // 处理玩家输入
        HandleInput();
        
        // 处理移动
        if (isLerping)
        {
            LerpToTarget();
        }
        else
        {
            // 不在移动时，尝试移动
            TryMove();
        }
    }

    void LateUpdate()
    {
        // 确保位置不被其他脚本覆盖
        // 如果位置被意外修改，重新设置
        if (!isLerping)
        {
            Vector2 expectedWorldPos = GridToWorldPosition(currentGridPosition);
            Vector3 currentPos = transform.position;
            
            // 如果位置差异太大，重新设置
            if (Vector2.Distance(new Vector2(currentPos.x, currentPos.y), expectedWorldPos) > 0.1f)
            {
                transform.position = new Vector3(expectedWorldPos.x, expectedWorldPos.y, -2);
                Debug.Log($"位置被重置: {transform.position}");
            }
        }
    }

    // 处理玩家输入
    private void HandleInput()
    {
        Vector2 input = Vector2.zero;
        
        if (Input.GetKeyDown(KeyCode.W))
            input = Vector2.down; // W键向上移动，但网格Y轴向下为正
        else if (Input.GetKeyDown(KeyCode.S))
            input = Vector2.up; // S键向下移动
        else if (Input.GetKeyDown(KeyCode.A))
            input = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D))
            input = Vector2.right;
        
        if (input != Vector2.zero)
        {
            lastInput = input;
        }
    }

    // 尝试移动
    private void TryMove()
    {
        // 检查lastInput方向
        if (lastInput != Vector2.zero && CanMoveTo(currentGridPosition + lastInput))
        {
            currentInput = lastInput;
            StartLerpToTarget(currentGridPosition + lastInput);
        }
        // 如果lastInput方向不可行，检查currentInput方向
        else if (currentInput != Vector2.zero && CanMoveTo(currentGridPosition + currentInput))
        {
            StartLerpToTarget(currentGridPosition + currentInput);
        }
        // 如果都不可行，处理墙壁碰撞
        else if (lastInput != Vector2.zero && !CanMoveTo(currentGridPosition + lastInput))
        {
            HandleWallCollision();
        }
    }

    // 开始移动到目标位置
    private void StartLerpToTarget(Vector2 targetGrid)
    {
        targetGridPosition = targetGrid;
        isLerping = true;
        lerpProgress = 0f;
        
        // 重置音效状态（成功移动时）
        hasPlayedWallSound = false;
        
        // 更新移动方向
        UpdateMoveDirection();
        
        // 播放移动动画、音频和粒子特效
        PlayMoveAnimation();
        PlayMoveAudio();
        PlayDustEffect();
    }

    // 插值移动到目标位置
    private void LerpToTarget()
    {
        lerpProgress += moveSpeed * Time.deltaTime;
        
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        Vector2 targetWorldPos = GridToWorldPosition(targetGridPosition);
        
        Vector2 lerpedPosition = Vector2.Lerp(currentWorldPos, targetWorldPos, lerpProgress);
        transform.position = new Vector3(lerpedPosition.x, lerpedPosition.y, -2);
        
        if (lerpProgress >= 1f)
        {
            // 到达目标位置
            currentGridPosition = targetGridPosition;
            isLerping = false;
            lerpProgress = 0f;
        }
    }

    // 检查是否可以移动到指定网格位置
    private bool CanMoveTo(Vector2 gridPos)
    {
        // 阶段2：实现真正的碰撞检测
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        // 使用射线检测检查目标位置是否有墙壁
        Collider2D wallCollider = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Wall"));
        
        if (wallCollider != null)
        {
            return false; // 有墙壁，不能移动
        }
        
        // 简单的边界检查，防止移动到太远的地方
        if (Mathf.Abs(gridPos.x) > 30 || Mathf.Abs(gridPos.y) > 30)
        {
            return false; // 超出合理范围
        }
        
        return true; // 没有墙壁，可以移动
    }

    // 更新移动方向
    private void UpdateMoveDirection()
    {
        Vector2 direction = (targetGridPosition - currentGridPosition).normalized;
        moveX = Mathf.RoundToInt(direction.x);
        moveY = -Mathf.RoundToInt(direction.y); // 保持原来的负号，这样动画方向正确
    }

    // 处理墙壁碰撞
    private void HandleWallCollision()
    {
        // 检查是否已经播放过音效（防止重复播放）
        if (!hasPlayedWallSound || lastWallCollisionPosition != currentGridPosition)
        {
            // 播放墙壁碰撞音效
            if (audioManager != null)
            {
                audioManager.PlayCollideWallSFX();
            }
            
            // 播放墙体碰撞粒子（一次性）
            PlayWallCollisionEffect();

            // 标记已播放音效
            hasPlayedWallSound = true;
            lastWallCollisionPosition = currentGridPosition;
            
            Debug.Log("PacStudent撞墙了！");
        }
    }

    // 播放墙体碰撞粒子（与移动灰尘不同）
    private void PlayWallCollisionEffect()
    {
        if (wallCollisionParticle == null)
        {
            return;
        }

        // 计算尝试移动方向
        Vector2 attemptedDir = Vector2.zero;
        if (lastInput != Vector2.zero)
        {
            attemptedDir = lastInput.normalized;
        }
        else if (currentInput != Vector2.zero)
        {
            attemptedDir = currentInput.normalized;
        }

        // 当前世界位置
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);

        // 用射线尽量获取真实的墙面命中点；否则退化为前方半格
        RaycastHit2D hit = Physics2D.Raycast(currentWorldPos, attemptedDir, gridSize, LayerMask.GetMask("Wall"));
        Vector3 spawnPos;
        if (hit.collider != null)
        {
            spawnPos = new Vector3(hit.point.x, hit.point.y, -2f);
        }
        else
        {
            spawnPos = new Vector3(currentWorldPos.x + attemptedDir.x * (gridSize * 0.5f),
                                   currentWorldPos.y + attemptedDir.y * (gridSize * 0.5f),
                                   -2f);
        }

        // 先停止并清除所有粒子
        wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 将粒子系统移动到碰撞点
        wallCollisionParticle.transform.position = spawnPos;
        
        // 播放一次粒子效果
        wallCollisionParticle.Play();
        
        // 启动协程来停止粒子效果
        StartCoroutine(StopWallCollisionParticleAfterDelay());
    }
    
    private System.Collections.IEnumerator StopWallCollisionParticleAfterDelay()
    {
        // 等待粒子效果播放完成（根据你的粒子系统设置调整时间）
        yield return new WaitForSeconds(0.5f);
        
        if (wallCollisionParticle != null)
        {
            wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /* 作业3的旧代码 - 保留作为参考
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
        new Vector2(1, 1),
    };

    private int currentPathIndex = 0;
    private int nextPathIndex = 1;
    private float pathProgress = 0f;
    private bool isMoving = false;
    private int lastPlayedPathIndex = -1;

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

        Vector2 currentWorldPosition = Vector2.Lerp(
            currentWorldPoint,
            nextWorldPoint,
            pathProgress
        );
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
    */

    // 网格坐标转换为世界坐标
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
        
        // 应用偏移
        float worldX = baseX + offsetX;
        float worldY = baseY + offsetY;
        
        return new Vector2(worldX, worldY);
    }

    // 播放移动音频
    private void PlayMoveAudio()
    {
        if (audioManager != null)
        {
            // 检查下一个位置是否有豆子
            if (WillEatPelletAtNextPosition())
            {
                audioManager.PlayEatPelletSFX(); // 播放吃豆子音效
            }
            else
            {
                audioManager.PlayMoveSFX(); // 播放普通移动音效
            }
        }
    }
    
    // 检查下一个位置是否有豆子
    private bool WillEatPelletAtNextPosition()
    {
        // 阶段3：暂时返回false，等70%档实现真正的豆子检测
        // 这里先实现基础框架，后续会完善
        return false;
    }
    
    // 播放灰尘粒子特效（轻量级版本）
    private void PlayDustEffect()
    {
        if (dustParticleSystem != null)
        {
            // 平衡版本：每次产生5个颗粒，既看得见又不会太卡
            dustParticleSystem.Emit(5);
        }
    }
    
    // 停止灰尘粒子特效
    private void StopDustEffect()
    {
        if (dustParticleSystem != null)
        {
            dustParticleSystem.Stop();
        }
    }


    private void PlayMoveAnimation()
    {
        if (animator != null)
        {
            animator.SetInteger("MoveX", moveX);
            animator.SetInteger("MoveY", moveY);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pellet"))
        {
            // TODO: 处理吃豆子逻辑
            Debug.Log("Ate a pellet!");
        }
        else if (other.CompareTag("PowerPellet"))
        {
            // TODO: 处理吃能量豆逻辑
            Debug.Log("Ate a power pellet!");
        }
        else if (other.CompareTag("Wall"))
        {
            // TODO: 处理撞墙逻辑
            Debug.Log("Hit a wall!");
        }
    }

    // 公共方法
    public void PauseMovement()
    {
        isLerping = false;
    }

    public void ResumeMovement()
    {
        // 恢复移动（如果需要的话）
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    // 获取当前网格位置
    public Vector2 GetCurrentGridPosition()
    {
        return currentGridPosition;
    }

    // 获取是否正在移动
    public bool IsMoving()
    {
        return isLerping;
    }

    // 世界坐标转换为网格坐标
    public Vector2 WorldToGridPosition(Vector3 worldPos)
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

    // 设置PacStudent的网格位置（用于重置位置等）
    public void SetGridPosition(Vector2 gridPos)
    {
        currentGridPosition = gridPos;
        targetGridPosition = gridPos;
        Vector2 worldPos = GridToWorldPosition(gridPos);
        transform.position = new Vector3(worldPos.x, worldPos.y, -2);
        isLerping = false;
    }
}
