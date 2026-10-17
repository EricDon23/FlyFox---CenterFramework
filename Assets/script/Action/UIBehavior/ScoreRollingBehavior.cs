using UnityEngine;

/// <summary>
/// 分数行为：仅在 Playing 状态累计分数。
/// 加分由 Coin/Gem/Cherry 碰撞触发，本类只维护分数数值，不生成滚动背景条。
/// </summary>
[AutoRegisterBehaviour("ScoreRolling")]
public class ScoreRollingBehavior : Objectbehavior
{
    public ScoreRollingBehavior() { isBuild = true; isGlobal = true; }

    private float _elapsedTime;

    public override void LogicAwake()
    {
        Debug.Log("[ScoreRolling] LogicAwake 完成，分数系统就绪");
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
    }

    private void OnUpdate()
    {
        if (Global.CurrentState != GameEnum.Playing) return;
        // 时间累计分：每秒 +10 分
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= 1f)
        {
            _elapsedTime -= 1f;
            Global.SessionBaseScore += 10;
        }
    }
}
