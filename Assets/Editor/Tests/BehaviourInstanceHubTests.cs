using NUnit.Framework;
using UnityEngine;

/// <summary>BehaviourInstanceHub 实例仓库测试（EditMode）。</summary>
public class BehaviourInstanceHubTests
{
    // 记录 LogicDestroy 是否被调用的桩行为
    private class SpyBehavior : Objectbehavior
    {
        public bool DestroyCalled;
        public override void LogicDestroy() { DestroyCalled = true; }
    }

    private BehaviourInstanceHub _hub;

    [SetUp]
    public void SetUp()
    {
        _hub = BehaviourInstanceHub.Instance;
        _hub.ClearAll();
    }

    [TearDown]
    public void TearDown()
    {
        _hub.ClearAll();
    }

    [Test]
    public void AddToStack_IncreasesCount()
    {
        var b = new SpyBehavior();
        _hub.AddToStack(b);
        Assert.AreEqual(1, _hub.Behaviours.Count);
        Assert.AreSame(b, _hub.Behaviours[0]);
    }

    [Test]
    public void AddPending_ThenTryTake_ReturnsSame()
    {
        var b = new SpyBehavior();
        _hub.AddPending("pendingKey", b);
        Assert.IsTrue(_hub.TryTakePending("pendingKey", out var taken));
        Assert.AreSame(b, taken);
    }

    [Test]
    public void TryTakePending_UnknownKey_ReturnsFalse()
    {
        Assert.IsFalse(_hub.TryTakePending("no_such_pending", out _));
    }

    [Test]
    public void RemovePending_RemovesEntry()
    {
        _hub.AddPending("pendingKey", new SpyBehavior());
        _hub.RemovePending("pendingKey");
        Assert.IsFalse(_hub.TryTakePending("pendingKey", out _));
    }

    [Test]
    public void DestroyAll_CallsLogicDestroyOnAllStackedBehaviors()
    {
        var b1 = new SpyBehavior();
        var b2 = new SpyBehavior();
        _hub.AddToStack(b1);
        _hub.AddToStack(b2);

        _hub.DestroyAll();

        Assert.IsTrue(b1.DestroyCalled);
        Assert.IsTrue(b2.DestroyCalled);
        Assert.AreEqual(0, _hub.Behaviours.Count);
    }

    [Test]
    public void DestroyAll_ClearsPendingToo()
    {
        _hub.AddPending("pendingKey", new SpyBehavior());
        _hub.DestroyAll();
        Assert.IsFalse(_hub.TryTakePending("pendingKey", out _));
    }

    [Test]
    public void ClearAll_EmptiesStackAndPendingWithoutCallingDestroy()
    {
        var b = new SpyBehavior();
        _hub.AddToStack(b);
        _hub.AddPending("p", b);
        _hub.ClearAll();
        Assert.IsFalse(b.DestroyCalled);
        Assert.AreEqual(0, _hub.Behaviours.Count);
    }
}
