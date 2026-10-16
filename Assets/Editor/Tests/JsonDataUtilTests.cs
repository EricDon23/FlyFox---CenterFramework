using System.Collections.Generic;
using NUnit.Framework;

/// <summary>JsonDataUtil 序列化/反序列化测试（EditMode）。</summary>
public class JsonDataUtilTests
{
    [Test]
    public void ReadOne_ValidJson_Deserializes()
    {
        JsonTestData data = JsonDataUtil.ReadOne<JsonTestData>("{\"index\":3,\"name\":\"abc\"}");
        Assert.IsNotNull(data);
        Assert.AreEqual(3, data.index);
        Assert.AreEqual("abc", data.name);
    }

    [Test]
    public void WrapToJson_ThenReadOne_RoundTrips()
    {
        var src = new JsonTestData { index = 7, name = "x" };
        string json = JsonDataUtil.WrapToJson(src);
        JsonTestData dst = JsonDataUtil.ReadOne<JsonTestData>(json);
        Assert.AreEqual(7, dst.index);
        Assert.AreEqual("x", dst.name);
    }

    [Test]
    public void ReadList_ArrayJson_ReturnsList()
    {
        List<JsonTestData> list = JsonDataUtil.ReadList<JsonTestData>(
            "[{\"index\":1,\"name\":\"a\"},{\"index\":2,\"name\":\"b\"}]");
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("a", list[0].name);
        Assert.AreEqual("b", list[1].name);
    }

    [Test]
    public void ReadList_EmptyJson_ReturnsEmptyList()
    {
        List<JsonTestData> list = JsonDataUtil.ReadList<JsonTestData>("");
        Assert.IsNotNull(list);
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public void WrapListToJson_ReturnsArrayForm()
    {
        var list = new List<JsonTestData>
        {
            new JsonTestData { index = 1, name = "a" },
            new JsonTestData { index = 2, name = "b" },
        };
        string json = JsonDataUtil.WrapListToJson(list);
        Assert.IsTrue(json.TrimStart().StartsWith("["));
        Assert.IsTrue(json.TrimEnd().EndsWith("]"));
    }
}

[System.Serializable]
public class JsonTestData
{
    public int index;
    public string name;
}
