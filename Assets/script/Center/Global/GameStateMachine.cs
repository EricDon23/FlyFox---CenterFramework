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
        ChangeState(GameEnum.Playing);
        EventCenter.Instance.Broadcast("GameStart");
    }

    private void OnRequestGameOver()
    {
        Debug.Log("[GameStateMachine] 返回主菜单");
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
