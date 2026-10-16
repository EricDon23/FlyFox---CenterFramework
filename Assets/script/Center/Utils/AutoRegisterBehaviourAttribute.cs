using System;

/// <summary>
/// 行为自动注册标记：打在 Objectbehavior 子类 / CollisionBridgeBase 子类上。
/// registerKey 同时关联「行为类型 / ConfigCenter配置名 / 预制体资源名」三者，
/// 由 GlobalConter 程序集扫描自动注册进 FactoryCenter，底层无需手写 RegisterBehaviour。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AutoRegisterBehaviourAttribute : Attribute
{
    /// <summary>注册键：行为类型、配置名、预制体名三者统一对应。</summary>
    public string RegisterKey { get; }

    public AutoRegisterBehaviourAttribute(string registerKey)
    {
        RegisterKey = registerKey;
    }
}
