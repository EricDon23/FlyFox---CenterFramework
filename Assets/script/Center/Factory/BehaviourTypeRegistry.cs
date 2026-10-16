using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 行为类型预备站：仅负责「注册哪些类型可用 + 缓存 isBuild/isGlobal 标志 + 初始加载判定」。
/// 不构造对象、不持有实例。
/// </summary>
public class BehaviourTypeRegistry : BasicSingle<BehaviourTypeRegistry>
{
    private BehaviourTypeRegistry() { }

    // registerKey -> 行为类型
    private readonly Dictionary<string, Type> _behaviourTypes = new Dictionary<string, Type>();
    // 显式实例化开关（覆盖 isBuild 默认）
    private readonly Dictionary<string, bool> _instantiateFlags = new Dictionary<string, bool>();
    // 注册时缓存的 isBuild
    private readonly Dictionary<string, bool> _isBuildFlags = new Dictionary<string, bool>();
    // 注册时缓存的 isGlobal
    private readonly Dictionary<string, bool> _isGlobalFlags = new Dictionary<string, bool>();

    public void RegisterBehaviour<T>(string registerKey) where T : Objectbehavior, new()
    {
        _behaviourTypes[registerKey] = typeof(T);
        CacheFlags(registerKey, typeof(T));
    }

    public void RegisterBehaviour(Type behaviourType, string registerKey)
    {
        if (behaviourType == null)
        {
            Debug.LogWarning("[Registry] RegisterBehaviour 传入空类型，跳过");
            return;
        }
        _behaviourTypes[registerKey] = behaviourType;
        CacheFlags(registerKey, behaviourType);
    }

    private void CacheFlags(string registerKey, Type type)
    {
        object probe = null;
        try { probe = Activator.CreateInstance(type); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Registry] {type.Name} 无法临时探测标志：{e.Message}");
        }
        _isBuildFlags[registerKey] = ReadBoolField(probe, "isBuild");
        _isGlobalFlags[registerKey] = ReadBoolField(probe, "isGlobal");
    }

    private static bool ReadBoolField(object instance, string fieldName)
    {
        if (instance == null) return false;
        FieldInfo field = instance.GetType().GetField(
            fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(instance);
    }

    public void RemoveBehaviour(string registerKey)
    {
        _behaviourTypes.Remove(registerKey);
        _instantiateFlags.Remove(registerKey);
        _isBuildFlags.Remove(registerKey);
        _isGlobalFlags.Remove(registerKey);
    }

    public void SetInstantiateFlag(string registerKey, bool enabled)
    {
        _instantiateFlags[registerKey] = enabled;
    }

    public bool TryGetType(string registerKey, out Type behaviourType)
    {
        return _behaviourTypes.TryGetValue(registerKey, out behaviourType);
    }

    public bool Contains(string registerKey) => _behaviourTypes.ContainsKey(registerKey);

    public List<string> GetKeys() => new List<string>(_behaviourTypes.Keys);

    public bool TryGetInstantiateFlag(string registerKey, out bool flag)
        => _instantiateFlags.TryGetValue(registerKey, out flag);

    /// <summary>是否初始加载：显式开关优先，否则读缓存 isBuild。</summary>
    public bool IsInitialLoad(string registerKey)
    {
        if (_instantiateFlags.TryGetValue(registerKey, out bool flag)) return flag;
        return _isBuildFlags.TryGetValue(registerKey, out bool isBuild) && isBuild;
    }

    /// <summary>是否立即注入：显式开关优先，否则跟随缓存 isBuild。</summary>
    public bool ShouldInstantiate(string registerKey) => IsInitialLoad(registerKey);

    public bool IsGlobal(string registerKey)
        => _isGlobalFlags.TryGetValue(registerKey, out bool g) && g;
}
