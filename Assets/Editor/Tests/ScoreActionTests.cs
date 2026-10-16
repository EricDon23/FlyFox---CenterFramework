using System;
using NUnit.Framework;

/// <summary>ScoreAction 得分/治疗逻辑测试（EditMode）。</summary>
public class ScoreActionTests
{
    [SetUp]
    public void SetUp()
    {
        Global.Level = 0;
        Global.SessionBaseScore = 0;
    }

    [Test]
    public void Constructor_NullScore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ScoreAction(null));
    }

    [Test]
    public void DevelopScore_LevelZero_WeightOne()
    {
        var score = new Score(5);
        var action = new ScoreAction(score);
        action.DevelopScore();
        Assert.AreEqual(5, score.CurrentScore);
    }

    [Test]
    public void DevelopScore_HigherLevel_IncreasesWeight()
    {
        Global.Level = 4;
        var score = new Score(100);
        var action = new ScoreAction(score);
        action.DevelopScore();
        // weight = 1 + 4*0.25 = 2
        Assert.AreEqual(200, score.CurrentScore);
    }

    [Test]
    public void GetHealAmount_NonFruit_IsZero()
    {
        var action = new ScoreAction(new Score(5), 0);
        Assert.AreEqual(0, action.GetHealAmount());
    }

    [Test]
    public void GetHealAmount_Fruit_ReturnsHeal()
    {
        var fruit = new Fruit();
        var action = new ScoreAction(fruit, 2);
        Assert.AreEqual(2, action.GetHealAmount());
    }

    [Test]
    public void GetCurrentScore_NonFruit_AddsScore()
    {
        var player = new Player();
        var score = new Score(10);
        var action = new ScoreAction(score);
        action.DevelopScore();

        bool added = action.GetCurrentScore(player);
        Assert.IsTrue(added);
        Assert.AreEqual(10, Global.SessionBaseScore);
    }
}
