using NUnit.Framework;
using UnityEngine;

/// <summary>GameStateMachine 状态流转守卫与事件广播测试（EditMode）。</summary>
public class GameStateMachineTests
{
    private GameStateMachine _fsm;

    [SetUp]
    public void SetUp()
    {
        EventCenter.Instance.Clear();
        _fsm = GameStateMachine.Instance;
        Global.CurrentState = GameEnum.Menu;
    }

    [Test]
    public void Initialize_SetsStateToMenu()
    {
        _fsm.Initialize();
        Assert.AreEqual(GameEnum.Menu, Global.CurrentState);
    }

    [Test]
    public void StartGame_FromMenu_TransitionsToPlaying()
    {
        Global.CurrentState = GameEnum.Menu;
        _fsm.StartGame();
        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
    }

    [Test]
    public void StartGame_FromPaused_DoesNotTransition()
    {
        Global.CurrentState = GameEnum.Paused;
        _fsm.StartGame();
        Assert.AreEqual(GameEnum.Paused, Global.CurrentState); // 守卫拦截
    }

    [Test]
    public void PauseGame_FromPlaying_TransitionsToPaused()
    {
        Global.CurrentState = GameEnum.Playing;
        _fsm.PauseGame();
        Assert.AreEqual(GameEnum.Paused, Global.CurrentState);
    }

    [Test]
    public void PauseGame_FromMenu_DoesNotTransition()
    {
        Global.CurrentState = GameEnum.Menu;
        _fsm.PauseGame();
        Assert.AreEqual(GameEnum.Menu, Global.CurrentState);
    }

    [Test]
    public void ResumeGame_FromPaused_TransitionsBackToPlaying()
    {
        Global.CurrentState = GameEnum.Paused;
        _fsm.ResumeGame();
        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
    }

    [Test]
    public void EndGame_FromPlaying_TransitionsToGameOver()
    {
        Global.CurrentState = GameEnum.Playing;
        _fsm.EndGame(100);
        Assert.AreEqual(GameEnum.GameOver, Global.CurrentState);
    }

    [Test]
    public void EndGame_FromMenu_DoesNotTransition()
    {
        Global.CurrentState = GameEnum.Menu;
        _fsm.EndGame(100);
        Assert.AreEqual(GameEnum.Menu, Global.CurrentState);
    }

    [Test]
    public void ChangeState_BroadcastsGameStateChanged()
    {
        GameEnum received = GameEnum.Menu;
        EventCenter.Instance.Subscribe<GameEnum>("GameStateChanged", (s) => received = s);
        Global.CurrentState = GameEnum.Menu;

        _fsm.OnChangeState(GameEnum.Playing);

        Assert.AreEqual(GameEnum.Playing, received);
        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
    }

    [Test]
    public void RequestGameStart_BroadcastsViaEvent()
    {
        int gameStartCount = 0;
        _fsm.Initialize(); // 订阅 RequestGameStart
        EventCenter.Instance.Subscribe("GameStart", () => gameStartCount++);
        Global.CurrentState = GameEnum.Menu;

        EventCenter.Instance.Broadcast("RequestGameStart");

        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
        Assert.GreaterOrEqual(gameStartCount, 1);
    }
}
