using System;

/// <summary>
/// 配置自动注册标记：打在 BasicDate 子类上。
/// registerKey 与行为的 registerKey 对齐，由 GlobalConter 程序集扫描自动注册进 ConfigCenter。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AutoRegisterConfigAttribute : Attribute
{
    /// <summary>注册键：与对应行为/预制体的 registerKey 保持一致。</summary>
    public string RegisterKey { get; }

    public AutoRegisterConfigAttribute(string registerKey)
    {
        RegisterKey = registerKey;
    }
}
