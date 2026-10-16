using NUnit.Framework;

/// <summary>ScoreCalculator 分段加权结算测试（EditMode）。</summary>
public class ScoreCalculatorTests
{
    [Test]
    public void Calculate_Under30s_WeightIsOne()
    {
        int result = ScoreCalculator.Calculate(100, 10f, out float bonus);
        Assert.AreEqual(100, result);
        Assert.AreEqual(0f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_Exactly30s_WeightIsOne()
    {
        int result = ScoreCalculator.Calculate(100, 30f, out float bonus);
        Assert.AreEqual(100, result);
        Assert.AreEqual(0f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_At60s_WeightIsOnePointFive()
    {
        int result = ScoreCalculator.Calculate(100, 60f, out float bonus);
        Assert.AreEqual(150, result);
        Assert.AreEqual(50f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_At120s_WeightIsTwo()
    {
        int result = ScoreCalculator.Calculate(100, 120f, out float bonus);
        Assert.AreEqual(200, result);
        Assert.AreEqual(100f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_Over120s_AddsHalfWeightPerExtraMinute()
    {
        // 180s = 2.0 + floor(60/60)*0.5 = 2.5
        int result = ScoreCalculator.Calculate(100, 180f, out float bonus);
        Assert.AreEqual(250, result);
        Assert.AreEqual(150f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_BaseScoreZero_ReturnsZero()
    {
        int result = ScoreCalculator.Calculate(0, 300f, out float bonus);
        Assert.AreEqual(0, result);
        Assert.AreEqual(0f, bonus, 0.001f);
    }

    [Test]
    public void Calculate_NegativeDuration_ClampedToWeightOne()
    {
        int result = ScoreCalculator.Calculate(100, -5f, out _);
        Assert.AreEqual(100, result);
    }
}
