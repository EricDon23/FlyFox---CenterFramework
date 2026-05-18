using System;
using UnityEngine;

public static class GlobalMonitor
{
    // 保留所有原事件名和签名
    public static event Action<GameEnum> OnGameStateChanged;
    public static event Action<int> OnFinalScoreCalculated;

    // 保留原属性（新增内部实现优化）
    public static bool IsPaused => Global.CurrentState == GameEnum.Paused;
    public static bool IsPlaying => Global.CurrentState == GameEnum.Playing;

    // 保留原函数名和逻辑
    public static void Initialize()
    {
        Global.StartTime = Time.time;
        Global.CurrentState = GameEnum.Menu;
        ResetSessionData();
        Debug.Log("[GlobalMonitor] 全局监控已初始化");
    }

    public static void StartGame()
    {
        if (Global.CurrentState != GameEnum.Menu && Global.CurrentState != GameEnum.GameOver) return;

        ResetSessionData();
        Global.SessionStartTime = Time.time;
        ChangeState(GameEnum.Playing);
        Debug.Log("[GlobalMonitor] 单局游戏开始");
    }

    public static void PauseGame()
    {
        if (Global.CurrentState != GameEnum.Playing) return;

        Global.SessionPausedTime = Time.time;
        ChangeState(GameEnum.Paused);
        Debug.Log("[GlobalMonitor] 游戏暂停");
    }

    public static void ResumeGame()
    {
        if (Global.CurrentState != GameEnum.Paused) return;

        Global.TotalPausedDuration += Time.time - Global.SessionPausedTime;
        ChangeState(GameEnum.Playing);
        Debug.Log("[GlobalMonitor] 游戏恢复");
    }

    public static void EndGame(int baseScore)
    {
        if (Global.CurrentState != GameEnum.Playing) return;

        Global.SessionBaseScore = baseScore;
        CalculateFinalScore();
        ChangeState(GameEnum.GameOver);
        Debug.Log($"[GlobalMonitor] 游戏结束！最终得分：{Global.FinalSessionScore}");
    }

    public static void UpdateMonitor()
    {
        Global.GameDuration = Time.time - Global.StartTime;

        if (Global.CurrentState == GameEnum.Playing)
        {
            Global.SessionDuration = Time.time - Global.SessionStartTime - Global.TotalPausedDuration;
        }
    }

    public static void OnApplicationQuit()
    {
        ResetSessionData();
        Global.GameDuration += Time.time;
        Debug.Log($"[GlobalMonitor] 游戏退出！总游戏时长：{BaseUtils.FormatTime(Global.GameDuration)}");
    }

    // 保留原内部逻辑
    private static void CalculateFinalScore()
    {
        float weight;

        if (Global.SessionDuration <= 30f)
        {
            weight = 1.0f;
        }
        else if (Global.SessionDuration <= 60f)
        {
            weight = Mathf.Lerp(1.0f, 1.5f, (Global.SessionDuration - 30f) / 30f);
        }
        else if (Global.SessionDuration <= 120f)
        {
            weight = Mathf.Lerp(1.5f, 2.0f, (Global.SessionDuration - 60f) / 60f);
        }
        else
        {
            float extraTime = Global.SessionDuration - 120f;
            float extraWeight = Mathf.Floor(extraTime / 60f) * 0.5f;
            weight = 2.0f + extraWeight;
        }

        Global.FinalSessionScore = Mathf.Max(0, Mathf.RoundToInt(Global.SessionBaseScore * weight));
        Global.SessionTimeBonus = Global.FinalSessionScore - Global.SessionBaseScore;

        OnFinalScoreCalculated?.Invoke(Global.FinalSessionScore);
    }

    private static void ResetSessionData()
    {
        Global.SessionDuration = 0f;
        Global.TotalPausedDuration = 0f;
        Global.SessionBaseScore = 0;
        Global.SessionTimeBonus = 0f;
        Global.FinalSessionScore = 0;
        Global.SessionStartTime = 0f;
        Global.SessionPausedTime = 0f;
    }

    private static void ChangeState(GameEnum newState)
    {
        if (Global.CurrentState == newState) return;
        Global.CurrentState = newState;
        OnGameStateChanged?.Invoke(Global.CurrentState);
    }
}