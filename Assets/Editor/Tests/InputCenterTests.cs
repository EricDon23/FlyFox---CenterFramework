using NUnit.Framework;
using UnityEngine;

/// <summary>InputCenter 映射与全局开关测试（EditMode）。
/// EditMode 下 Input.GetKeyDown/GetAxisRaw 返回默认值（false/0），
/// 本测试只验证映射查找与开关门控，不验证真实输入。</summary>
public class InputCenterTests
{
    private InputCenter _input;

    [SetUp]
    public void SetUp()
    {
        _input = InputCenter.Instance;
        _input.InputEnable = true;
    }

    [Test]
    public void GetKeyDown_UnmappedKey_ReturnsFalse()
    {
        Assert.IsFalse(_input.GetKeyDown("TestUnmappedKey_" + System.Guid.NewGuid().ToString("N")));
    }

    [Test]
    public void SetKeyMap_ThenRemoveKeyMap_BecomesUnmapped()
    {
        const string key = "Jump";
        _input.SetKeyMap(key, "Space");
        Assert.IsFalse(_input.GetKeyDown(key)); // EditMode 无真实输入
        _input.RemoveKeyMap(key);
        Assert.IsFalse(_input.GetKeyDown(key));
    }

    [Test]
    public void InputEnableFalse_ShortCircuits_BeforeMapLookup()
    {
        _input.InputEnable = false;
        Assert.IsFalse(_input.GetKeyDown("Whatever"));
        Assert.That(_input.GetAxisRaw("Whatever"), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void GetAxisRaw_UnmappedAxis_ReturnsZero()
    {
        Assert.That(_input.GetAxisRaw("NoSuchAxis"), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void SetAxisMap_ThenRemoveAxisMap_BecomesUnmapped()
    {
        const string axis = "TestHorizontal";
        _input.SetAxisMap(axis, "Horizontal");
        Assert.That(_input.GetAxisRaw(axis), Is.EqualTo(0f).Within(0.0001f));
        _input.RemoveAxisMap(axis);
        Assert.That(_input.GetAxisRaw(axis), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void GetMoveVector_ReturnsZeroWhenNoInput()
    {
        Vector2 v = _input.GetMoveVector();
        Assert.That(v.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(v.y, Is.EqualTo(0f).Within(0.0001f));
    }
}
