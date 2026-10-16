using System;
using UnityEngine;

[Serializable]
public class Fruit : Score
{
    // 显式无参构造：基类 Score 仅有 Score(int baseScore = 1)，需显式链到 base(1) 才能被无参构造
    public Fruit() : base(1) { }

    // 标准化治疗量属性（可读可写，序列化）
    [SerializeField] private int _heal;

    public int Heal
    {
        get => _heal;
        set => _heal = value;
    }
}
