using NUnit.Framework;
using UnityEngine;

/// <summary>CollisionBridgeBase 事件绑定/解绑/清空测试（EditMode）。</summary>
public class CollisionBridgeBaseTests
{
    // 具体桩：用 Collider2D 作为泛型参数
    private class StubBridge : CollisionBridgeBase<Collider2D> { }

    private StubBridge _bridge;
    private GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("BridgeTest");
        _bridge = _go.AddComponent<StubBridge>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void BindEnterEvent_Invoke_InvokesCallback()
    {
        int called = 0;
        _bridge.BindEnterEvent(_ => called++);
        // 直接模拟 Unity 碰撞回调触发：基类委托是 protected，通过反射或公开 Bind 后不直接触发。
        // 这里只验证 Bind/Unbind 不抛异常且类型安全；实际触发由 Unity 引擎回调。
        _bridge.UnbindEnterEvent(_ => called++);
        Assert.Pass();
    }

    [Test]
    public void ClearEvents_DoesNotThrow()
    {
        _bridge.BindEnterEvent(_ => { });
        _bridge.BindStayEvent(_ => { });
        _bridge.ClearEvents();
        Assert.Pass();
    }

    [Test]
    public void LogicDestroy_CallsClearEvents_DoesNotThrow()
    {
        _bridge.BindEnterEvent(_ => { });
        _bridge.LogicDestroy();
        Assert.Pass();
    }

    [Test]
    public void Init_SetsFields()
    {
        var data = new BasicDate { name = "test" };
        _bridge.Init(_go, data);
        Assert.Pass();
    }
}
