using NUnit.Framework;
using UnityEngine;

/// <summary>BehaviourFactory 公共构造路径测试（EditMode）。
/// 注意：涉及 ResCenter.LoadSync 加载 Resources 预制体的路径需要真实资源，
/// 本测试只覆盖 CreateBehaviour&lt;T&gt; 这条不依赖 Resources 的公共路径。</summary>
public class BehaviourFactoryTests
{
    // 记录生命周期调用的桩行为
    private class SpyCreateBehavior : Objectbehavior
    {
        public bool InitCalled;
        public bool AwakeCalled;
        public bool DestroyCalled;
        public override void Init(GameObject obj, BasicDate objectDate) { base.Init(obj, objectDate); InitCalled = true; }
        public override void LogicAwake() { AwakeCalled = true; }
        public override void LogicDestroy() { DestroyCalled = true; }
    }

    private BehaviourFactory _factory;
    private BehaviourInstanceHub _hub;

    [SetUp]
    public void SetUp()
    {
        _factory = BehaviourFactory.Instance;
        _hub = BehaviourInstanceHub.Instance;
        _hub.ClearAll();
    }

    [TearDown]
    public void TearDown()
    {
        _hub.ClearAll();
    }

    [Test]
    public void CreateBehaviour_InvokesInitAndLogicAwake()
    {
        var go = new GameObject("factory_test");
        var behavior = _factory.CreateBehaviour<SpyCreateBehavior>(go, (BasicDate)null);

        Assert.IsNotNull(behavior);
        Assert.IsTrue(behavior.InitCalled);
        Assert.IsTrue(behavior.AwakeCalled);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CreateBehaviour_AddsToHubStack()
    {
        var go = new GameObject("factory_test_stack");
        var behavior = _factory.CreateBehaviour<SpyCreateBehavior>(go, (BasicDate)null);

        CollectionAssert.Contains(_hub.Behaviours, behavior);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HubDestroyAll_TriggersBehaviorLogicDestroy()
    {
        var go = new GameObject("factory_test_destroy");
        var behavior = _factory.CreateBehaviour<SpyCreateBehavior>(go, (BasicDate)null);

        _hub.DestroyAll();

        Assert.IsTrue(behavior.DestroyCalled);
        Object.DestroyImmediate(go);
    }
}
