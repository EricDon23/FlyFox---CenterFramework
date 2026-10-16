using System;
using System.Collections.Generic;
using UnityEngine;

///<summary>
/// 支持单一实例读取
/// 多实例读取
/// 支持把现有的数据包装成json
/// 是序列化和反序列化的工具
/// </summary>
public static class JsonDataUtil
{
    /// <summary>
    /// 列表包装器：Unity 的 JsonUtility 不支持顶层数组，
    /// 需要套一层对象，字段名 Items 对应数组内容
    /// </summary>
    [Serializable]
    private class ListWrapper<T>
    {
        public List<T> Items;
    }

    /// <summary>
    /// 读取一个数据并返回（反序列化单个对象）
    /// </summary>
    /// <typeparam name="T">数据模型类型</typeparam>
    /// <param name="json">json文本</param>
    /// <returns>数据模型实例，失败返回 default(T)</returns>
    public static T ReadOne<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// 读取整个表并返回（反序列化 json 数组，返回列表）
    /// 自动把顶层数组包装成对象，兼容 JsonUtility
    /// </summary>
    /// <typeparam name="T">数据模型类型</typeparam>
    /// <param name="json">json数组文本，形如 [ {...}, {...} ]</param>
    /// <returns>数据模型列表</returns>
    public static List<T> ReadList<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new List<T>();
        }

        string wrapped = "{\"Items\":" + json + "}";
        ListWrapper<T> wrapper = JsonUtility.FromJson<ListWrapper<T>>(wrapped);
        return wrapper == null || wrapper.Items == null ? new List<T>() : wrapper.Items;
    }

    /// <summary>
    /// 把单个数据包装成 json
    /// </summary>
    /// <typeparam name="T">数据模型类型</typeparam>
    /// <param name="data">数据模型实例</param>
    /// <returns>json文本</returns>
    public static string WrapToJson<T>(T data)
    {
        return JsonUtility.ToJson(data);
    }

    /// <summary>
    /// 把整个列表包装成 json（输出数组格式）
    /// </summary>
    /// <typeparam name="T">数据模型类型</typeparam>
    /// <param name="list">数据模型列表</param>
    /// <returns>json数组文本</returns>
    public static string WrapListToJson<T>(IList<T> list)
    {
        if (list == null)
        {
            return "[]";
        }

        string wrapped = JsonUtility.ToJson(new ListWrapper<T> { Items = new List<T>(list) });
        int start = wrapped.IndexOf('[');
        int end = wrapped.LastIndexOf(']');
        return start >= 0 && end > start ? wrapped.Substring(start, end - start + 1) : wrapped;
    }
}
