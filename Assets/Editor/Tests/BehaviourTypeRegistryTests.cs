using NUnit.Framework;

/// <summary>BehaviourTypeRegistry 类型注册与标志缓存测试（EditMode）。</summary>
public class BehaviourTypeRegistryTests
{
    // 测试用桩行为：isBuild=false, isGlobal=false
    private class StubNormalBehavior : Objectbehavior { }

    // 测试用桩行为：isBuild=true
    private class StubBuildBehavior : Objectbehavior { public StubBuildBehavior() { isBuild = true; } }

    // 测试用桩行为：isGlobal=true
    private class StubGlobalBehavior : Objectbehavior { public StubGlobalBehavior() { isGlobal = true; } }

    private BehaviourTypeRegistry _registry;
    private string _key;

    [SetUp]
    public void SetUp()
    {
        _registry = BehaviourTypeRegistry.Instance;
        _key = "Test_Reg_" + System.Guid.NewGuid().ToString("N");
    }

    [TearDown]
    public void TearDown()
    {
        _registry.RemoveBehaviour(_key);
    }

    [Test]
    public void RegisterBehaviour_ThenTryGetType_ReturnsSameType()
    {
        _registry.RegisterBehaviour<StubNormalBehavior>(_key);
        Assert.IsTrue(_registry.TryGetType(_key, out System.Type type));
        Assert.AreEqual(typeof(StubNormalBehavior), type);
    }

    [Test]
    public void RegisterBehaviour_ReadsIsBuildFlagFromCtor()
    {
        _registry.RegisterBehaviour<StubBuildBehavior>(_key);
        Assert.IsTrue(_registry.IsInitialLoad(_key));
    }

    [Test]
    public void RegisterBehaviour_DefaultIsBuildFalse()
    {
        _registry.RegisterBehaviour<StubNormalBehavior>(_key);
        Assert.IsFalse(_registry.IsInitialLoad(_key));
    }

    [Test]
    public void RegisterBehaviour_ReadsIsGlobalFlag()
    {
        _registry.RegisterBehaviour<StubGlobalBehavior>(_key);
        Assert.IsTrue(_registry.IsGlobal(_key));
    }

    [Test]
    public void SetInstantiateFlag_OverridesIsBuildDefault()
    {
        _registry.RegisterBehaviour<StubNormalBehavior>(_key);
        Assert.IsFalse(_registry.IsInitialLoad(_key));
        _registry.SetInstantiateFlag(_key, true);
        Assert.IsTrue(_registry.IsInitialLoad(_key));
    }

    [Test]
    public void RemoveBehaviour_ClearsRegistration()
    {
        _registry.RegisterBehaviour<StubNormalBehavior>(_key);
        Assert.IsTrue(_registry.Contains(_key));
        _registry.RemoveBehaviour(_key);
        Assert.IsFalse(_registry.Contains(_key));
        Assert.IsFalse(_registry.TryGetType(_key, out _));
    }

    [Test]
    public void TryGetType_UnknownKey_ReturnsFalse()
    {
        Assert.IsFalse(_registry.TryGetType("Test_NeverRegistered_" + System.Guid.NewGuid().ToString("N"), out _));
    }

    [Test]
    public void GetKeys_ContainsRegisteredKey()
    {
        _registry.RegisterBehaviour<StubNormalBehavior>(_key);
        CollectionAssert.Contains(_registry.GetKeys(), _key);
    }
}
