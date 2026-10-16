using NUnit.Framework;

/// <summary>AudioVolumeHelper 音量转分贝测试（EditMode）。</summary>
public class AudioVolumeHelperTests
{
    [Test]
    public void VolumeToDb_MinVolume_ReturnsNegativeEighty()
    {
        Assert.AreEqual(-80f, AudioVolumeHelper.VolumeToDb(0f), 0.01f);
    }

    [Test]
    public void VolumeToDb_MaxVolume_ReturnsZero()
    {
        Assert.AreEqual(0f, AudioVolumeHelper.VolumeToDb(1f), 0.01f);
    }

    [Test]
    public void VolumeToDb_HalfVolume_ReturnsApproxMinusSix()
    {
        // 20*log10(0.5) ≈ -6.02 dB
        Assert.AreEqual(-6.02f, AudioVolumeHelper.VolumeToDb(0.5f), 0.1f);
    }

    [Test]
    public void VolumeToDb_NegativeInput_ClampedToZero()
    {
        // Clamp01 把负值夹到 0
        Assert.AreEqual(-80f, AudioVolumeHelper.VolumeToDb(-1f), 0.01f);
    }

    [Test]
    public void VolumeToDb_AboveOne_ClampedToOne()
    {
        Assert.AreEqual(0f, AudioVolumeHelper.VolumeToDb(2f), 0.01f);
    }
}
