using System;
using UnityEngine;

[Serializable]
[AutoRegisterConfig("Score")]
public class Score :BasicDate
{
    [SerializeField] private GridType _type;
    [SerializeField] private int _baseScore;
    [SerializeField] private int _currentScore;

    // 公共属性
    public int BaseScore
    {
        get => _baseScore;
        set => _baseScore = value;
    }
    public int CurrentScore
    {
        get => _currentScore;
        set => _currentScore = value;
    }
    public GridType Type
    {
        get => _type;
        set => _type = value;
    }
    // 构造函数
    // 【改造】显式无参构造：ConfigCenter 反射创建需要；旧代码只有 Score(int=1) 带可选参数，Activator 找不到无参构造会抛异常
    public Score() { }
    public Score(int baseScore = 1)
    {
        _baseScore = baseScore;
        _currentScore = baseScore;
    }
}