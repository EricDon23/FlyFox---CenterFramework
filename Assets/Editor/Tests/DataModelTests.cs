using NUnit.Framework;
using UnityEngine;

/// <summary>BasicDate 子类得失函数与网格数据访问测试（EditMode）。</summary>
public class DataModelTests
{
    [Test]
    public void Player_Heal_ClampsToMaxHealth()
    {
        var player = new Player();
        player.Heal(100);
        Assert.AreEqual(player.MaxHealth, player.Health);
    }

    [Test]
    public void Player_ApplyDamage_ClampsToZero()
    {
        var player = new Player();
        player.ApplyDamage(999);
        Assert.AreEqual(0, player.Health);
    }

    [Test]
    public void Player_SetMaxHealth_ClampsToAtLeastOne()
    {
        var player = new Player();
        player.SetMaxHealth(0);
        Assert.AreEqual(1, player.MaxHealth);
    }

    [Test]
    public void Fruit_Heal_GetSet()
    {
        var fruit = new Fruit { Heal = 5 };
        Assert.AreEqual(5, fruit.Heal);
    }

    [Test]
    public void Score_Constructor_SetsBaseAndCurrent()
    {
        var score = new Score(7);
        Assert.AreEqual(7, score.BaseScore);
        Assert.AreEqual(7, score.CurrentScore);
    }

    [Test]
    public void GridCell_Constructor_SetsType()
    {
        var cell = new GridCell(GridType.Coin);
        Assert.AreEqual(GridType.Coin, cell.Type);
        Assert.IsNull(cell.Prefab);
    }

    [Test]
    public void SpawnPositionData_InitializeGrid_SetsDimensions()
    {
        var data = new SpawnPositionData();
        data.InitializeGrid(3, 4);
        Assert.AreEqual(3, data.gridWidth);
        Assert.AreEqual(4, data.gridHeight);
    }

    [Test]
    public void SpawnPositionData_IsValidGrid_Boundaries()
    {
        var data = new SpawnPositionData();
        data.InitializeGrid(3, 4);
        Assert.IsTrue(data.IsValidGrid(0, 0));
        Assert.IsTrue(data.IsValidGrid(2, 3));
        Assert.IsFalse(data.IsValidGrid(3, 0));
        Assert.IsFalse(data.IsValidGrid(0, 4));
        Assert.IsFalse(data.IsValidGrid(-1, 0));
    }

    [Test]
    public void SpawnPositionData_SetGridCell_OutOfRange_IsNoOp()
    {
        var data = new SpawnPositionData();
        data.InitializeGrid(3, 4);
        data.SetGridCell(99, 99, GridType.Gem);
        Assert.AreEqual(GridType.Empty, data.GetGridType(0, 0)); // 未受影响
        Assert.AreEqual(GridType.Empty, data.GetGridType(99, 99)); // 越界返回 Empty
    }

    [Test]
    public void SpawnPositionData_SetAndGetGridCell_RoundTrips()
    {
        var data = new SpawnPositionData();
        data.InitializeGrid(3, 4);
        var prefab = new GameObject("cellPrefab");
        data.SetGridCell(1, 2, GridType.Coin, prefab);
        Assert.AreEqual(GridType.Coin, data.GetGridType(1, 2));
        Assert.AreSame(prefab, data.GetGridPrefab(1, 2));
        Object.DestroyImmediate(prefab);
    }
}
