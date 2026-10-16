using NUnit.Framework;
using UnityEngine;

/// <summary>MathOfGame 纯数学工具测试（EditMode）。</summary>
public class MathOfGameTests
{
    [Test]
    public void DistanceSqr3D_ReturnsSquaredMagnitude()
    {
        Assert.AreEqual(4f, MathOfGame.DistanceSqr(new Vector3(1, 0, 0), new Vector3(3, 0, 0)), 1e-5f);
    }

    [Test]
    public void DistanceSqr2D_ReturnsSquaredMagnitude()
    {
        Assert.AreEqual(25f, MathOfGame.DistanceSqr(Vector2.zero, new Vector2(3, 4)), 1e-5f);
    }

    [Test]
    public void Cross_RightCrossUp_IsOne()
    {
        Assert.AreEqual(1f, MathOfGame.Cross(Vector2.right, Vector2.up), 1e-5f);
    }

    [Test]
    public void GetAngle3D_RightVsUp_Is90Degrees()
    {
        Assert.AreEqual(90f, MathOfGame.GetAngle(Vector3.right, Vector3.up), 0.01f);
    }

    [Test]
    public void GetAngle2D_RightVsRight_IsZero()
    {
        Assert.AreEqual(0f, MathOfGame.GetAngle(Vector2.right, Vector2.right), 0.01f);
    }

    [Test]
    public void Normalize_ZeroVector_ReturnsZero()
    {
        Assert.AreEqual(Vector3.zero, MathOfGame.Normalize(Vector3.zero));
    }

    [Test]
    public void Normalize_NonZero_ReturnsUnitVector()
    {
        Vector3 n = MathOfGame.Normalize(new Vector3(3, 0, 0));
        Assert.AreEqual(1f, n.magnitude, 1e-5f);
        Assert.AreEqual(1f, n.x, 1e-5f);
    }

    [Test]
    public void Project_ProjectsOntoAxis()
    {
        Vector2 result = MathOfGame.Project(Vector2.right, new Vector2(2, 2));
        Assert.AreEqual(2f, result.x, 1e-5f);
        Assert.AreEqual(0f, result.y, 1e-5f);
    }

    [Test]
    public void GetSignedAngle_RightToUp_IsPositive90()
    {
        Assert.AreEqual(90f, MathOfGame.GetSignedAngle(Vector2.right, Vector2.up), 0.01f);
    }

    [Test]
    public void RepeatAngle_Negative_WrapsToPositive()
    {
        Assert.AreEqual(270f, MathOfGame.RepeatAngle(-90f), 1e-5f);
    }

    [Test]
    public void RepeatAngle_Over360_Wraps()
    {
        Assert.AreEqual(90f, MathOfGame.RepeatAngle(450f), 1e-5f);
    }
}
