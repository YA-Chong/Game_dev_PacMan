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
    private bool hasEnteredCameraView = false; // 是否进入过屏幕
    
    void Start()
    {
        //Debug.Log("CherryController Start() 被调用");
        //Debug.Log($"BeanL对象状态: {gameObject.activeInHierarchy}");
        
        // 强制启用BeanL对象
        gameObject.SetActive(true);
        //Debug.Log($"强制启用后BeanL状态: {gameObject.activeInHierarchy}");
        
        // 设置渲染顺序，确保Cherry在最上层
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10; // 比其他精灵更高的渲染顺序
        }
        
        // 初始时隐藏Cherry（但保持对象启用）
        GetComponent<SpriteRenderer>().enabled = false;
        //Debug.Log("Cherry初始状态：隐藏");
    }
    
    void Update()
    {
        //Debug.Log("CherryController Update() 被调用");
        
        // 生成计时器
        if (!hasSpawned)
        {
            spawnTimer -= Time.deltaTime;
            //Debug.Log($"等待生成Cherry，剩余时间: {spawnTimer:F1}秒");
            if (spawnTimer <= 0f)
            {
                //Debug.Log("5秒到了，开始生成Cherry");
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
        //Debug.Log($"SpriteRenderer启用状态: {renderer.enabled}");
        
        // 开始移动
        isMoving = true;
        moveProgress = 0f;
        hasSpawned = true;
        hasEnteredCameraView = false;
        
        //Debug.Log($"Cherry生成在位置: {startPosition}");
    }
    
    // 设置随机起始位置
    private void SetRandomStartPosition()
    {
        // 随机选择从哪一边进入（左、右、上、下）
        int side = Random.Range(0, 4);
        
        switch (side)
        {
            case 0: // 从左边进入（确保在蒙版外）
                startPosition = new Vector3(-25f, Random.Range(-20f, 20f), -1f);
                break;
            case 1: // 从右边进入（确保在蒙版外）
                startPosition = new Vector3(25f, Random.Range(-20f, 20f), -1f);
                break;
            case 2: // 从上面进入（确保在蒙版外）
                startPosition = new Vector3(Random.Range(-25f, 25f), 20f, -1f);
                break;
            case 3: // 从下面进入（确保在蒙版外）
                startPosition = new Vector3(Random.Range(-25f, 25f), -20f, -1f);
                break;
        }
        
        transform.position = startPosition;
        //Debug.Log($"Cherry在蒙版外生成: {startPosition}");
    }
    
    // 设置中心点和目标位置
    private void SetCenterAndEndPosition()
    {
        // 关卡中心点（根据地图布局）
        centerPosition = new Vector3(0f, 0f, -1f);
        
        // 根据起始位置计算目标位置（穿过中心点，确保移出屏幕）
        if (startPosition.x < 0) // 从左边进入，向右移动
        {
            // 计算穿过中心点的目标位置，确保移出右边界
            float ratio = (0f - startPosition.x) / (25f - startPosition.x);
            endPosition = new Vector3(25f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.x > 0) // 从右边进入，向左移动
        {
            // 计算穿过中心点的目标位置，确保移出左边界
            float ratio = (0f - startPosition.x) / (-25f - startPosition.x);
            endPosition = new Vector3(-25f, startPosition.y + (0f - startPosition.y) / ratio, startPosition.z);
        }
        else if (startPosition.y > 0) // 从上面进入，向下移动
        {
            // 计算穿过中心点的目标位置，确保移出下边界
            float ratio = (0f - startPosition.y) / (-20f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, -20f, startPosition.z);
        }
        else // 从下面进入，向上移动
        {
            // 计算穿过中心点的目标位置，确保移出上边界
            float ratio = (0f - startPosition.y) / (20f - startPosition.y);
            endPosition = new Vector3(startPosition.x + (0f - startPosition.x) / ratio, 20f, startPosition.z);
        }
        
        //Debug.Log($"Cherry目标位置: {endPosition}");
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
        
        // 标记是否进入过屏幕
        if (!hasEnteredCameraView && IsInsideCameraView())
        {
            hasEnteredCameraView = true;
            ////Debug.Log("Cherry首次进入屏幕");
        }

        // 销毁条件：
        // 1) 已经进入过屏幕后再次离开屏幕
        // 2) 或者到达目标位置（双保险）
        if ((hasEnteredCameraView && IsOutOfCameraView()) || moveProgress >= 1f)
        {
            // 移出摄像机视野或到达目标位置，销毁Cherry
            DestroyCherry();
        }
    }
    
    // 检查Cherry是否移出摄像机视野
    private bool IsOutOfCameraView()
    {
        // 获取主摄像机
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;
        
        // 获取摄像机视野边界
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        // 计算摄像机边界
        float leftBound = mainCamera.transform.position.x - cameraWidth / 2f;
        float rightBound = mainCamera.transform.position.x + cameraWidth / 2f;
        float bottomBound = mainCamera.transform.position.y - cameraHeight / 2f;
        float topBound = mainCamera.transform.position.y + cameraHeight / 2f;
        
        // 检查Cherry是否完全移出摄像机视野
        Vector3 pos = transform.position;
        bool isOut = pos.x < leftBound || pos.x > rightBound || 
                     pos.y < bottomBound || pos.y > topBound;
        
        // 详细调试信息
        if (isOut)
        {
            //Debug.Log($"Cherry移出视野: 位置({pos.x:F1}, {pos.y:F1})");
            //Debug.Log($"摄像机位置: ({mainCamera.transform.position.x:F1}, {mainCamera.transform.position.y:F1})");
            //Debug.Log($"摄像机尺寸: {cameraWidth:F1} x {cameraHeight:F1}");
            //Debug.Log($"边界: L:{leftBound:F1} R:{rightBound:F1} B:{bottomBound:F1} T:{topBound:F1}");
        }
        
        return isOut;
    }

    // 检查Cherry是否在摄像机视野内
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
    
    // 销毁Cherry
    private void DestroyCherry()
    {
        //Debug.Log("Cherry移出摄像机视野，销毁");
        
        // 重置状态，准备重新生成
        hasSpawned = false;
        spawnTimer = 5f; // 5秒后重新生成
        isMoving = false;
        moveProgress = 0f;
        hasEnteredCameraView = false;
        
        // 隐藏Cherry并重置位置
        GetComponent<SpriteRenderer>().enabled = false;
        transform.position = Vector3.zero; // 重置到原点
        
        //Debug.Log("Cherry重置，5秒后重新生成");
    }
}
