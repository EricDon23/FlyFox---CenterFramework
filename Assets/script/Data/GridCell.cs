using System;
using UnityEngine;

/// <summary>
/// 单个网格格子的数据（LevelBuild 内部单元，不独立注册配置）。
/// </summary>
[System.Serializable]
public class GridCell : BasicDate
{
    [Tooltip("格子类型")]
    private GridType type;

    [Tooltip("可选：绑定对应的生成预制体（敌人/物品/障碍）")]
    private GameObject prefab;

    public GridCell() { }

    public GridCell(GridType type, GameObject prefab = null)
    {
        this.type = type;
        this.prefab = prefab;
    }

    public GridType Type { get => type; set => type = value; }
    public GameObject Prefab { get => prefab; set => prefab = value; }
}
