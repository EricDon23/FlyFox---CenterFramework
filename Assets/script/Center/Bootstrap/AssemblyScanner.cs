using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 程序集扫描注册器（基础设施，静态）。
/// 识别 [AutoRegisterBehaviour] 的行为类型注册进 FactoryCenter，
/// 识别 [AutoRegisterConfig] 的配置类型注册进 ConfigCenter；
/// 缺无参构造的类型告警跳过，不阻断启动。
/// </summary>
public static class AssemblyScanner
{
    /// <summary>扫描行为类型（Objectbehavior / CollisionBridgeBase 体系）注册进工厂预备站。</summary>
    public static void ScanBehaviours()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;

            AutoRegisterBehaviourAttribute attr =
                type.GetCustomAttribute<AutoRegisterBehaviourAttribute>();
            if (attr == null) continue;

            if (!IsBehaviourType(type)) continue;

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogWarning($"[AssemblyScanner] 行为类型 {type.Name} 缺少无参构造，跳过注册");
                continue;
            }

            FactoryCenter.Instance.RegisterBehaviour(type, attr.RegisterKey);
        }
    }

    /// <summary>扫描配置类型（BasicDate 子类）注册进 ConfigCenter。</summary>
    public static void ScanConfigs()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            if (!typeof(BasicDate).IsAssignableFrom(type)) continue;

            AutoRegisterConfigAttribute attr =
                type.GetCustomAttribute<AutoRegisterConfigAttribute>();
            if (attr == null) continue;

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogWarning($"[AssemblyScanner] 配置类型 {type.Name} 缺少无参构造，跳过注册");
                continue;
            }

            ConfigCenter.Instance.RegisterConfigType(type, attr.RegisterKey);
        }
    }

    /// <summary>判断类型是否属于行为体系（Objectbehavior 或实现契约的 MonoBehaviour）。</summary>
    private static bool IsBehaviourType(Type type)
    {
        if (typeof(Objectbehavior).IsAssignableFrom(type)) return true;

        if (typeof(BasicObjectBehaviorImp).IsAssignableFrom(type)
            && typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type))
        {
            return true;
        }
        return false;
    }
}
