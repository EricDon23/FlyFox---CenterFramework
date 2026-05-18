using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelBuild
{
    private readonly SpawnPositionData _spawnData;
    private readonly Dictionary<GridType, GameObject> _prefabMap;
    private static Dictionary<ScoreShape, int> _shapeCounts = new Dictionary<ScoreShape, int>();

    public LevelBuild(SpawnPositionData spawnData, Dictionary<GridType, GameObject> prefabMap)
    {
        _spawnData = spawnData ?? throw new ArgumentNullException(nameof(spawnData));
        _prefabMap = prefabMap ?? throw new ArgumentNullException(nameof(prefabMap));
    }

    // ==================== 核心：规则化数据填充（已重构） ====================
public void SetGridItemType(float currentHp, float maxHp)
{
    if (_spawnData.gridCells == null || _spawnData.gridCells.Count == 0)
    {
        Debug.LogError("网格未初始化！请先调用 SpawnPositionData.InitializeGrid()");
        return;
    }

    InitializeShapeCounts();
    bool berrySpawned = false;
    int currentColumn = 0;

    // 限制最大生成组数，避免数量过多
    int maxShapes = 5;
    int shapeCount = 0;

    while (currentColumn < _spawnData.gridWidth && shapeCount < maxShapes)
    {
        // 1. 随机形状（平衡四种形状数量）
        ScoreShape shape = GetRandomBalancedShape();

        // 【关键】生成长度强制锁定在 4~8
        int shapeLength = UnityEngine.Random.Range(4, 9); // 4 ≤ length ≤ 8

        // 2. 根据形状类型，严格限制起点Y的范围，避免出界
        int startY;
        if (shape == ScoreShape.vertical)
        {
            // 竖线：必须留够下方空间，避免超出网格底部
            startY = UnityEngine.Random.Range(0, _spawnData.gridHeight - shapeLength);
        }
        else if (shape == ScoreShape.diagonal_left)
        {
            // 左斜线（y -= i）：必须留够下方空间，避免y < 0
            startY = UnityEngine.Random.Range(shapeLength - 1, _spawnData.gridHeight);
        }
        else if (shape == ScoreShape.diagonal_right)
        {
            // 右斜线（y += i）：必须留够上方空间，避免超出网格底部
            startY = UnityEngine.Random.Range(0, _spawnData.gridHeight - shapeLength);
        }
        else // horizontal
        {
            startY = UnityEngine.Random.Range(0, _spawnData.gridHeight);
        }

        // 3. 生成形状位置，并做双重校验
        List<Vector2Int> shapePositions = GetShapePositions(currentColumn, startY, shape, shapeLength);
        bool isValid = true;

        foreach (var pos in shapePositions)
        {
            // 校验1：是否在网格内
            if (!_spawnData.IsValidGrid(pos.x, pos.y))
            {
                isValid = false;
                break;
            }
            // 校验2：是否和已有物品重叠
            if (_spawnData.GetGridType(pos.x, pos.y) != GridType.Empty)
            {
                isValid = false;
                break;
            }
        }

        // 4. 校验通过才生成
        if (isValid)
        {
            foreach (var pos in shapePositions)
            {
                GridType itemType = GetRandomItemType(Global.Level, currentHp, maxHp, ref berrySpawned);
                _spawnData.SetGridCell(pos.x, pos.y, itemType);
            }
            shapeCount++;
        }

        // 【关键】取消间隔：生成后直接从当前形状的末尾继续
        currentColumn += shapeLength+2;
    }

    // 血量低时强制生成一个berry
    if (!berrySpawned && currentHp / maxHp < 0.5f)
    {
        SpawnSingleBerry();
    }

    Debug.Log($"✅ 无间隔连续生成完成，共生成 {shapeCount} 组形状（长度4~8）");
}

    // ==================== 辅助函数（已修复） ====================
    private static void InitializeShapeCounts()
    {
        _shapeCounts.Clear();
        foreach (ScoreShape shape in Enum.GetValues(typeof(ScoreShape)))
        {
            _shapeCounts[shape] = 0;
        }
    }

    private static ScoreShape GetRandomBalancedShape()
    {
        List<ScoreShape> candidates = new List<ScoreShape>();
        int minCount = int.MaxValue;

        foreach (var kvp in _shapeCounts)
        {
            if (kvp.Value < minCount)
            {
                minCount = kvp.Value;
                candidates.Clear();
                candidates.Add(kvp.Key);
            }
            else if (kvp.Value == minCount)
            {
                candidates.Add(kvp.Key);
            }
        }

        ScoreShape selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _shapeCounts[selected]++;
        return selected;
    }

    // 【关键】严格按形状生成格子位置，不越界
    private List<Vector2Int> GetShapePositions(int startX, int startY, ScoreShape shape, int length)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int i = 0; i < length; i++)
        {
            int x = startX;
            int y = startY;

            switch (shape)
            {
                case ScoreShape.horizontal:
                    x += i;
                    break;
                case ScoreShape.vertical:
                    y += i;
                    break;
                case ScoreShape.diagonal_left:
                    x += i;
                    y -= i;
                    break;
                case ScoreShape.diagonal_right:
                    x += i;
                    y += i;
                    break;
            }

            // 只保留在网格内的位置
            if (_spawnData.IsValidGrid(x, y))
            {
                positions.Add(new Vector2Int(x, y));
            }
        }

        return positions;
    }

    private static GridType GetRandomItemType(int level, float currentHp, float maxHp, ref bool berrySpawned)
    {
        float hpNormalized = currentHp / maxHp;

        // 1. 先判断berry（只生成一个）
        if (!berrySpawned)
        {
            float berryProb = GetBerryProbability(hpNormalized);
            if (UnityEngine.Random.value < berryProb)
            {
                berrySpawned = true;
                return GridType.cherry;
            }
        }

        // 2. 再判断coin/gem（按等级）
        var (coinProb, gemProb) = GetCoinGemProbability(level);
        float r = UnityEngine.Random.value;

        if (r < coinProb)
            return GridType.Coin;
        else
            return GridType.gem;
    }

    private static float GetBerryProbability(float hpNormalized)
    {
        if (hpNormalized >= 1f) return 0.3f;
        if (hpNormalized <= 0.1f) return 0.9f;
        return 0.3f + (1f - hpNormalized) * 0.5f;
    }

    private static (float coin, float gem) GetCoinGemProbability(int level)
    {
        if (level >= 7) return (0.2f, 0.8f);
        if (level >= 5) return (0.5f, 0.5f);
        if (level > 2) return (0.7f, 0.3f);
        return (0.9f, 0.1f);
    }

    private void SpawnSingleBerry()
    {
        for (int x = 0; x < _spawnData.gridWidth; x++)
        {
            for (int y = 0; y < _spawnData.gridHeight; y++)
            {
                if (_spawnData.GetGridType(x, y) == GridType.Empty)
                {
                    _spawnData.SetGridCell(x, y, GridType.cherry);
                    return;
                }
            }
        }
    }

    // ==================== 预制体绑定 & 场景构建（保持不变） ====================
    public void SetGridPrefab()
    {
        if (_spawnData.gridCells == null || _spawnData.gridCells.Count == 0)
        {
            Debug.LogError("网格未初始化！请先调用 SpawnPositionData.InitializeGrid()");
            return;
        }
        // 1. 绑定预制体
        int boundCount = 0;
        for (int y = 0; y < _spawnData.gridHeight; y++)
        {
            for (int x = 0; x < _spawnData.gridWidth; x++)
            {
                GridCell cell = _spawnData.gridCells[y][x];
                if (cell.Type == GridType.Empty) continue;
                if (!_prefabMap.TryGetValue(cell.Type, out GameObject prefab))//
                {
                    Debug.LogWarning($"未找到 {cell.Type} 对应的预制体！跳过格子({x},{y})");
                    continue;
                }
                cell.Prefab = prefab;
                boundCount++;
            }
        }

        Debug.Log($"✅ 预制体绑定完成！共绑定 {boundCount} 个格子");
    }

    public void BuildLevel(Transform parent = null)
    {
        if (parent == null)
        {
            Debug.LogError("父物体不能为空！请传入有效的 Transform");
            return;
        }
        if (_spawnData.gridCells == null || _spawnData.gridCells.Count == 0)
        {
            Debug.LogError("网格未初始化！请先调用 SpawnPositionData.InitializeGrid()");
            return;
        }

        bool hasPrefab = false;
        foreach (var row in _spawnData.gridCells)
        {
            foreach (var cell in row)
            {
                if (cell.Prefab != null)
                {
                    hasPrefab = true;
                    break;
                }
            }
            if (hasPrefab) break;
        }
        if (!hasPrefab)
        {
            Debug.LogError("未绑定任何预制体！请先调用 SetGridPrefab()");
            return;
        }

        int spawnCount = 0;
        for (int y = 0; y < _spawnData.gridHeight; y++)
        {
            for (int x = 0; x < _spawnData.gridWidth; x++)
            {
                GridCell cell = _spawnData.gridCells[y][x];
                if (cell.Prefab == null) continue;

                Vector2 localPos = _spawnData.GridToLocal(x, y);
                GameObject go = GameObject.Instantiate(cell.Prefab, parent);
                ItemManager itemManager = go.GetComponent<ItemManager>();
                if (itemManager != null)
                {
                    itemManager.SetFruit(cell.Type);
                }
                else
                {
                    Debug.LogWarning($"{cell.Prefab} 未绑定 ItemManager 组件！");
                }
                go.transform.localPosition = localPos;


                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.name = $"{cell.Type}_{x}_{y}";

                spawnCount++;
            }
        }

        Debug.Log($"✅ 场景构建完成！共生成 {spawnCount} 个物体");
    }
}