using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 配置表生成器（静态单例）：在指定路径生成 JSON 配置表。
/// - GenerateOne    单一生成：直接写入数据
/// - AppendRow      续写生成：校验类型一致后追加一行
/// - GenerateBatch  批量生成：集合数据一次性写入
/// </summary>
public static class ConfigTableGenerator
{
    private const string DEFAULT_DIR = "Assets/Resources/Config";

    /// <summary>单一生成：直接在指定路径写 JSON 文件。</summary>
    /// <param name="data">要序列化的数据实例</param>
    /// <param name="relativePath">相对路径（不含 .json 后缀），如 "uimanager"</param>
    public static void GenerateOne(object data, string relativePath)
    {
        string fullPath = ResolvePath(relativePath);
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[ConfigTableGenerator] 已生成：{fullPath}");
    }

    /// <summary>续写生成：追加一行数据到已有表，类型不一致则报错。</summary>
    /// <param name="data">要追加的数据实例</param>
    /// <param name="relativePath">已有 JSON 表路径</param>
    public static void AppendRow(object data, string relativePath)
    {
        string fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[ConfigTableGenerator] 文件不存在：{fullPath}");
            return;
        }

        string existing = File.ReadAllText(fullPath);

        // 反序列化已有数据，判断是否为列表结构
        Type dataType = data.GetType();

        // 如果已有 JSON 是 ListWrapper 结构（数组）
        if (existing.TrimStart().StartsWith("["))
        {
            AppendToArray(data, fullPath, existing);
        }
        else
        {
            // 单个对象：判断类型是否一致，不一致报错
            try
            {
                var existingObj = JsonUtility.FromJson(existing, dataType);
                if (existingObj == null)
                {
                    Debug.LogError($"[ConfigTableGenerator] 类型不匹配！期望 {dataType.Name}，但文件内容无法反序列化");
                    return;
                }
                // 单个对象无法追加，提示用户用批量模式
                Debug.LogError($"[ConfigTableGenerator] 目标是单个对象表，无法追加行。请用 GenerateBatch 初始化列表表");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigTableGenerator] 类型不匹配！此表无法写入 {dataType.Name}：{e.Message}");
            }
        }
    }

    /// <summary>批量生成：把集合里的同类型实例一次性写入 JSON 表。</summary>
    /// <param name="collection">List/数组/Stack/Queue 等集合</param>
    /// <param name="relativePath">输出路径</param>
    public static void GenerateBatch(IEnumerable collection, string relativePath)
    {
        string fullPath = ResolvePath(relativePath);
        var list = new List<object>();
        Type elementType = null;

        foreach (var item in collection)
        {
            if (item == null) continue;
            elementType ??= item.GetType();
            if (item.GetType() != elementType)
            {
                Debug.LogError($"[ConfigTableGenerator] 集合内类型不一致！期望 {elementType.Name}，遇到 {item.GetType().Name}");
                return;
            }
            list.Add(item);
        }

        if (list.Count == 0)
        {
            Debug.LogWarning("[ConfigTableGenerator] 集合为空，未生成文件");
            return;
        }

        // 用 JsonUtility 包装类序列化列表
        var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
        var wrapper = Activator.CreateInstance(wrapperType);
        var listProp = wrapperType.GetProperty("Items");
        var genericList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        foreach (var item in list) ((IList)genericList).Add(item);
        listProp.SetValue(wrapper, genericList);

        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[ConfigTableGenerator] 批量生成 {list.Count} 条 → {fullPath}");
    }

    // ---------------- 内部方法 ----------------

    private static void AppendToArray(object data, string fullPath, string existingJson)
    {
        // 读取已有数组
        Type elementType = data.GetType();
        var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);

        // JsonUtility 不支持直接数组，需要包装类；已有文件如果是裸数组，需要先包一层
        var wrapper = Activator.CreateInstance(wrapperType);
        try
        {
            wrapper = JsonUtility.FromJson(existingJson, wrapperType);
        }
        catch
        {
            Debug.LogError($"[ConfigTableGenerator] 类型不匹配！此表无法写入 {elementType.Name}");
            return;
        }

        var list = (IList)wrapperType.GetProperty("Items").GetValue(wrapper);
        list.Add(data);

        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[ConfigTableGenerator] 追加 1 条 → {fullPath}（共 {list.Count} 条）");
    }

    private static string ResolvePath(string relativePath)
    {
        string dir = DEFAULT_DIR;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, relativePath.EndsWith(".json") ? relativePath : relativePath + ".json");
    }
}

/// <summary>JsonUtility 列表包装类。</summary>
[Serializable]
public class ListWrapper<T>
{
    public List<T> Items;
}
