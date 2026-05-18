using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成/物品生成 网格位置数据类
/// 纯数据存储，可序列化，Inspector可编辑
/// </summary>
[System.Serializable]
public class SpawnPositionData
{
    [Header("=== 网格基础配置 ===")]
    [Tooltip("单个格子的世界单位大小（比如1x1、2x2）")]
    public Vector2 cellSize = new Vector2(1f, 1f);
    
    [Tooltip("网格原点偏移：(0,0)索引对应的世界坐标位置")]
    public Vector2 originOffset = Vector2.zero;

    [Header("=== 网格尺寸 ===")]
    [Tooltip("网格宽度（列数）")]
    public int gridWidth = 19;
    
    [Tooltip("网格高度（行数）")]
    public int gridHeight = 7;

    [Header("=== 网格数据（二维表格） ===")]
    [Tooltip("二维网格数据：[行][列]，行对应gridY，列对应gridX")]
    public List<List<GridCell>> gridCells = new List<List<GridCell>>();

    #region 初始化函数
    /// <summary>
    /// 代码初始化网格（可选，Inspector也可手动编辑）
    /// </summary>
    public void InitializeGrid(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        gridCells.Clear();

        for (int y = 0; y < height; y++)
        {
            List<GridCell> row = new List<GridCell>();
            for (int x = 0; x < width; x++)
            {
                row.Add(new GridCell(GridType.Empty, null));
            }
            gridCells.Add(row);
        }
    }
    #endregion

    #region 核心转换函数（你的需求重点）
    /// <summary>
    /// 【核心】二维网格索引 → 世界坐标Vector2（中心为0,0）
    /// </summary>
    /// <param name="gridX">网格X索引（列）</param>
    /// <param name="gridY">网格Y索引（行）</param>
    /// <returns>对应的世界坐标位置</returns>
    public Vector2 GridToWorld(int gridX, int gridY)
    {
        // 计算中心偏移（让整个表格中心点为 0,0）
        float centerOffsetX = (gridWidth - 1) / 2f;
        float centerOffsetY = (gridHeight - 1) / 2f;

        // 最终位置 = (索引 - 中心偏移) * 单个格子大小
        float x = (gridX - centerOffsetX) * cellSize.x;
        float y = (gridY - centerOffsetY) * cellSize.y;

        return new Vector2(x, y);
    }

    /// <summary>
    /// 【核心】二维网格索引 → 相对父物体的本地坐标Vector2（中心为0,0）
    /// </summary>
    /// <param name="gridX">网格X索引（列）</param>
    /// <param name="gridY">网格Y索引（行）</param>
    /// <returns>对应的相对父物体本地坐标位置</returns>
    public Vector2 GridToLocal(int gridX, int gridY)
    {
        // 和世界坐标计算逻辑完全一致
        // 因为父物体在世界原点时，本地坐标 = 世界坐标
        // 父物体移动时，子物体的localPosition会自动跟随
        return GridToWorld(gridX, gridY);
    }

    /// <summary>
    /// 【核心】世界坐标Vector2 → 二维网格索引
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    /// <returns>对应的网格索引（Vector2Int）</returns>
    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
         float centerOffsetX = (gridWidth - 1) / 2f;
        float centerOffsetY = (gridHeight - 1) / 2f;

        int gridX = Mathf.RoundToInt(worldPos.x / cellSize.x + centerOffsetX);
        int gridY = Mathf.RoundToInt(worldPos.y / cellSize.y + centerOffsetY);

        return new Vector2Int(gridX, gridY);
    }

    /// <summary>
    /// 【核心】相对父物体的本地坐标Vector2 → 二维网格索引
    /// </summary>
    /// <param name="localPos">相对父物体的本地坐标位置</param>
    /// <returns>对应的网格索引（Vector2Int）</returns>
    public Vector2Int LocalToGrid(Vector2 localPos)
    {
        // 和世界坐标转索引逻辑完全一致
        return WorldToGrid(localPos);
    }
    #endregion

    #region 辅助功能函数
    /// <summary>
    /// 检查网格索引是否有效（防止越界）
    /// </summary>
    public bool IsValidGrid(int gridX, int gridY)
    {
        return gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight;
    }

    /// <summary>
    /// 获取指定索引的格子类型
    /// </summary>
    public GridType GetGridType(int gridX, int gridY)
    {
        return IsValidGrid(gridX, gridY) ? gridCells[gridY][gridX].Type : GridType.Empty;
    }

    /// <summary>
    /// 设置指定索引的格子类型+预制体
    /// </summary>
    public void SetGridCell(int gridX, int gridY, GridType type, GameObject prefab = null)
    {
        if (!IsValidGrid(gridX, gridY)) return;
        
        gridCells[gridY][gridX].Type = type;
        gridCells[gridY][gridX].Prefab = prefab;
    }

    /// <summary>
    /// 获取指定索引的格子预制体
    /// </summary>
    public GameObject GetGridPrefab(int gridX, int gridY)
    {
        return IsValidGrid(gridX, gridY) ? gridCells[gridY][gridX].Prefab : null;
    }
    #endregion
}