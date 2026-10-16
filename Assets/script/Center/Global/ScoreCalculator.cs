using UnityEngine;

/// <summary>
/// 分数结算器（纯函数，不持有状态、不订阅事件）。
/// 输入基础分与会话时长，输出加权最终分与时间奖励，便于独立调整权重公式。
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// 计算加权最终分。
    /// </summary>
    /// <param name="baseScore">基础分</param>
    /// <param name="sessionDuration">会话时长（秒）</param>
    /// <param name="timeBonus">输出：时间奖励 = 最终分 - 基础分</param>
    /// <returns>加权后的最终分</returns>
    public static int Calculate(int baseScore, float sessionDuration, out float timeBonus)
    {
        float weight;
        if (sessionDuration <= 30f)
        {
            weight = 1.0f;
        }
        else if (sessionDuration <= 60f)
        {
            weight = Mathf.Lerp(1.0f, 1.5f, (sessionDuration - 30f) / 30f);
        }
        else if (sessionDuration <= 120f)
        {
            weight = Mathf.Lerp(1.5f, 2.0f, (sessionDuration - 60f) / 60f);
        }
        else
        {
            float extraTime = sessionDuration - 120f;
            float extraWeight = Mathf.Floor(extraTime / 60f) * 0.5f;
            weight = 2.0f + extraWeight;
        }

        int finalScore = Mathf.Max(0, Mathf.RoundToInt(baseScore * weight));
        timeBonus = finalScore - baseScore;
        return finalScore;
    }
}
