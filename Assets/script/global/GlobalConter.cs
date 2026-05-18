using System;
using UnityEngine;

public class GlobalConter 
{
    // 优化1：线程安全单例（对外接口不变）
    private static readonly Lazy<GlobalConter> _lazyInstance = new Lazy<GlobalConter>(() => new GlobalConter());
    public static GlobalConter Instance => _lazyInstance.Value;

    // 保留所有原事件名和签名
    public Action<GameEnum> ChanageState{get;set;}
    public Action GameStart{get;set;}
    public Action GamePause{get;set;}
    public Action GameOver{get;set;}

    // 私有构造函数，防止外部实例化
    private GlobalConter() { }

    // 保留原函数名和逻辑
    public void OnChangeState(GameEnum state)
    {
        Debug.Log("切换游戏状态：" + state);
        ChanageState?.Invoke(state);
    }

    public void OnGamePause()
    {
        if(GlobalMonitor.IsPaused) return;
        Debug.Log("暂停游戏");
        GamePause?.Invoke();
    }

    public void OnGameStart()
    {
        if(GlobalMonitor.IsPlaying) return;
        Debug.Log("开始游戏");
        GameStart?.Invoke();
    }

    public void OnGameOver()
    {    
        Debug.Log("游戏结束");
        GameOver?.Invoke();
    }
}