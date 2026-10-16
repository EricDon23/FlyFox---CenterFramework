using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成/物品生成 网格位置数据类
/// 纯数据存储 + 网格数据访问/初始化（得失函数），可序列化，Inspector可编辑。
/// 网格坐标换算（GridToWorld / GridToLocal / WorldToGrid / LocalToGrid）已迁至 LevelBuild 逻辑层。
/// </summary>
[System.Serializable]
[AutoRegisterConfig("LevelBuild")]
public class SpawnPositionData : BasicDate
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

    #region 网格数据访问（得失函数）
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
