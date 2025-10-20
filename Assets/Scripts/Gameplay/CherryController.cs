using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Cherry Settings")]
    public float moveSpeed = 0.5f; // 降低移动速度，与PacStudent类似
    
    [Header("Movement")]
    private Vector3 startPosition;
    private Vector3 centerPosition;
    private Vector3 endPosition;
    private bool isMoving = false;
    private float moveProgress = 0f;
    
    [Header("Spawn Settings")]
    private float spawnTimer = 5f; // 5秒后生成
    private bool hasSpawned = false;
    
    void Start()
    {
        Debug.Log("CherryController Start() 被调用");
        Debug.Log($"BeanL对象状态: {gameObject.activeInHierarchy}");
        
        // 强制启用BeanL对象
        gameObject.SetActive(true);
        Debug.Log($"强制启用后BeanL状态: {gameObject.activeInHierarchy}");
        
        // 设置渲染顺序，确保Cherry在最上层
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10; // 比其他精灵更高的渲染顺序
        }
        
        // 初始时隐藏Cherry（但保持对象启用）
        GetComponent<SpriteRenderer>().enabled = false;
        Debug.Log("Cherry初始状态：隐藏");
    }
    
    void Update()
    {
        Debug.Log("CherryController Update() 被调用");
        
        // 生成计时器
        if (!hasSpawned)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                Debug.Log("5秒到了，开始生成Cherry");
                SpawnCherry();
            }
        }
        
        // 移动逻辑
        if (isMoving)
        {
            MoveCherry();
        }
    }
    
    // 生成Cherry
    private void SpawnCherry()
    {
        // 设置随机起始位置（在地图外）
        SetRandomStartPosition();
        
        // 设置中心点和目标位置
        SetCenterAndEndPosition();
        
        // 显示Cherry（启用SpriteRenderer）
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.enabled = true;
        Debug.Log($"SpriteRenderer启用状态: {renderer.enabled}");
        
        // 开始移动
        isMoving = true;
        moveProgress = 0f;
        hasSpawned = true;
        
        Debug.Log($"Cherry生成在位置: {startPosition}");
    }
    
    // 设置随机起始位置
    private void SetRandomStartPosition()
    {
        // 随机选择从哪一边进入（左、右、上、下）
        int side = Random.Range(0, 4);
        
        switch (side)
        {
            case 0: // 从左边进入
                startPosition = new Vector3(-15f, Random.Range(-15f, 15f), -1f);
                break;
            case 1: // 从右边进入
                startPosition = new Vector3(15f, Random.Range(-15f, 15f), -1f);
                break;
            case 2: // 从上面进入
                startPosition = new Vector3(Random.Range(-20f, 20f), 15f, -1f);
                break;
            case 3: // 从下面进入
                startPosition = new Vector3(Random.Range(-20f, 20f), -15f, -1f);
                break;
        }
        
        transform.position = startPosition;
    }
    
    // 设置中心点和目标位置
    private void SetCenterAndEndPosition()
    {
        // 关卡中心点（根据地图布局）
        centerPosition = new Vector3(0f, 0f, -1f);
        
        // 根据起始位置计算目标位置（穿过中心点）
        if (startPosition.x < 0) // 从左边进入，向右移动
        {
            // 计算穿过中心点的目标位置
            float ratio = (0f - startPosition.x) / (15f - startPosition.x);
            endPosition = new Vector3(15f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.x > 0) // 从右边进入，向左移动
        {
            // 计算穿过中心点的目标位置
            float ratio = (0f - startPosition.x) / (-15f - startPosition.x);
            endPosition = new Vector3(-15f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.y > 0) // 从上面进入，向下移动
        {
            // 计算穿过中心点的目标位置
            float ratio = (0f - startPosition.y) / (-15f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, -15f, startPosition.z);
        }
        else // 从下面进入，向上移动
        {
            // 计算穿过中心点的目标位置
            float ratio = (0f - startPosition.y) / (15f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, 15f, startPosition.z);
        }
    }
    
    // 移动Cherry
    private void MoveCherry()
    {
        // 计算总距离
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        
        // 计算移动速度（单位/秒）
        float actualSpeed = moveSpeed; // 0.5 单位/秒
        
        // 计算移动进度
        moveProgress += (actualSpeed * Time.deltaTime) / totalDistance;
        
        // 直线移动：从起始位置直接到目标位置
        transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);
        
        // 检查是否到达目标位置
        if (moveProgress >= 1f)
        {
            // 到达目标位置，销毁Cherry
            DestroyCherry();
        }
    }
    
    // 销毁Cherry
    private void DestroyCherry()
    {
        Debug.Log("Cherry到达目标位置，销毁");
        
        // 重置状态，准备重新生成
        hasSpawned = false;
        spawnTimer = 5f; // 5秒后重新生成
        isMoving = false;
        moveProgress = 0f;
        
        // 隐藏Cherry
        GetComponent<SpriteRenderer>().enabled = false;
        
        Debug.Log("Cherry重置，5秒后重新生成");
    }
}
