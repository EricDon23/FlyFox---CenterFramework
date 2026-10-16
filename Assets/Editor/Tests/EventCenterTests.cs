using System;
using NUnit.Framework;

/// <summary>EventCenter 事件总线测试（EditMode）。</summary>
public class EventCenterTests
{
    [SetUp]
    public void SetUp()
    {
        EventCenter.Instance.Clear();
    }

    [Test]
    public void SubscribeAndBroadcast_ZeroArg_InvokesCallback()
    {
        int count = 0;
        EventCenter.Instance.Subscribe("Test", () => count++);
        EventCenter.Instance.Broadcast("Test");
        Assert.AreEqual(1, count);
    }

    [Test]
    public void SubscribeAndBroadcast_SingleArg_InvokesCallback()
    {
        int received = 0;
        EventCenter.Instance.Subscribe<int>("Test", (v) => received = v);
        EventCenter.Instance.Broadcast("Test", 42);
        Assert.AreEqual(42, received);
    }

    [Test]
    public void Unsubscribe_StopsDelivery()
    {
        int count = 0;
        Action cb = () => count++;
        EventCenter.Instance.Subscribe("Test", cb);
        EventCenter.Instance.Broadcast("Test");
        EventCenter.Instance.Unsubscribe("Test", cb);
        EventCenter.Instance.Broadcast("Test");
        Assert.AreEqual(1, count);
    }

    [Test]
    public void MixedSignatureSubscribe_Throws()
    {
        EventCenter.Instance.Subscribe("Test", () => { });
        Assert.Throws<InvalidOperationException>(
            () => EventCenter.Instance.Subscribe<int>("Test", (v) => { }));
    }

    [Test]
    public void Broadcast_UnregisteredEvent_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => EventCenter.Instance.Broadcast("NotExist"));
    }

    [Test]
    public void Clear_RemovesAllEvents()
    {
        int count = 0;
        EventCenter.Instance.Subscribe("Test", () => count++);
        EventCenter.Instance.Clear();
        EventCenter.Instance.Broadcast("Test");
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Broadcast_SingleArgNullReference_IsDelivered()
    {
        string received = "sentinel";
        EventCenter.Instance.Subscribe<string>("NullTest", (s) => received = s);
        EventCenter.Instance.Broadcast<string>("NullTest", null);
        Assert.IsNull(received);
    }

    [Test]
    public void Broadcast_AssignableType_IsDelivered()
    {
        object received = null;
        EventCenter.Instance.Subscribe<object>("CovTest", (o) => received = o);
        EventCenter.Instance.Broadcast("CovTest", "hello");
        Assert.AreEqual("hello", received);
    }
}
