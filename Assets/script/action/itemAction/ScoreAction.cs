using UnityEngine;

public class ScoreAction
{
    private readonly Score _score;
    private readonly Fruit _fruit;
    
    private bool isFruit; // 仅当Score是Fruit时才会赋值

    public void OnGetGridType()
    {
        _fruit.OnGridItemTypeChanged?.Invoke();
    }

    // 修复：安全的构造函数，自动识别子类
     public ScoreAction(Score score, int healAmount = 0)
    {
        if (score == null)
        {
            Debug.LogError("Score 不能为空！");
            return;
        }
        _score = score;
        if (healAmount > 0 && score is Fruit fruit)
        {
            _fruit = fruit;
     
            _fruit.Heal = healAmount;
            isFruit = true;
            Debug.Log($"[ScoreAction] 获取治疗量：{_fruit.Heal}");
        }
    }

    /// <summary>
    /// 根据等级计算当前分数
    /// </summary>
    public void DevelopScore()
    {
        // 修复：严谨的等级权重计算
        float weight = 1f;
        int level = Global.Level;

        if (level > 0)
        {
            weight = 1 + level * 0.25f;
        }

        _score.CurrentScore = Mathf.RoundToInt(_score.BaseScore * weight);
    }

    /// <summary>
    /// 核心逻辑：满血加分，非满血(Fruit)加血不加分
    /// </summary>
    public bool GetCurrentScore(Player player)
    {
        // 空判断
        if (player == null) return true;

        // 如果是水果类，判断血量
        if (isFruit)
        {
            // 玩家非满血 → 加血，不加分
            if (player.Health < player.MaxHealth)
            {
                return false;
            }
        }

        // 普通物品 / 玩家满血 → 加分
        Global.SessionBaseScore += _score.CurrentScore;
        Debug.Log($"[ScoreAction] 获得分数：{_score.CurrentScore} | 全局总分：{Global.SessionBaseScore}");
        return true;
    }
    

    /// <summary>
    /// 获取治疗量（仅水果有效）
    /// </summary>
    public int GetHealAmount()
    {
        return isFruit ? _fruit.Heal : 0;
    }
}