using System;
using UnityEngine;

[Serializable]
public class Score
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
    public Score(int baseScore = 1)
    {
        _baseScore = baseScore;
        _currentScore = baseScore;
    }
}
//分数的分布形状
public enum ScoreShape
{
   horizontal,
   vertical,
   diagonal_left,
   diagonal_right  
}