using NUnit.Framework;

/// <summary>GameStateMachine 状态流转测试（三态：start/Playing/stop）。</summary>
public class GameStateMachineTests
{
    private GameStateMachine _fsm;

    [SetUp]
    public void SetUp()
    {
        EventCenter.Instance.Clear();
        _fsm = GameStateMachine.Instance;
        Global.CurrentState = GameEnum.start;
    }

    [Test]
    public void Initialize_SetsStateToStart()
    {
        _fsm.Initialize();
        Assert.AreEqual(GameEnum.start, Global.CurrentState);
    }

    [Test]
    public void RequestGameStart_FromStart_TransitionsToPlaying()
    {
        Global.CurrentState = GameEnum.start;
        _fsm.Initialize();
        EventCenter.Instance.Broadcast("RequestGameStart");
        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
    }

    [Test]
    public void RequestGameOver_FromPlaying_ReturnsToStart()
    {
        Global.CurrentState = GameEnum.Playing;
        _fsm.Initialize();
        EventCenter.Instance.Broadcast("RequestGameOver");
        Assert.AreEqual(GameEnum.start, Global.CurrentState);
    }

    [Test]
    public void RequestPause_FromPlaying_TransitionsToStop()
    {
        Global.CurrentState = GameEnum.Playing;
        _fsm.Initialize();
        EventCenter.Instance.Broadcast("RequestPause");
        Assert.AreEqual(GameEnum.stop, Global.CurrentState);
    }

    [Test]
    public void RequestPause_FromStop_ResumesToPlaying()
    {
        Global.CurrentState = GameEnum.stop;
        _fsm.Initialize();
        EventCenter.Instance.Broadcast("RequestPause");
        Assert.AreEqual(GameEnum.Playing, Global.CurrentState);
    }
}
