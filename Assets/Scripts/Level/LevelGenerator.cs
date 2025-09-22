using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite tiles_0;
    public Sprite tiles_1;
    public Sprite tiles_2;
    public Sprite tiles_3;
    public Sprite tiles_4;
    public Sprite tiles_5;
    public Sprite tiles_6;

    [Header("Prefabs")]
    public GameObject beanS;
    public GameObject beanM;

    [Header("Settings")]
    public float tileSize = 1f;
    public Transform levelParent;

    [Header("Static Level Management")]
    public GameObject[] staticLevelObjects;
    private int[,] levelMap = new int[,]
    {
        { 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7 },
        { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 4, 4, 4, 3, 5, 4 },
        { 2, 6, 4, 0, 0, 4, 5, 4, 0, 0, 0, 4, 5, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 4, 4, 4, 3, 5, 3 },
        { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 3, 5, 3, 4, 4, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 4, 4, 5, 3, 4, 4, 3 },
        { 2, 5, 5, 5, 5, 5, 5, 4, 4, 5, 5, 5, 5, 4 },
        { 1, 2, 2, 2, 2, 1, 5, 4, 3, 4, 4, 3, 0, 4 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 3, 4, 4, 3, 0, 3 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 3, 4, 4, 8 },
        { 2, 2, 2, 2, 2, 1, 5, 3, 3, 0, 4, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 4, 0, 0, 0 },
    };

    void Start()
    {
        HideStaticLevel();
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        DeleteExistingLevel();

        if (levelParent == null)
        {
            GameObject levelParentObj = new GameObject("Generated Level");
            levelParent = levelParentObj.transform;
        }

        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        float startX = -cols + 0.5f;
        float startY = -rows + 0.5f;

        GenerateQuadrant(levelMap, startX, startY, "TopLeft");

        int[,] topRightMap = MirrorHorizontally(levelMap);
        GenerateQuadrant(topRightMap, startX + cols, startY, "TopRight");

        int[,] bottomLeftMap = MirrorVertically(levelMap);
        GenerateQuadrant(bottomLeftMap, startX, startY + rows, "BottomLeft");

        int[,] bottomRightMap = MirrorHorizontally(bottomLeftMap);
        GenerateQuadrant(bottomRightMap, startX + cols, startY + rows, "BottomRight");

        AdjustCamera(rows, cols);
    }

    private void GenerateQuadrant(int[,] map, float offsetX, float offsetY, string quadrantName)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        GameObject quadrantParent = new GameObject(quadrantName);
        quadrantParent.transform.SetParent(levelParent);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int tileType = map[row, col];

                float worldX = (col + offsetX) * tileSize;
                float worldY = -(row + offsetY) * tileSize;

                CreateTile(tiles_0, worldX, worldY, 0f, quadrantParent.transform);
                switch (tileType)
                {
                    case 1:
                        CreateTile(
                            tiles_1,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 1, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
                        break;
                    case 2:
                        CreateTile(
                            tiles_2,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 2, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
                        break;
                    case 3:
                        CreateTile(
                            tiles_3,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 3, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
                        break;
                    case 4:
                        CreateTile(
                            tiles_4,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 4, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
                        break;
                    case 5:
                        CreatePrefab(beanS, worldX, worldY, quadrantParent.transform);
                        break;
                    case 6:
                        CreatePrefab(beanM, worldX, worldY, quadrantParent.transform);
                        break;
                    case 7:
                        CreateTile(
                            tiles_5,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 7, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
                        break;
                    case 8:
                        CreateTile(
                            tiles_6,
                            worldX,
                            worldY,
                            GetRotationAngle(map, row, col, 8, quadrantName),
                            quadrantParent.transform,
                            quadrantName
                        );
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
        renderer.sortingOrder = 0;
    }

    private void CreateTile(
        Sprite sprite,
        float x,
        float y,
        float rotation,
        Transform parent,
        string quadrantName
    )
    {
        GameObject tile = new GameObject("Tile");
        tile.transform.SetParent(parent);
        tile.transform.position = new Vector3(x, y, 0);

        Quaternion finalRotation = GetQuadrantRotation(rotation, quadrantName);
        tile.transform.rotation = finalRotation;

        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 0;
    }

    private Quaternion GetQuadrantRotation(float rotationZ, string quadrantName)
    {
        switch (quadrantName)
        {
            case "TopLeft":
                return Quaternion.Euler(0, 0, rotationZ);

            case "TopRight":
                return Quaternion.Euler(0, 180, rotationZ);

            case "BottomLeft":
                return Quaternion.Euler(180, 0, rotationZ);

            case "BottomRight":
                return Quaternion.Euler(180, 180, rotationZ);

            default:
                return Quaternion.Euler(0, 0, rotationZ);
        }
    }

    private void CreatePrefab(GameObject prefab, float x, float y, Transform parent)
    {
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.transform.position = new Vector3(x, y, -1);

            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 1;
            }
        }
    }

    private float GetRotationAngle(int[,] map, int row, int col, int tileType, string quadrantName)
    {
        if (quadrantName == "TopLeft")
        {
            bool hasTop = IsWall(map, row - 1, col);
            bool hasBottom = IsWall(map, row + 1, col);
            bool hasLeft = IsWall(map, row, col - 1);
            bool hasRight = IsWall(map, row, col + 1);

            float rotation = 0f;
            switch (tileType)
            {
                case 1:
                    rotation = GetCornerRotation(hasTop, hasBottom, hasLeft, hasRight, true);
                    break;
                case 2:
                    rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                    break;
                case 3:
                    rotation = GetCornerRotation(hasTop, hasBottom, hasLeft, hasRight, false);
                    break;
                case 4:
                    rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                    break;
                case 7:
                    rotation = GetTJunctionRotation(hasTop, hasBottom, hasLeft, hasRight);
                    break;
                case 8:
                    rotation = GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
                    break;
                default:
                    rotation = 0f;
                    break;
            }

            rotation = ApplyTopLeftSpecialLogic(
                row,
                col,
                tileType,
                rotation,
                hasTop,
                hasBottom,
                hasLeft,
                hasRight
            );

            return rotation;
        }
        else
        {
            (int mappedRow, int mappedCol) = MapToTopLeft(row, col, quadrantName);
            return GetRotationAngle(levelMap, mappedRow, mappedCol, tileType, "TopLeft");
        }
    }

    private (int mappedRow, int mappedCol) MapToTopLeft(int row, int col, string quadrantName)
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        switch (quadrantName)
        {
            case "TopLeft":
                return (row, col);

            case "TopRight":
                return (row, cols - 1 - col);

            case "BottomLeft":
                return (rows - 2 - row, col);

            case "BottomRight":
                return (rows - 2 - row, cols - 1 - col);

            default:
                return (row, col);
        }
    }

    private bool IsWall(int[,] map, int row, int col)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return false;

        int tileType = map[row, col];
        return tileType == 1
            || tileType == 2
            || tileType == 3
            || tileType == 4
            || tileType == 7
            || tileType == 8;
    }

    private float ApplyTopLeftSpecialLogic(
        int row,
        int col,
        int tileType,
        float baseRotation,
        bool hasTop,
        bool hasBottom,
        bool hasLeft,
        bool hasRight
    )
    {
        if (tileType == 3 && hasTop && hasBottom && hasLeft && hasRight)
        {
            return GetTJunctionCornerRotation(levelMap, row, col);
        }

        if (tileType == 8)
        {
            if (hasLeft && !hasTop && !hasBottom && !hasRight)
                return 0f;
            else if (hasRight && !hasTop && !hasBottom && !hasLeft)
                return 0f;
            else if (hasTop && !hasLeft && !hasBottom && !hasRight)
                return 90f;
            else if (hasBottom && !hasTop && !hasLeft && !hasRight)
                return 90f;
        }

        return baseRotation;
    }

    private float GetTJunctionCornerRotation(int[,] map, int row, int col)
    {
        float topRotation = GetTileRotation(map, row - 1, col);
        float bottomRotation = GetTileRotation(map, row + 1, col);
        float leftRotation = GetTileRotation(map, row, col - 1);
        float rightRotation = GetTileRotation(map, row, col + 1);

        if (
            IsWall(map, row - 1, col)
            && IsWall(map, row, col + 1)
            && GetTileType(map, row - 1, col) == 4
            && GetTileType(map, row, col + 1) == 4
        )
        {
            if (topRotation == 0f && rightRotation == 90f)
                return 90f;
            if (topRotation == 90f && rightRotation == 0f)
                return 0f;
        }

        if (
            IsWall(map, row, col - 1)
            && IsWall(map, row + 1, col)
            && GetTileType(map, row, col - 1) == 4
            && GetTileType(map, row + 1, col) == 4
        )
        {
            if (leftRotation == 0f && bottomRotation == 90f)
                return 0f;
            if (leftRotation == 90f && bottomRotation == 0f)
                return 90f;
        }

        if (
            IsWall(map, row + 1, col)
            && IsWall(map, row, col + 1)
            && GetTileType(map, row + 1, col) == 4
            && GetTileType(map, row, col + 1) == 4
        )
        {
            if (bottomRotation == 0f && rightRotation == 90f)
                return 0f;
            if (bottomRotation == 90f && rightRotation == 0f)
                return 90f;
        }

        if (
            IsWall(map, row - 1, col)
            && IsWall(map, row, col - 1)
            && GetTileType(map, row - 1, col) == 4
            && GetTileType(map, row, col - 1) == 4
        )
        {
            if (topRotation == 0f && leftRotation == 90f)
                return 90f;
            if (topRotation == 90f && leftRotation == 0f)
                return 0f;
        }

        return 0f;
    }

    private float GetTileRotation(int[,] map, int row, int col)
    {
        if (!IsWall(map, row, col))
            return 0f;

        int tileType = GetTileType(map, row, col);
        if (tileType == 4)
        {
            bool hasTop = IsWall(map, row - 1, col);
            bool hasBottom = IsWall(map, row + 1, col);
            bool hasLeft = IsWall(map, row, col - 1);
            bool hasRight = IsWall(map, row, col + 1);
            return GetWallRotation(hasTop, hasBottom, hasLeft, hasRight);
        }

        return 0f;
    }

    private int GetTileType(int[,] map, int row, int col)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        if (row < 0 || row >= rows || col < 0 || col >= cols)
            return 0;

        return map[row, col];
    }

    private float GetWallRotation(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        int wallCount = 0;
        if (hasTop)
            wallCount++;
        if (hasBottom)
            wallCount++;
        if (hasLeft)
            wallCount++;
        if (hasRight)
            wallCount++;

        if (wallCount == 2)
        {
            if (hasTop && hasBottom && !hasLeft && !hasRight)
                return 0f;
            if (hasLeft && hasRight && !hasTop && !hasBottom)
                return 90f;
            if ((hasTop || hasBottom) && !(hasLeft || hasRight))
                return 0f;
            if ((hasLeft || hasRight) && !(hasTop || hasBottom))
                return 90f;
            if (hasBottom && hasLeft && !hasTop && !hasRight)
                return 90f;
            if (hasTop && hasRight && !hasBottom && !hasLeft)
                return 90f;
            if (hasTop && hasLeft && !hasBottom && !hasRight)
                return 0f;
            if (hasBottom && hasRight && !hasTop && !hasLeft)
                return 0f;
        }

        if (wallCount == 1)
        {
            if (hasTop || hasBottom)
                return 0f;
            if (hasLeft || hasRight)
                return 90f;
        }

        if (wallCount >= 3)
        {
            if (hasTop && hasBottom)
                return 0f;
            if (hasLeft && hasRight)
                return 90f;
        }

        return 0f;
    }

    private float GetCornerRotation(
        bool hasTop,
        bool hasBottom,
        bool hasLeft,
        bool hasRight,
        bool isOutside
    )
    {
        int wallCount = 0;
        if (hasTop)
            wallCount++;
        if (hasBottom)
            wallCount++;
        if (hasLeft)
            wallCount++;
        if (hasRight)
            wallCount++;

        if (wallCount == 1)
        {
            if (hasTop)
                return 90f;
            if (hasBottom)
                return 270f;
            if (hasLeft)
                return 0f;
            if (hasRight)
                return 180f;
        }

        if (wallCount == 2)
        {
            if (hasTop && hasLeft)
                return 180f;
            if (hasTop && hasRight)
                return 90f;
            if (hasBottom && hasRight)
                return 0f;
            if (hasBottom && hasLeft)
                return 270f;
        }

        if (wallCount == 3)
        {
            if (!hasTop)
                return 90f;
            if (!hasBottom)
                return 270f;
            if (!hasLeft)
                return 0f;
            if (!hasRight)
                return 270f;
        }

        if (wallCount == 4)
        {
            if (hasTop && hasBottom && hasLeft && hasRight)
            {
                return 0f;
            }
        }

        return 0f;
    }

    private float GetTJunctionRotation(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        if (hasTop && hasLeft && hasRight)
            return 0f;
        if (hasRight && hasTop && hasBottom)
            return 90f;
        if (hasBottom && hasLeft && hasRight)
            return 180f;
        if (hasLeft && hasTop && hasBottom)
            return 270f;

        return 0f;
    }

    private int[,] MirrorHorizontally(int[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        int[,] mirrored = new int[rows, cols];

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
        int[,] mirrored = new int[rows - 1, cols];

        for (int row = 0; row < rows - 1; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                mirrored[row, col] = original[rows - 2 - row, col];
            }
        }

        // Debug.Log($"Vertical mirror: new[0,0] = {mirrored[0,0]} (from original[{rows-2},0] = {original[rows-2,0]})");
        return mirrored;
    }

    private void DeleteExistingLevel()
    {
        GameObject existingLevel = GameObject.Find("Level 01");
        if (existingLevel != null)
        {
            DestroyImmediate(existingLevel);
        }

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
            mainCamera.transform.position = new Vector3(0, 0, -10);

            float totalWidth = cols * 2 * tileSize;
            float totalHeight = rows * 2 * tileSize;

            float aspectRatio = (float)Screen.width / Screen.height;
            float requiredSize = Mathf.Max(totalWidth / (2 * aspectRatio), totalHeight / 2);
            mainCamera.orthographicSize = requiredSize + 2f;

            // Debug.Log($"Camera kept at (0,0,-10), orthographic size adjusted to {mainCamera.orthographicSize}");
        }
    }

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
