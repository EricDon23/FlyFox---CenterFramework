using UnityEngine;

/// <summary>
/// 游戏状态机（纯 C# 单例）：状态流转、状态事件广播、会话数据重置、UI请求订阅。
/// 结算委托给 ScoreCalculator，自身不写权重公式。
/// </summary>
public class GameStateMachine : BasicSingle<GameStateMachine>
{
    private GameStateMachine() { }

    /// <summary>初始化状态事件、订阅请求、重置会话并置初始状态（Menu）。</summary>
    public void Initialize()
    {
        EventCenter.Instance.RegisterEvent("GameStateChanged");
        EventCenter.Instance.RegisterEvent("GameStart");
        EventCenter.Instance.RegisterEvent("GamePause");
        EventCenter.Instance.RegisterEvent("GameOver");

        EventCenter.Instance.Subscribe("RequestGameStart", OnRequestGameStart);
        EventCenter.Instance.Subscribe("RequestGameOver", OnRequestGameOver);

        ResetSessionData();
        Global.StartTime = Time.time;
        Global.CurrentState = GameEnum.Menu;
    }

    // ---------------- 状态广播 ----------------
    public void OnChangeState(GameEnum state)
    {
        Debug.Log("切换游戏状态：" + state);
        ChangeState(state);
    }

    public void OnGameStart()
    {
        if (Global.CurrentState == GameEnum.Playing) return;
        Debug.Log("开始游戏");
        EventCenter.Instance.Broadcast("GameStart");
    }

    public void OnGamePause()
    {
        if (Global.CurrentState == GameEnum.Paused) return;
        Debug.Log("暂停游戏");
        EventCenter.Instance.Broadcast("GamePause");
    }

    public void OnGameOver()
    {
        Debug.Log("游戏结束");
        EventCenter.Instance.Broadcast("GameOver");
    }

    // ---------------- UI 请求（经 EventCenter 触发） ----------------
    private void OnRequestGameStart()
    {
        OnGameStart();
        OnChangeState(GameEnum.Playing);
    }

    private void OnRequestGameOver()
    {
        OnGameOver();
        OnChangeState(GameEnum.GameOver);
    }

    // ---------------- 状态流转 ----------------
    public void StartGame()
    {
        if (Global.CurrentState != GameEnum.Menu && Global.CurrentState != GameEnum.GameOver) return;
        ResetSessionData();
        Global.SessionStartTime = Time.time;
        ChangeState(GameEnum.Playing);
        EventCenter.Instance.Broadcast("GameStart");
        Debug.Log("[GameStateMachine] 单局游戏开始");
    }

    public void PauseGame()
    {
        if (Global.CurrentState != GameEnum.Playing) return;
        Global.SessionPausedTime = Time.time;
        ChangeState(GameEnum.Paused);
        EventCenter.Instance.Broadcast("GamePause");
        Debug.Log("[GameStateMachine] 游戏暂停");
    }

    public void ResumeGame()
    {
        if (Global.CurrentState != GameEnum.Paused) return;
        Global.TotalPausedDuration += Time.time - Global.SessionPausedTime;
        ChangeState(GameEnum.Playing);
        Debug.Log("[GameStateMachine] 游戏恢复");
    }

    public void EndGame(int baseScore)
    {
        if (Global.CurrentState != GameEnum.Playing) return;
        Global.SessionBaseScore = baseScore;
        ApplyFinalScore();
        ChangeState(GameEnum.GameOver);
        EventCenter.Instance.Broadcast("GameOver");
        Debug.Log($"[GameStateMachine] 游戏结束！最终得分：{Global.FinalSessionScore}");
    }

    private void ChangeState(GameEnum newState)
    {
        if (Global.CurrentState == newState) return;
        Global.CurrentState = newState;
        EventCenter.Instance.Broadcast("GameStateChanged", newState);
    }

    // ---------------- 结算（委托纯函数） ----------------
    private void ApplyFinalScore()
    {
        Global.FinalSessionScore = ScoreCalculator.Calculate(
            Global.SessionBaseScore, Global.SessionDuration, out float timeBonus);
        Global.SessionTimeBonus = timeBonus;
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
