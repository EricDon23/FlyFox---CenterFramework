using NUnit.Framework;

/// <summary>AssemblyScanner 程序集扫描连通性测试（EditMode）。
/// 注意：ScanBehaviours/ScanConfigs 扫描的是 Runtime 程序集（Assembly-CSharp），
/// 测试桩类位于 Editor 程序集，不会被扫描到；本测试验证扫描器本身不抛异常、
/// 对已打特性的业务类型连通到 Registry/ConfigCenter。</summary>
public class AssemblyScannerTests
{
    [SetUp]
    public void SetUp()
    {
        EventCenter.Instance.Clear();
    }

    [Test]
    public void ScanBehaviours_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => AssemblyScanner.ScanBehaviours());
    }

    [Test]
    public void ScanConfigs_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => AssemblyScanner.ScanConfigs());
    }

    [Test]
    public void ScanBehaviours_IsIdempotent_RepeatedCallDoesNotThrow()
    {
        // 重复扫描应幂等（Dictionary 覆盖写），不抛异常
        Assert.DoesNotThrow(() =>
        {
            AssemblyScanner.ScanBehaviours();
            AssemblyScanner.ScanBehaviours();
        });
    }
}
