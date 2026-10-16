using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>EventCenter 广播异常隔离测试（EditMode）：单个回调抛异常不中断其他监听器。</summary>
public class EventCenterErrorIsolationTests
{
    [SetUp]
    public void SetUp()
    {
        EventCenter.Instance.Clear();
        // EventCenter 捕获回调异常时会打 Error 日志，这是预期行为，抑制其导致测试失败
        LogAssert.ignoreFailingMessages = true;
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void Broadcast_OneCallbackThrows_OtherCallbacksStillInvoked()
    {
        int goodCount = 0;
        EventCenter.Instance.Subscribe("ErrTest", () => throw new InvalidOperationException("boom"));
        EventCenter.Instance.Subscribe("ErrTest", () => goodCount++);
        EventCenter.Instance.Subscribe("ErrTest", () => goodCount++);

        EventCenter.Instance.Broadcast("ErrTest");

        Assert.AreEqual(2, goodCount, "一个回调抛异常后，其余监听器应照常执行");
    }

    [Test]
    public void Broadcast_TypedCallbackThrows_OtherCallbacksStillInvoked()
    {
        int goodCount = 0;
        EventCenter.Instance.Subscribe<int>("ErrTyped", (v) => throw new InvalidOperationException("boom"));
        EventCenter.Instance.Subscribe<int>("ErrTyped", (v) => goodCount++);

        EventCenter.Instance.Broadcast("ErrTyped", 1);

        Assert.AreEqual(1, goodCount, "带参事件回调抛异常后，其余监听器应照常执行");
    }

    [Test]
    public void Broadcast_NullObjectArgument_IsDeliveredToObjectListener()
    {
        object received = new object();
        EventCenter.Instance.Subscribe<object>("NullObj", (o) => received = o);
        EventCenter.Instance.Broadcast<object>("NullObj", null);
        Assert.IsNull(received);
    }
}
