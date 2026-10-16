using NUnit.Framework;

/// <summary>ConfigCenter 配置注册与默认实例测试（EditMode）。</summary>
public class ConfigCenterTests
{
    private class TestConfigData : BasicDate { }

    [Test]
    public void RegisterConfigType_ThenCreateDefault_ReturnsTypedInstance()
    {
        ConfigCenter.Instance.RegisterConfigType<TestConfigData>("ConfigCenterTestData");
        BasicDate data = ConfigCenter.Instance.CreateDefault("ConfigCenterTestData");
        Assert.IsInstanceOf<TestConfigData>(data);
    }

    [Test]
    public void CreateDefault_UnknownName_ReturnsBareBasicDateWithName()
    {
        const string name = "SomeUnknownConfig";
        BasicDate data = ConfigCenter.Instance.CreateDefault(name);
        Assert.IsNotNull(data);
        Assert.IsNotInstanceOf<TestConfigData>(data);
        Assert.AreEqual(name, data.name);
    }

    [Test]
    public void GetConfig_UnknownName_ReturnsNull()
    {
        Assert.IsNull(ConfigCenter.Instance.GetConfig("NeverRegistered_SomeUnknownConfig"));
    }
}
