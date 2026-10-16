using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏对象池，单例，基于Stack后进先出
/// </summary>
public class AssetPool : BasicSingle<AssetPool>
{
    // 修正：字典Key：预制体名称，Value：存储GameObject的栈
    private Dictionary<string, Stack<GameObject>> poolDictionary = new Dictionary<string, Stack<GameObject>>();

    // 默认栈初始容量
    private readonly int defaultCapacity = 5;

    /// <summary>
    /// 初始化对象池，预生成若干物体存入池内
    /// </summary>
    /// <param name="prefabName">池标识名称</param>
    /// <param name="prefab">预制体资源</param>
    /// <param name="capacity">预创建数量，默认5</param>
    public void InitPool(string prefabName, GameObject prefab, int capacity = 5)
    {
        if (prefab == null)
        {
            Debug.LogError($"InitPool: 预制体为空 {prefabName}");
            return;
        }
        if (poolDictionary.ContainsKey(prefabName))
        {
            Debug.LogWarning($"InitPool: 对象池已存在 {prefabName}");
            return;
        }

        Stack<GameObject> stack = new Stack<GameObject>(capacity);
        poolDictionary[prefabName] = stack;

        // 预先实例化指定数量对象
        for (int i = 0; i < capacity; i++)
        {
            GameObject go = Object.Instantiate(prefab);
            go.name = prefabName;
            go.SetActive(false);
            stack.Push(go);
        }
    }

    /// <summary>
    /// 取出对象
    /// </summary>
    /// <param name="prefabName">池名称</param>
    /// <returns>取出的GameObject</returns>
    public GameObject GetObject(string prefabName, GameObject prefab = null)
    {
        // 池子存在并且有闲置物体
        if (poolDictionary.TryGetValue(prefabName, out Stack<GameObject> stack) && stack.Count > 0)
        {
            GameObject go = stack.Pop();
            go.SetActive(true);
            return go;
        }

        // 池子无闲置对象，提供预制体就自动实例化扩容
        if (prefab != null)
        {
            GameObject newGo = Object.Instantiate(prefab);
            newGo.name = prefabName;
            newGo.SetActive(true);
            return newGo;
        }

        Debug.LogWarning($"对象池中没有可用对象，且未传入预制体：{prefabName}");
        return null;
    }

    /// <summary>
    /// 回收物体回池子
    /// </summary>
    /// <param name="prefabName">池标识</param>
    /// <param name="go">回收物体</param>
    /// <param name="maxPoolCount">池子最大上限，超过直接销毁</param>
    public void PushGameObject(string prefabName, GameObject go, int maxPoolCount = 20)
    {
        if (go == null)
        {
            Debug.LogWarning($"PushGameObject:物体为空 {prefabName}");
            return;
        }

        // 如果池子不存在，新建栈
        if (!poolDictionary.TryGetValue(prefabName, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>(defaultCapacity);
            poolDictionary.Add(prefabName, stack);
        }

        // 超过池子最大容量，直接销毁，不进栈，防止内存暴涨
        if (stack.Count >= maxPoolCount)
        {
            Object.Destroy(go);
            return;
        }

        go.SetActive(false);
        go.name = prefabName;
        stack.Push(go);
    }

    /// <summary>
    /// 清空单个类型对象池，销毁所有物体
    /// </summary>
    public void ClearPool(string prefabName)
    {
        if (poolDictionary.TryGetValue(prefabName, out Stack<GameObject> stack))
        {
            while (stack.Count > 0)
            {
                GameObject go = stack.Pop();
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
            poolDictionary.Remove(prefabName);
        }
    }

    /// <summary>
    /// 清空全部对象池（场景切换调用）
    /// </summary>
    public void ClearAllPool()
    {
        foreach (var kv in poolDictionary)
        {
            Stack<GameObject> stack = kv.Value;
            while (stack.Count > 0)
            {
                GameObject go = stack.Pop();
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }
        poolDictionary.Clear();
    }
}
