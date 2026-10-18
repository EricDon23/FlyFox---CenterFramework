using UnityEngine;

/// <summary>
/// 游戏状态机（纯 C# 单例）：
/// start → 主菜单初始化界面
/// Playing → 游戏进行中（LevelBuild/玩家/GamingUI）
/// stop → 暂停
/// </summary>
public class GameStateMachine : BasicSingle<GameStateMachine>
{
    private GameStateMachine() { }

    public void Initialize()
    {
        EventCenter.Instance.RegisterEvent("GameStateChanged");
        EventCenter.Instance.RegisterEvent("GameStart");
        EventCenter.Instance.RegisterEvent("GamePause");

        EventCenter.Instance.Subscribe("RequestGameStart", OnRequestGameStart);
        EventCenter.Instance.Subscribe("RequestGameOver", OnRequestGameOver);
        EventCenter.Instance.Subscribe("RequestPause", OnRequestPause);

        ResetSessionData();
        Global.StartTime = Time.time;
        Global.CurrentState = GameEnum.start;
        Debug.Log("[GameStateMachine] 初始化完成，当前状态：start");
    }

    private void OnRequestGameStart()
    {
        Debug.Log("[GameStateMachine] 开始游戏");
        ResetSessionData();
        Global.SessionStartTime = Time.time;  // 记录本局开始时间
        ChangeState(GameEnum.Playing);
        EventCenter.Instance.Broadcast("GameStart");
    }

    private void OnRequestGameOver()
    {
        Debug.Log("[GameStateMachine] 游戏结束，结算本局数据");
        // 计算本局持续时间
        Global.SessionDuration = Time.time - Global.SessionStartTime;
        // 计算最终分数（基础分 + 时间加成）
        Global.SessionTimeBonus = Global.SessionDuration * 10f;
        Global.FinalSessionScore = Global.SessionBaseScore + Mathf.RoundToInt(Global.SessionTimeBonus);
        Debug.Log($"[GameStateMachine] 本局时长={Global.SessionDuration:F1}s，基础分={Global.SessionBaseScore}，时间加成={Global.SessionTimeBonus:F0}，最终分={Global.FinalSessionScore}");
        ChangeState(GameEnum.start);
    }

    private void OnRequestPause()
    {
        if (Global.CurrentState == GameEnum.Playing)
        {
            ChangeState(GameEnum.stop);
            EventCenter.Instance.Broadcast("GamePause");
        }
        else if (Global.CurrentState == GameEnum.stop)
        {
            ChangeState(GameEnum.Playing);
        }
    }

    private void ChangeState(GameEnum newState)
    {
        if (Global.CurrentState == newState) return;
        Global.CurrentState = newState;
        Debug.Log($"[GameStateMachine] 状态切换 → {newState}");
        EventCenter.Instance.Broadcast("GameStateChanged", newState);
    }

    private void ResetSessionData()
    {
        Global.SessionDuration = 0f;
        Global.TotalPausedDuration = 0f;
        Global.SessionBaseScore = 0;
        Global.SessionTimeBonus = 0f;
        Global.FinalSessionScore = 0;
        Global.SessionStartTime = 0f;
        Global.SessionPausedTime = 0f;
    }
}
