using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 编队形状定义：存储经典飞行游戏的物品生成阵型。
/// 每个形状返回一组局部偏移坐标，LevelBuildBehavior 负责实例化。
/// </summary>
public static class FormationLibrary
{
    /// <summary>获取所有可用形状及权重。</summary>
    public static List<FormationDef> GetAllFormations()
    {
        return new List<FormationDef>
        {
            new FormationDef { Name = "Horizontal", Weight = 30, Points = Horizontal(5, 0.9f) },
            new FormationDef { Name = "Vertical", Weight = 20, Points = Vertical(4, 0.9f) },
            new FormationDef { Name = "V", Weight = 20, Points = VShape(5, 0.9f, 1.2f) },
            new FormationDef { Name = "ReverseV", Weight = 10, Points = ReverseVShape(5, 0.9f, 1.2f) },
            new FormationDef { Name = "Square", Weight = 10, Points = Square(3, 0.9f) },
            new FormationDef { Name = "Diamond", Weight = 10, Points = Diamond(0.9f) },
        };
    }

    /// <summary>横排：y=0，x 从 0 开始递增。</summary>
    private static List<Vector2> Horizontal(int count, float spacing)
    {
        var list = new List<Vector2>();
        for (int i = 0; i < count; i++)
            list.Add(new Vector2(i * spacing, 0));
        return list;
    }

    /// <summary>竖列：x=0，y 居中对称分布。</summary>
    private static List<Vector2> Vertical(int count, float spacing)
    {
        var list = new List<Vector2>();
        float startY = -(count - 1) / 2f * spacing;
        for (int i = 0; i < count; i++)
            list.Add(new Vector2(0, startY + i * spacing));
        return list;
    }

    /// <summary>V字：中间低两边高。</summary>
    private static List<Vector2> VShape(int count, float spacingX, float height)
    {
        var list = new List<Vector2>();
        float center = (count - 1) / 2f;
        for (int i = 0; i < count; i++)
        {
            float x = i * spacingX;
            float y = Mathf.Abs(i - center) / center * height;
            list.Add(new Vector2(x, y));
        }
        return list;
    }

    /// <summary>倒V：中间高两边低。</summary>
    private static List<Vector2> ReverseVShape(int count, float spacingX, float height)
    {
        var list = new List<Vector2>();
        float center = (count - 1) / 2f;
        for (int i = 0; i < count; i++)
        {
            float x = i * spacingX;
            float y = height - Mathf.Abs(i - center) / center * height;
            list.Add(new Vector2(x, y));
        }
        return list;
    }

    /// <summary>矩形方阵。</summary>
    private static List<Vector2> Square(int size, float spacing)
    {
        var list = new List<Vector2>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                list.Add(new Vector2(x * spacing, y * spacing - 1f));
        return list;
    }

    /// <summary>菱形。</summary>
    private static List<Vector2> Diamond(float spacing)
    {
        var list = new List<Vector2>
        {
            new Vector2(0, spacing),
            new Vector2(-spacing, 0), new Vector2(0, 0), new Vector2(spacing, 0),
            new Vector2(0, -spacing),
        };
        return list;
    }
}

/// <summary>单个编队定义。</summary>
public class FormationDef
{
    public string Name;
    public int Weight;
    public List<Vector2> Points;
}
