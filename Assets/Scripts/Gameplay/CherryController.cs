using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Cherry Settings")]
    public float moveSpeed = 0.3f;
    
    [Header("Movement")]
    private Vector3 startPosition;
    private Vector3 centerPosition;
    private Vector3 endPosition;
    private bool isMoving = false;
    private float moveProgress = 0f;
    
    [Header("Spawn Settings")]
    private float spawnTimer = 5f;
    private bool hasSpawned = false;
    private bool hasEnteredCameraView = false;
    
    void Start()
    {
        gameObject.SetActive(true);
        
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10;
        }
        
        SetupCollider();
        
        GetComponent<SpriteRenderer>().enabled = false;
    }
    
    void Update()
    {
        if (!hasSpawned)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnCherry();
            }
        }
        
        if (isMoving)
        {
            MoveCherry();
        }
    }
    
    private void SpawnCherry()
    {
        SetRandomStartPosition();
        
        SetCenterAndEndPosition();
        
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.enabled = true;
        
        isMoving = true;
        moveProgress = 0f;
        hasSpawned = true;
        hasEnteredCameraView = false;
    }
    
    private void SetRandomStartPosition()
    {
        int side = Random.Range(0, 4);
        
        switch (side)
        {
            case 0:
                startPosition = new Vector3(-25f, Random.Range(-20f, 20f), -1f);
                break;
            case 1:
                startPosition = new Vector3(25f, Random.Range(-20f, 20f), -1f);
                break;
            case 2:
                startPosition = new Vector3(Random.Range(-25f, 25f), 20f, -1f);
                break;
            case 3:
                startPosition = new Vector3(Random.Range(-25f, 25f), -20f, -1f);
                break;
        }
        
        transform.position = startPosition;
    }
    
    private void SetCenterAndEndPosition()
    {
        centerPosition = new Vector3(0f, 0f, -1f);
        
        if (startPosition.x < 0)
        {
            float ratio = (0f - startPosition.x) / (25f - startPosition.x);
            endPosition = new Vector3(25f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.x > 0)
        {
            float ratio = (0f - startPosition.x) / (-25f - startPosition.x);
            endPosition = new Vector3(-25f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.y > 0)
        {
            float ratio = (0f - startPosition.y) / (-20f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, -20f, startPosition.z);
        }
        else
        {
            float ratio = (0f - startPosition.y) / (20f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, 20f, startPosition.z);
        }
    }
    
    private void MoveCherry()
    {
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        
        float actualSpeed = moveSpeed;
        
        moveProgress += (actualSpeed * Time.deltaTime) / totalDistance;
        
        transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);
        
        if (!hasEnteredCameraView && IsInsideCameraView())
        {
            hasEnteredCameraView = true;
        }

        if ((hasEnteredCameraView && IsOutOfCameraView()) || moveProgress >= 1f)
        {
            DestroyCherry();
        }
    }
    
    private bool IsOutOfCameraView()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;
        
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        float leftBound = mainCamera.transform.position.x - cameraWidth / 2f;
        float rightBound = mainCamera.transform.position.x + cameraWidth / 2f;
        float bottomBound = mainCamera.transform.position.y - cameraHeight / 2f;
        float topBound = mainCamera.transform.position.y + cameraHeight / 2f;
        
        Vector3 pos = transform.position;
        bool isOut = pos.x < leftBound || pos.x > rightBound || 
                     pos.y < bottomBound || pos.y > topBound;
        
        return isOut;
    }

    private bool IsInsideCameraView()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        float leftBound = mainCamera.transform.position.x - cameraWidth / 2f;
        float rightBound = mainCamera.transform.position.x + cameraWidth / 2f;
        float bottomBound = mainCamera.transform.position.y - cameraHeight / 2f;
        float topBound = mainCamera.transform.position.y + cameraHeight / 2f;

        Vector3 pos = transform.position;
        return pos.x >= leftBound && pos.x <= rightBound && pos.y >= bottomBound && pos.y <= topBound;
    }

    private void DestroyCherry()
    {
        hasSpawned = false;
        spawnTimer = 5f;
        isMoving = false;
        moveProgress = 0f;
        hasEnteredCameraView = false;
        
        GetComponent<SpriteRenderer>().enabled = false;
        transform.position = Vector3.zero;
    }
    
    public void ResetCherry()
    {
        hasSpawned = false;
        spawnTimer = 5f;
        isMoving = false;
        moveProgress = 0f;
        hasEnteredCameraView = false;
        
        GetComponent<SpriteRenderer>().enabled = false;
        transform.position = Vector3.zero;
    }
    
    private void SetupCollider()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        collider.isTrigger = true;
        
        collider.radius = 0.4f;
        
        gameObject.layer = LayerMask.NameToLayer("Cherry");
    }
}
