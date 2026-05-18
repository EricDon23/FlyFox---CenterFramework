using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 单个网格格子的数据
/// </summary>
[System.Serializable]

public class GridCell
{
    [Tooltip("格子类型")]
    private GridType type;
    
    [Tooltip("可选：绑定对应的生成预制体（敌人/物品/障碍）")]
    private GameObject prefab;

    public GridCell(GridType type,GameObject prefab = null)
    {
        this.type = type;
        this.prefab = prefab;
    }

    public GridType Type { get => type; set => type = value; }
    public GameObject Prefab { get => prefab; set => prefab = value; }
    

}
public enum GridType
{
    Empty,      // 空（无内容）
    cherry,
    Coin,
    gem    // 物品

}