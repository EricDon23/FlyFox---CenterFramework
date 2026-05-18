using System;
using UnityEngine;

[Serializable]
public class Fruit : Score
{
    // 修复：标准化治疗量属性（可读可写，序列化）
    [SerializeField] private int _heal;

    public int Heal
    {
        get => _heal;
        set => _heal = value;
    }
    private Action GetGridItemType;
    public Action OnGridItemTypeChanged
    {
        get => GetGridItemType;
        set => GetGridItemType = value;
    }
}