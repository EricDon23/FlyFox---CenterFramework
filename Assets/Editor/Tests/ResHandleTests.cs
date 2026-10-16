using NUnit.Framework;

/// <summary>ResHandle 引用计数句柄测试（EditMode）。</summary>
public class ResHandleTests
{
    [Test]
    public void Release_InvokesCallbackOnce()
    {
        int releaseCount = 0;
        var handle = new ResHandle("path", null, h => releaseCount++);

        handle.Release();
        handle.Release(); // 幂等：重复释放被拦截

        Assert.IsTrue(handle.IsReleased);
        Assert.AreEqual(1, releaseCount);
    }

    [Test]
    public void NewHandle_IsNotReleased()
    {
        var handle = new ResHandle("path", null, h => { });
        Assert.IsFalse(handle.IsReleased);
        Assert.AreEqual("path", handle.AssetPath);
        Assert.IsNull(handle.AssetObj);
    }
}
