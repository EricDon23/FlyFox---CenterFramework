using NUnit.Framework;

/// <summary>BasicSingle 反射式懒加载单例测试（EditMode）。</summary>
public class BasicSingleTests
{
    private class TestSingle : BasicSingle<TestSingle>
    {
        private TestSingle() { }
    }

    [Test]
    public void Instance_IsNonNull()
    {
        Assert.IsNotNull(TestSingle.Instance);
    }

    [Test]
    public void Instance_ReturnsSameReference()
    {
        var a = TestSingle.Instance;
        var b = TestSingle.Instance;
        Assert.AreSame(a, b);
    }
}
