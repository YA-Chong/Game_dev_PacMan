using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite tiles_0; // 空地板
    public Sprite tiles_1; // 外角
    public Sprite tiles_2; // 外壁
    public Sprite tiles_3; // 内角
    public Sprite tiles_4; // 内壁
    public Sprite tiles_5; // T型连接
    public Sprite tiles_6; // 幽灵出口墙
    
    [Header("Prefabs")]
    public GameObject beanS; // 标准豆子
    public GameObject beanM; // 能量豆
    
    [Header("Settings")]
    public float tileSize = 1f; // 每个格子的尺寸
    public Transform levelParent; // 关卡父对象
    
    [Header("Static Level Management")]
    public GameObject[] staticLevelObjects; // 静态关卡对象数组
    
    // levelMap 2D数组 - 左上象限
    private int[,] levelMap = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };
    
    void Start()
    {
        // 隐藏静态地图
        HideStaticLevel();
        // 生成程序地图
        GenerateLevel();
    }
    
    public void GenerateLevel()
    {
        // 删除现有的Level 01
        DeleteExistingLevel();
        
        // 创建关卡父对象
        if (levelParent == null)
        {
            GameObject levelParentObj = new GameObject("Generated Level");
            levelParent = levelParentObj.transform;
        }
        
        // 获取数组尺寸
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);
        
        // 重新计算位置：相机(0,0)应该和topleft的右下角对齐
        // 手动调整0.5偏移，向右下移动0.5
        float startX = -cols + 0.5f;
        float startY = -rows + 0.5f;
        
        // 生成左上象限（原始）
        // Debug.Log($"Generating TopLeft at ({startX}, {startY})");
        // Debug.Log($"TopLeft右下角应该在: ({startX + cols}, {startY - rows}) = ({startX + cols}, {startY - rows})");
        // Debug.Log($"Original map[0,0] = {levelMap[0,0]}, should be 1 (outside corner)");
        GenerateQuadrant(levelMap, startX, startY, "TopLeft");
        
        // 生成右上象限（水平镜像）
        int[,] topRightMap = MirrorHorizontally(levelMap);
        // Debug.Log($"TopRight map[0,0] = {topRightMap[0,0]}, should be 7 (T-junction)");
        // Debug.Log($"Generating TopRight at ({startX + cols}, {startY})");
        GenerateQuadrant(topRightMap, startX + cols, startY, "TopRight");
        
        // 生成左下象限（垂直镜像）
        int[,] bottomLeftMap = MirrorVertically(levelMap);
        // Debug.Log($"BottomLeft map[0,0] = {bottomLeftMap[0,0]}, should be 2 (outside wall)");
        // Debug.Log($"Generating BottomLeft at ({startX}, {startY + rows})");
        GenerateQuadrant(bottomLeftMap, startX, startY + rows, "BottomLeft");
        
        // 生成右下象限（水平+垂直镜像）
        int[,] bottomRightMap = MirrorHorizontally(bottomLeftMap);
        // Debug.Log($"BottomRight map[0,0] = {bottomRightMap[0,0]}, should be 0 (empty)");
        // Debug.Log($"Generating BottomRight at ({startX + cols}, {startY + rows})");
        GenerateQuadrant(bottomRightMap, startX + cols, startY + rows, "BottomRight");
        
        // 调整相机
        AdjustCamera(rows, cols);
        
        // Debug.Log("Level generation completed!");
    }
    
    private void GenerateQuadrant(int[,] map, float offsetX, float offsetY, string quadrantName)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        
        // 创建象限父对象
        GameObject quadrantParent = new GameObject(quadrantName);
        quadrantParent.transform.SetParent(levelParent);
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int tileType = map[row, col];
                
                // 计算世界坐标
                float worldX = (col + offsetX) * tileSize;
                float worldY = -(row + offsetY) * tileSize; // Y轴向下
                
                // 每个位置都铺空地板
                CreateTile(tiles_0, worldX, worldY, 0f, quadrantParent.transform);
                
                // 根据类型创建对应的sprite或prefab
                switch (tileType)
                {
                        case 1: // 外角
                            CreateTile(tiles_1, worldX, worldY, GetRotationAngle(map, row, col, 1, quadrantName), quadrantParent.transform);
                            break;
                        case 2: // 外壁
                            CreateTile(tiles_2, worldX, worldY, GetRotationAngle(map, row, col, 2, quadrantName), quadrantParent.transform);
                            break;
                        case 3: // 内角
                            CreateTile(tiles_3, worldX, worldY, GetRotationAngle(map, row, col, 3, quadrantName), quadrantParent.transform);
                            break;
                        case 4: // 内壁
                            CreateTile(tiles_4, worldX, worldY, GetRotationAngle(map, row, col, 4, quadrantName), quadrantParent.transform);
                            break;
                        case 5: // 标准豆子
                            CreatePrefab(beanS, worldX, worldY, quadrantParent.transform);
                            break;
                        case 6: // 能量豆
                            CreatePrefab(beanM, worldX, worldY, quadrantParent.transform);
                            break;
                        case 7: // T型连接
                            CreateTile(tiles_5, worldX, worldY, GetRotationAngle(map, row, col, 7, quadrantName), quadrantParent.transform);
                            break;
                        case 8: // 幽灵出口墙
                            CreateTile(tiles_6, worldX, worldY, GetRotationAngle(map, row, col, 8, quadrantName), quadrantParent.transform);
                            break;
                }
            }
        }
    }
    
    private void CreateTile(Sprite sprite, float x, float y, float rotation, Transform parent)
    {
        GameObject tile = new GameObject("Tile");
        tile.transform.SetParent(parent);
        tile.transform.position = new Vector3(x, y, 0);
        tile.transform.rotation = Quaternion.Euler(0, 0, rotation);
        
        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 0; // 地板和墙块在底层
    }
    
    private void CreatePrefab(GameObject prefab, float x, float y, Transform parent)
    {
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.transform.position = new Vector3(x, y, -1); // Z轴设为-1，确保在墙块前面
            
            // 确保豆子的SpriteRenderer在正确的层级
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 1; // 豆子在墙块前面
            }
        }
    }
    
    private float GetRotationAngle(int[,] map, int row, int col, int tileType, string quadrantName)
    {
        // 获取周围格子的信息
        bool hasTop = IsWall(map, row - 1, col);
        bool hasBottom = IsWall(map, row + 1, col);
        bool hasLeft = IsWall(map, row, col - 1);
        bool hasRight = IsWall(map, row, col + 1);
        
        float rotation = 0f;
        
        // 根据tile类型和周围情况确定旋转角度
        switch (tileType)
        {
            case 1: // 外角
                rotation = GetCornerRotation(hasTop, hasBottom, hasLeft, hasRight, true);
                break;
            case 2: // 外壁
                rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                break;
            case 3: // 内角
                rotation = GetCornerRotation(hasTop, hasBottom, hasLeft, hasRight, false);
                break;
            case 4: // 内壁
                rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                break;
            case 7: // T型连接
                rotation = GetTJunctionRotation(hasTop, hasBottom, hasLeft, hasRight);
                break;
            case 8: // 幽灵出口墙
                rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                break;
            default:
                rotation = 0f;
                break;
        }
        
        // 特殊处理：根据具体情况调整旋转角度（只处理TopLeft象限）
        if (quadrantName == "TopLeft")
        {
            // T型区域拐角的特殊处理（4个连接的情况）
            if (tileType == 3 && hasTop && hasBottom && hasLeft && hasRight)
            {
                if (row == 9 && col == 8)
                    rotation = 90f;
                else if (row == 10 && col == 8)
                    rotation = 0f;
            }
            
            // Tiles_6的特殊处理
            if (tileType == 8)
            {
                // 根据周围情况判断
                if (hasLeft && !hasTop && !hasBottom && !hasRight)
                    rotation = 0f; // 横着的
                else if (hasRight && !hasTop && !hasBottom && !hasLeft)
                    rotation = 0f; // 横着的
                else if (hasTop && !hasLeft && !hasBottom && !hasRight)
                    rotation = 90f; // 竖着的
                else if (hasBottom && !hasTop && !hasLeft && !hasRight)
                    rotation = 90f; // 竖着的
            }
        }
        
        // 暂时禁用象限调整，让基础逻辑处理所有象限
        // rotation = AdjustRotationForQuadrant(rotation, tileType, quadrantName);
        
        // 调试输出已关闭
        // if (tileType == 8 && quadrantName == "TopLeft")
        // {
        //     Debug.Log($"Tile {tileType} at ({row},{col}) in {quadrantName}: T{hasTop} B{hasBottom} L{hasLeft} R{hasRight} -> Rotation: {rotation}");
        // }
        
        return rotation;
    }
    
    private bool IsWall(int[,] map, int row, int col)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        
        // 边界检查
        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return false; // 边界外不算墙，让墙块自己判断
        
        int tileType = map[row, col];
        // 1,2,3,4,7,8 都是墙类型
        return tileType == 1 || tileType == 2 || tileType == 3 || tileType == 4 || tileType == 7 || tileType == 8;
    }
    
    private float GetWallRotation(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        // 计算连接的墙数量
        int wallCount = 0;
        if (hasTop) wallCount++;
        if (hasBottom) wallCount++;
        if (hasLeft) wallCount++;
        if (hasRight) wallCount++;
        
        // 如果只有2个连接，判断是水平还是垂直
        if (wallCount == 2)
        {
            // 垂直连接（上下）
            if (hasTop && hasBottom && !hasLeft && !hasRight)
                return 0f; // 垂直墙
            // 水平连接（左右）
            if (hasLeft && hasRight && !hasTop && !hasBottom)
                return 90f; // 水平墙
            
            // 其他2个连接的情况：根据连接方向判断
            if ((hasTop || hasBottom) && !(hasLeft || hasRight))
                return 0f; // 垂直墙
            if ((hasLeft || hasRight) && !(hasTop || hasBottom))
                return 90f; // 水平墙
            
            // 特殊情况：TFalse BTrue LTrue RFalse 应该是水平墙
            if (hasBottom && hasLeft && !hasTop && !hasRight)
                return 90f; // 水平墙
            if (hasTop && hasRight && !hasBottom && !hasLeft)
                return 90f; // 水平墙
            if (hasTop && hasLeft && !hasBottom && !hasRight)
                return 0f; // 垂直墙
            if (hasBottom && hasRight && !hasTop && !hasLeft)
                return 0f; // 垂直墙
        }
        
        // 如果只有1个连接，根据连接方向判断
        if (wallCount == 1)
        {
            if (hasTop || hasBottom)
                return 0f; // 垂直墙
            if (hasLeft || hasRight)
                return 90f; // 水平墙
        }
        
        // 如果连接数更多，根据主要方向判断
        if (wallCount >= 3)
        {
            // 如果上下都有墙，优先垂直
            if (hasTop && hasBottom)
                return 0f; // 垂直墙
            // 如果左右都有墙，优先水平
            if (hasLeft && hasRight)
                return 90f; // 水平墙
        }
        
        // 默认垂直墙
        return 0f;
    }
    
    private float GetCornerRotation(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight, bool isOutside)
    {
        // 重新设计的拐角旋转逻辑
        // 根据周围墙的情况确定角的方向
        
        // 计算连接的墙数量
        int wallCount = 0;
        if (hasTop) wallCount++;
        if (hasBottom) wallCount++;
        if (hasLeft) wallCount++;
        if (hasRight) wallCount++;
        
        // 如果是1个连接（端点），根据连接方向判断
        if (wallCount == 1)
        {
            if (hasTop) return 90f;    // 开口向上（向左下）
            if (hasBottom) return 270f; // 开口向下（向右上）
            if (hasLeft) return 0f;   // 开口向左（向右下）
            if (hasRight) return 180f; // 开口向右（向左上）
        }
        
        // 如果是2个连接（普通拐角）
        if (wallCount == 2)
        {
            if (hasTop && hasLeft)
                return 180f; // 开口向左上
            if (hasTop && hasRight)
                return 90f;  // 开口向左下
            if (hasBottom && hasRight)
                return 0f;   // 开口向右下
            if (hasBottom && hasLeft)
                return 270f; // 开口向右上
        }
        
        // 如果是3个连接（T型区域的拐角）
        if (wallCount == 3)
        {
            // T型区域的拐角：开口朝向没有墙的方向
            if (!hasTop) return 90f;    // 开口向上（向左下）
            if (!hasBottom) return 270f; // 开口向下（向右上）
            if (!hasLeft) return 0f;   // 开口向左（向右下）
            if (!hasRight) return 270f; // 开口向右（向右上）- 修正为270度
        }
        
        // 如果是4个连接（十字路口），根据具体情况判断
        if (wallCount == 4)
        {
            // 对于4个连接的拐角，我们需要根据具体位置判断
            if (hasTop && hasBottom && hasLeft && hasRight)
            {
                // 这种情况下，需要根据具体位置判断
                // 暂时返回默认值，让特殊处理逻辑来处理
                return 0f;
            }
        }
        
        return 0f; // 默认
    }
    
    private float GetTJunctionRotation(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        // T型连接的方向
        if (hasTop && hasLeft && hasRight)
            return 0f; // T向上
        if (hasRight && hasTop && hasBottom)
            return 90f; // T向右
        if (hasBottom && hasLeft && hasRight)
            return 180f; // T向下
        if (hasLeft && hasTop && hasBottom)
            return 270f; // T向左
        
        return 0f; // 默认
    }
    
    private float AdjustRotationForQuadrant(float baseRotation, int tileType, string quadrantName)
    {
        // 根据象限调整旋转角度
        switch (quadrantName)
        {
            case "TopLeft":
                return baseRotation; // 原始象限，不需要调整
                
            case "TopRight":
                // 水平镜像：需要水平翻转旋转
                if (tileType == 1 || tileType == 3) // 拐角
                {
                    // 拐角需要特殊处理
                    if (baseRotation == 0f) return 90f;
                    if (baseRotation == 90f) return 0f;
                    if (baseRotation == 180f) return 270f;
                    if (baseRotation == 270f) return 180f;
                }
                else if (tileType == 2 || tileType == 4 || tileType == 8) // 墙
                {
                    // 墙的水平镜像：垂直变水平，水平变垂直
                    if (baseRotation == 0f) return 90f;
                    if (baseRotation == 90f) return 0f;
                }
                else if (tileType == 7) // T型
                {
                    // T型的水平镜像
                    if (baseRotation == 0f) return 0f; // T向上保持不变
                    if (baseRotation == 90f) return 270f; // T向右变向左
                    if (baseRotation == 180f) return 180f; // T向下保持不变
                    if (baseRotation == 270f) return 90f; // T向左变向右
                }
                break;
                
            case "BottomLeft":
                // 垂直镜像：需要垂直翻转旋转
                if (tileType == 1 || tileType == 3) // 拐角
                {
                    // 拐角的垂直镜像
                    if (baseRotation == 0f) return 270f;
                    if (baseRotation == 90f) return 180f;
                    if (baseRotation == 180f) return 90f;
                    if (baseRotation == 270f) return 0f;
                }
                else if (tileType == 2 || tileType == 4 || tileType == 8) // 墙
                {
                    // 墙的垂直镜像：垂直变垂直，水平变水平（不变）
                    return baseRotation;
                }
                else if (tileType == 7) // T型
                {
                    // T型的垂直镜像
                    if (baseRotation == 0f) return 180f; // T向上变向下
                    if (baseRotation == 90f) return 90f; // T向右保持不变
                    if (baseRotation == 180f) return 0f; // T向下变向上
                    if (baseRotation == 270f) return 270f; // T向左保持不变
                }
                break;
                
            case "BottomRight":
                // 水平+垂直镜像：需要180度旋转
                if (tileType == 1 || tileType == 3) // 拐角
                {
                    return (baseRotation + 180f) % 360f;
                }
                else if (tileType == 2 || tileType == 4 || tileType == 8) // 墙
                {
                    // 墙的180度旋转：垂直变垂直，水平变水平（不变）
                    return baseRotation;
                }
                else if (tileType == 7) // T型
                {
                    // T型的180度旋转
                    return (baseRotation + 180f) % 360f;
                }
                break;
        }
        
        return baseRotation;
    }
    
    private int[,] MirrorHorizontally(int[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        int[,] mirrored = new int[rows, cols];
        
        // 水平镜像：左右翻转
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                mirrored[row, col] = original[row, cols - 1 - col];
            }
        }
        
        // Debug.Log("Horizontal mirror applied");
        return mirrored;
    }
    
    private int[,] MirrorVertically(int[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        int[,] mirrored = new int[rows, cols];
        
        // 垂直镜像：上下翻转，但删除底部行
        // 新的第0行 = 原来的第13行
        // 新的第1行 = 原来的第12行
        // ...
        // 新的第13行 = 原来的第0行
        // 新的第14行 = 原来的第14行（保持原样）
        
        for (int row = 0; row < rows - 1; row++) // 不包含最后一行
        {
            for (int col = 0; col < cols; col++)
            {
                mirrored[row, col] = original[rows - 2 - row, col]; // 从倒数第二行开始镜像
            }
        }
        
        // 最后一行设为空（0）
        for (int col = 0; col < cols; col++)
        {
            mirrored[rows - 1, col] = 0; // 强制设为空
        }
        
        // Debug.Log($"Vertical mirror: new[0,0] = {mirrored[0,0]} (from original[{rows-2},0] = {original[rows-2,0]})");
        return mirrored;
    }
    
    private void DeleteExistingLevel()
    {
        // 查找并删除现有的Level 01
        GameObject existingLevel = GameObject.Find("Level 01");
        if (existingLevel != null)
        {
            DestroyImmediate(existingLevel);
        }
        
        // 删除之前生成的关卡
        GameObject generatedLevel = GameObject.Find("Generated Level");
        if (generatedLevel != null)
        {
            DestroyImmediate(generatedLevel);
        }
    }
    
    private void AdjustCamera(int rows, int cols)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 保持相机在原来的位置 (0, 0, -10)
            mainCamera.transform.position = new Vector3(0, 0, -10);
            
            // 计算完整关卡的尺寸
            float totalWidth = cols * 2 * tileSize;
            float totalHeight = rows * 2 * tileSize;
            
            // 调整orthographic size以适应关卡，但保持相机居中
            float aspectRatio = (float)Screen.width / Screen.height;
            float requiredSize = Mathf.Max(totalWidth / (2 * aspectRatio), totalHeight / 2);
            mainCamera.orthographicSize = requiredSize + 2f; // 加一点边距
            
            // Debug.Log($"Camera kept at (0,0,-10), orthographic size adjusted to {mainCamera.orthographicSize}");
        }
    }
    
    // 隐藏静态地图
    private void HideStaticLevel()
    {
        if (staticLevelObjects != null)
        {
            foreach (GameObject obj in staticLevelObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }
    
    // 显示静态地图（用于编辑器或调试）
    public void ShowStaticLevel()
    {
        if (staticLevelObjects != null)
        {
            foreach (GameObject obj in staticLevelObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}
