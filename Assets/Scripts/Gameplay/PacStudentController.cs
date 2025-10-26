using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;

    [Header("Audio")]
    public AudioManager audioManager;

    [Header("Particle Effects")]
    public ParticleSystem dustParticleSystem;
    [Tooltip("particles - hitting the wall")]
    public ParticleSystem wallCollisionParticle;
    [Tooltip("death particle")]
    public ParticleSystem deathParticleSystem;
    
    [Header("Teleporters")]
    [Tooltip("left teleporter position (world coordinates)")]
    public Transform leftTeleporter;
    [Tooltip("right teleporter position (world coordinates)")]
    public Transform rightTeleporter;
    
    private bool isDead = false;
    
    [Header("Game Manager")]
    [Tooltip("game manager reference (if null, will be automatically retrieved)")]
    public GameManager gameManager;

    [Header("Animation")]
    public Animator animator;

    [Header("Grid Settings")]
    public float gridSize = 1f;

    private Vector2 currentGridPosition;
    private Vector2 targetGridPosition;
    private Vector2 lastInput;
    private Vector2 currentInput;
    private bool isLerping = false;
    private float lerpProgress = 0f;
    
    private bool hasPlayedWallSound = false;
    private Vector2 lastWallCollisionPosition;
    
    private bool isTeleporting = false;
    private float teleportCooldown = 0.1f;
    
    private bool hasPickedUpCherry = false;

    private int moveX = 0;
    private int moveY = 0;

    void Start()
    {
        currentGridPosition = new Vector2(1, 1);
        targetGridPosition = currentGridPosition;
        
        Vector2 startWorldPos = GridToWorldPosition(currentGridPosition);
        transform.position = new Vector3(startWorldPos.x, startWorldPos.y, -2);

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 2;
        }

        isLerping = false;
        lastInput = Vector2.zero;
        currentInput = Vector2.zero;
        
        if (wallCollisionParticle != null)
        {
            wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        if (deathParticleSystem != null)
        {
            deathParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning())
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            //Debug.Log("Test key: Manually trigger PacStudent to die");
            HandlePacStudentDeath();
            return;
        }
        
        if (isDead)
        {
            return;
        }
        
        HandleInput();
        
        if (isLerping)
        {
            LerpToTarget();
        }
        else
        {
            TryMove();
        }
        
        CheckTeleportation();
        
        CheckPelletPickup();
        
        CheckCherryPickup();
        
        CheckPowerPillPickup();
        
        CheckGhostCollision();
    }

    void LateUpdate()
    {
        if (isDead)
        {
            return;
        }
        
        if (!isLerping)
        {
            Vector2 expectedWorldPos = GridToWorldPosition(currentGridPosition);
            Vector3 currentPos = transform.position;
            
            if (Vector2.Distance(new Vector2(currentPos.x, currentPos.y), expectedWorldPos) > 0.1f)
            {
                transform.position = new Vector3(expectedWorldPos.x, expectedWorldPos.y, -2);
                //Debug.Log($"Position reset: {transform.position}");
            }
        }
    }

    private void HandleInput()
    {
        Vector2 input = Vector2.zero;
        
        if (Input.GetKeyDown(KeyCode.W))
            input = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.S))
            input = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.A))
            input = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D))
            input = Vector2.right;
        
        if (input != Vector2.zero)
        {
            lastInput = input;
        }
    }

    private void TryMove()
    {
        if (lastInput != Vector2.zero && CanMoveTo(currentGridPosition + lastInput))
        {
            currentInput = lastInput;
            StartLerpToTarget(currentGridPosition + lastInput);
        }
        else if (currentInput != Vector2.zero && CanMoveTo(currentGridPosition + currentInput))
        {
            StartLerpToTarget(currentGridPosition + currentInput);
        }
        else if (lastInput != Vector2.zero && !CanMoveTo(currentGridPosition + lastInput))
        {
            HandleWallCollision();
        }
    }

    private void StartLerpToTarget(Vector2 targetGrid)
    {
        targetGridPosition = targetGrid;
        isLerping = true;
        lerpProgress = 0f;
        
        hasPlayedWallSound = false;
        
        UpdateMoveDirection();
        
        PlayMoveAnimation();
        PlayMoveAudio();
        PlayDustEffect();
    }

    private void LerpToTarget()
    {
        lerpProgress += moveSpeed * Time.deltaTime;
        
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        Vector2 targetWorldPos = GridToWorldPosition(targetGridPosition);
        
        Vector2 lerpedPosition = Vector2.Lerp(currentWorldPos, targetWorldPos, lerpProgress);
        transform.position = new Vector3(lerpedPosition.x, lerpedPosition.y, -2);
        
        if (lerpProgress >= 1f)
        {
            currentGridPosition = targetGridPosition;
            isLerping = false;
            lerpProgress = 0f;
        }
    }

    private bool CanMoveTo(Vector2 gridPos)
    {
        Vector2 worldPos = GridToWorldPosition(gridPos);
        
        int layerMask = (1 << 6) | (1 << 12) | (1 << 13);
        Collider2D wallCollider = Physics2D.OverlapPoint(worldPos, layerMask);
        
        if (wallCollider != null)
        {
            return false;
        }
        
        if (Mathf.Abs(gridPos.x) > 30 || Mathf.Abs(gridPos.y) > 30)
        {
            return false;
        }
        
        return true;
    }

    private void UpdateMoveDirection()
    {
        Vector2 direction = (targetGridPosition - currentGridPosition).normalized;
        moveX = Mathf.RoundToInt(direction.x);
        moveY = -Mathf.RoundToInt(direction.y);
    }

    private void HandleWallCollision()
    {
        if (!hasPlayedWallSound || lastWallCollisionPosition != currentGridPosition)
        {
            if (audioManager != null)
            {
                audioManager.PlayCollideWallSFX();
            }
            
            PlayWallCollisionEffect();

            hasPlayedWallSound = true;
            lastWallCollisionPosition = currentGridPosition;
            
            //Debug.Log("PacStudent hit the wall!");
        }
    }

    private void PlayWallCollisionEffect()
    {
        if (wallCollisionParticle == null)
        {
            return;
        }

        Vector2 attemptedDir = Vector2.zero;
        if (lastInput != Vector2.zero)
        {
            attemptedDir = lastInput.normalized;
        }
        else if (currentInput != Vector2.zero)
        {
            attemptedDir = currentInput.normalized;
        }

        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);

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

        wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        wallCollisionParticle.transform.position = spawnPos;
        
        wallCollisionParticle.Play();
        
        StartCoroutine(StopWallCollisionParticleAfterDelay());
    }
    
    private System.Collections.IEnumerator StopWallCollisionParticleAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (wallCollisionParticle != null)
        {
            wallCollisionParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    
    private void CheckTeleportation()
    {
        if (isTeleporting || leftTeleporter == null || rightTeleporter == null)
            return;
            
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        
        if (Vector2.Distance(currentWorldPos, leftTeleporter.position) < 0.5f && 
            (lastInput.x < 0 || currentInput.x < 0))
        {
            TeleportToRight();
        }
        else if (Vector2.Distance(currentWorldPos, rightTeleporter.position) < 0.5f && 
                 (lastInput.x > 0 || currentInput.x > 0))
        {
            TeleportToLeft();
        }
    }
    
    private void TeleportToRight()
    {
        if (isTeleporting) return;
        
        isTeleporting = true;
        
        Vector2 rightPos = rightTeleporter.position;
        Vector2 targetPos = new Vector2(rightPos.x + 1f, rightPos.y);
        
        Vector2 targetGridPos = WorldToGridPosition(targetPos);
        
        currentGridPosition = targetGridPos;
        targetGridPosition = targetGridPos;
        transform.position = new Vector3(targetPos.x, targetPos.y, -2);
        
        isLerping = false;
        lerpProgress = 0f;
        
        if (lastInput != Vector2.zero)
        {
            currentInput = lastInput;
        }
        
        StartCoroutine(TeleportCooldown());
        
        //Debug.Log($"Teleport from left to right: world coordinates ({targetPos.x}, {targetPos.y}) -> grid coordinates ({targetGridPos.x}, {targetGridPos.y})");
    }
    
    private void TeleportToLeft()
    {
        if (isTeleporting) return;
        
        isTeleporting = true;
        
        Vector2 leftPos = leftTeleporter.position;
        Vector2 targetPos = new Vector2(leftPos.x - 1f, leftPos.y);
        
        Vector2 targetGridPos = WorldToGridPosition(targetPos);
        
        currentGridPosition = targetGridPos;
        targetGridPosition = targetGridPos;
        transform.position = new Vector3(targetPos.x, targetPos.y, -2);
        
        isLerping = false;
        lerpProgress = 0f;
        
        if (lastInput != Vector2.zero)
        {
            currentInput = lastInput;
        }
        
        StartCoroutine(TeleportCooldown());
        
        //Debug.Log($"Teleport from right to left: world coordinates ({targetPos.x}, {targetPos.y}) -> grid coordinates ({targetGridPos.x}, {targetGridPos.y})");
    }
    
    private System.Collections.IEnumerator TeleportCooldown()
    {
        yield return new WaitForSeconds(teleportCooldown);
        isTeleporting = false;
    }
    
    private void CheckPelletPickup()
    {
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        
        Collider2D pellet = Physics2D.OverlapPoint(currentWorldPos, LayerMask.GetMask("Pellet"));
        
        if (pellet != null)
        {
            PickupPellet(pellet.gameObject);
        }
    }
    
    private void PickupPellet(GameObject pellet)
    {
        
        if (gameManager != null)
        {
            gameManager.AddScore(10);
        }
        
        Destroy(pellet);
        StartCoroutine(DelayedCheckAfterDestroy(pellet.name));
        
        if (gameManager != null)
        {
            gameManager.CheckAllPelletsEaten();
        }
        
        //Debug.Log("Picked up a pellet, +10 points");
    }
    
    private void CheckCherryPickup()
    {
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        
        Collider2D cherry = Physics2D.OverlapCircle(currentWorldPos, 0.5f, LayerMask.GetMask("Cherry"));
        
        if (cherry != null && !hasPickedUpCherry)
        {
            PickupCherry(cherry.gameObject);
        }
    }
    
    private void PickupCherry(GameObject cherry)
    {
        hasPickedUpCherry = true;
        
        if (audioManager != null)
        {
            audioManager.PlayEatPelletSFX();
        }
        
        if (gameManager != null)
        {
            gameManager.AddScore(100);
        }
        
        CherryController cherryController = cherry.GetComponent<CherryController>();
        if (cherryController != null)
        {
            cherryController.ResetCherry();
        }
        else
        {
            Destroy(cherry);
        }
        
        //Debug.Log("Picked up a cherry, +100 points");
        
        StartCoroutine(ResetCherryPickupFlag());
    }
    
    private System.Collections.IEnumerator ResetCherryPickupFlag()
    {
        yield return new WaitForSeconds(1f);
        hasPickedUpCherry = false;
    }
    
    private System.Collections.IEnumerator DelayedCheckAfterDestroy(string pelletName)
    {
        yield return new WaitForEndOfFrame();
        if (gameManager != null)
        {
            gameManager.CheckAllPelletsEaten();
        }
    }
    
    private void CheckPowerPillPickup()
    {
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        
        Collider2D powerPill = Physics2D.OverlapPoint(currentWorldPos, LayerMask.GetMask("PowerPill"));
        
        if (powerPill != null)
        {
            PickupPowerPill(powerPill.gameObject);
        }
    }
    
    private void PickupPowerPill(GameObject powerPill)
    {
        if (audioManager != null)
        {
            audioManager.PlayEatPelletSFX();
        }
        
        if (gameManager != null)
        {
            gameManager.AddScore(50);
            gameManager.SetGhostsFrightened(true);
        }
        
        Destroy(powerPill);
        
        if (gameManager != null)
        {
            gameManager.CheckAllPelletsEaten();
        }
        
        //Debug.Log("Picked up a power pill, +50 points, ghosts enter frightened state");
    }
    
    private void CheckGhostCollision()
    {
        Vector2 currentWorldPos = GridToWorldPosition(currentGridPosition);
        
        Collider2D ghost = Physics2D.OverlapPoint(currentWorldPos, LayerMask.GetMask("Ghost"));
        
        if (ghost != null)
        {
            HandleGhostCollision(ghost.gameObject);
        }
    }
    
    private void HandleGhostCollision(GameObject ghost)
    {
        GhostController ghostController = ghost.GetComponent<GhostController>();
        if (ghostController == null) return;
        
        GhostController.GhostState ghostState = ghostController.GetCurrentState();
        
        switch (ghostState)
        {
            case GhostController.GhostState.Normal:
                HandlePacStudentDeath();
                break;
                
            case GhostController.GhostState.Scared:
            case GhostController.GhostState.Recovering:
                HandleGhostEaten(ghostController);
                break;
                
            case GhostController.GhostState.Dead:
                break;
        }
    }
    
    private void HandlePacStudentDeath()
    {
        if (isDead)
        {
            return;
        }
        
        isDead = true;
        //Debug.Log("PacStudent died!");
        
        if (gameManager != null)
        {
            gameManager.LoseLife();
        }
        
        if (audioManager != null)
        {
            audioManager.PlayPacDeathSFX();
        }
        
        PlayDeathParticleEffect();
        
        if (animator != null)
        {
            animator.SetTrigger("IsDead");
        }
        
        StartCoroutine(DeathSequence());
    }
    
    private void HandleGhostEaten(GhostController ghostController)
    {
        //Debug.Log("Ghost eaten!");
        
        if (gameManager != null)
        {
            gameManager.AddScore(300);
        }
        
        if (audioManager != null)
        {
            audioManager.PlayEatPelletSFX();
        }
        
        ghostController.SetGhostState(GhostController.GhostState.Dead);
        
        if (gameManager != null)
        {
            gameManager.EnterGhostDie();
        }
    }
    
    private void PlayDeathParticleEffect()
    {
        if (deathParticleSystem != null)
        {
            deathParticleSystem.transform.position = transform.position;
            deathParticleSystem.Play();
            //Debug.Log($"Play death particle effect, position: {deathParticleSystem.transform.position}");
            
            StartCoroutine(StopDeathParticleAfterDelay(2f));
        }
        else
        {
            Debug.LogWarning("Death particle effect not configured!");
        }
    }
    
    private System.Collections.IEnumerator StopDeathParticleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (deathParticleSystem != null)
        {
            deathParticleSystem.Stop();
            //Debug.Log("Stop death particle effect");
        }
    }
    
    private System.Collections.IEnumerator DeathSequence()
    {
        isLerping = false;
        lerpProgress = 0f;
        lastInput = Vector2.zero;
        currentInput = Vector2.zero;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(-12.5f, 13.5f, -2f);
        float moveTime = 2f;
        float elapsedTime = 0f;
        
        //Debug.Log("Start death move sequence");
        
        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveTime;
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.position = newPosition;
            
            //Debug.Log($"Death move progress: {progress:F2}, current position: {newPosition}, target position: {targetPosition}");
            
            yield return null;
        }
        
        transform.position = targetPosition;
        
        if (gameManager != null && gameManager.GetCurrentLives() > 0)
        {
            RespawnPacStudent();
        }
    }
    
    private void RespawnPacStudent()
    {
        //Debug.Log("PacStudent respawned");
        
        isDead = false;
        
        isLerping = false;
        lerpProgress = 0f;
        lastInput = Vector2.zero;
        currentInput = Vector2.zero;
        
        if (animator != null)
        {
            animator.SetTrigger("Respawn");
        }
        
        StartCoroutine(SetInitialDirectionAfterRespawn());
        
        ResetAllGhosts();
    }
    
    private System.Collections.IEnumerator SetInitialDirectionAfterRespawn()
    {
        yield return new WaitForSeconds(0.1f);
        
        SetGridPosition(new Vector2(1, 1));
        
        if (animator != null)
        {
            animator.SetInteger("MoveX", 1);
            animator.SetInteger("MoveY", 0);
            //Debug.Log("Set respawned direction to right");
        }
    }
    
    private void ResetAllGhosts()
    {
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (GhostController ghost in ghosts)
        {
            ghost.SetGhostState(GhostController.GhostState.Normal);
            // ghost.SetPosition(ghostInitialPosition);
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

    private void PlayMoveAudio()
    {
        if (audioManager != null)
        {
            if (WillEatPelletAtNextPosition())
            {
                audioManager.PlayEatPelletSFX();
            }
            else
            {
                audioManager.PlayMoveSFX();
            }
        }
    }
    
    private bool WillEatPelletAtNextPosition()
    {
        Vector2 nextGridPos = currentGridPosition + currentInput;
        
        Vector2 nextWorldPos = GridToWorldPosition(nextGridPos);
        
        Collider2D pellet = Physics2D.OverlapPoint(nextWorldPos, LayerMask.GetMask("Pellet"));
        
        return pellet != null;
    }
    
    private void PlayDustEffect()
    {
        if (dustParticleSystem != null)
        {
            dustParticleSystem.Emit(5);
        }
    }
    
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


    public void PauseMovement()
    {
        isLerping = false;
    }

    public void ResumeMovement()
    {
        // resume movement (if needed)
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public Vector2 GetCurrentGridPosition()
    {
        return currentGridPosition;
    }

    public bool IsMoving()
    {
        return isLerping;
    }

    public Vector2 WorldToGridPosition(Vector3 worldPos)
    {
        float offsetX = worldPos.x - (-12.5f);
        float offsetY = 13.5f - worldPos.y;
        
        int gridX = Mathf.RoundToInt(1 + offsetX / 1.0f);
        int gridY = Mathf.RoundToInt(1 + offsetY / 1.0f);
        
        return new Vector2(gridX, gridY);
    }

    public void SetGridPosition(Vector2 gridPos)
    {
        currentGridPosition = gridPos;
        targetGridPosition = gridPos;
        Vector2 worldPos = GridToWorldPosition(gridPos);
        transform.position = new Vector3(worldPos.x, worldPos.y, -2);
        isLerping = false;
    }
}
