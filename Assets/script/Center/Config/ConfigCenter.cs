using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置中心：从资源中心加载配置表(Json)，翻译成数据模型并存储到配置字典。
/// 流程：得到配置地址 -> 传输到 ResCenter 获得配置资源(Json) -> JsonDataUtil 翻译 -> 存入配置字典。
/// 同时维护「配置名 -> 默认数据工厂」注册表，作为未加载配置时的默认数据来源（BasicDate 子类默认值）。
/// 配置类型由程序集扫描 [AutoRegisterConfig] 自动注册，底层零硬编码。
/// </summary>
public class ConfigCenter : BasicSingle<ConfigCenter>
{
    // 存储数据模型的字典：配置名(name) -> 数据模型，字典键与数据的 name 相同
    public Dictionary<string, BasicDate> DateDic = new Dictionary<string, BasicDate>();

    // 配置名 -> 默认数据工厂（命中则创建对应 BasicDate 子类的默认实例）
    private readonly Dictionary<string, Func<BasicDate>> _defaultFactories = new Dictionary<string, Func<BasicDate>>();

    /// <summary>泛型注册（兼容手动注册），内部委托非泛型重载。</summary>
    public void RegisterConfigType<T>(string registerKey) where T : BasicDate, new()
    {
        RegisterConfigType(typeof(T), registerKey);
    }

    /// <summary>
    /// 非泛型注册（供程序集扫描直接传入 Type）：
    /// 注册配置名对应的具体 BasicDate 子类类型，用于「配置缺失时生成默认实例」。
    /// 类型非法或缺无参构造时告警跳过，不阻断启动。
    /// </summary>
    public void RegisterConfigType(Type configType, string registerKey)
    {
        if (configType == null)
        {
            Debug.LogWarning("[ConfigCenter] RegisterConfigType 传入空类型，跳过");
            return;
        }
        if (!typeof(BasicDate).IsAssignableFrom(configType))
        {
            Debug.LogWarning($"[ConfigCenter] {configType.Name} 不是 BasicDate 子类，跳过注册：{registerKey}");
            return;
        }
        if (configType.GetConstructor(Type.EmptyTypes) == null)
        {
            Debug.LogWarning($"[ConfigCenter] {configType.Name} 缺少无参构造，跳过注册：{registerKey}");
            return;
        }

        _defaultFactories[registerKey] = () => (BasicDate)Activator.CreateInstance(configType);
    }

    /// <summary>
    /// 创建默认数据实例：命中注册表返回对应 BasicDate 子类的默认实例，未注册回退裸 BasicDate。
    /// </summary>
    public BasicDate CreateDefault(string registerKey)
    {
        if (_defaultFactories.TryGetValue(registerKey, out Func<BasicDate> factory))
        {
            return factory();
        }
        return new BasicDate { index = 0, name = registerKey };
    }

    /// <summary>
    /// 从资源管理中心加载配置表，反序列化到具体数据类并存入配置字典（config-first 数据源）。
    /// 资源缺失时仅告警并回退默认值，不中断流程。
    /// </summary>
    public void LoadConfig<T>(string path) where T : BasicDate
    {
        // 1. 得到地址，把地址传输到 ResCenter，获得配置资源（Json）
        ResHandle handle = ResCenter.Instance.LoadSync<TextAsset>(path);
        if (handle == null || !(handle.AssetObj is TextAsset textAsset))
        {
            Debug.LogWarning($"[ConfigCenter] 未找到配置资源，回退默认值：{path}");
            return;
        }
        // 2. 取出 json 文本后立即释放句柄
        string json = textAsset.text;
        handle.Release();
        // 3. 使用 JsonDataUtil 把配置表翻译成具体数据类（多态）
        List<T> list = JsonDataUtil.ReadList<T>(json);
        // 4. 存入配置字典，字典键与数据的 name 相同
        for (int i = 0; i < list.Count; i++)
        {
            DateDic[list[i].name] = list[i];
        }
    }

    /// <summary>按名称获取单个配置数据，不存在返回 null。</summary>
    public BasicDate GetConfig(string registerKey)
    {
        return DateDic.TryGetValue(registerKey, out BasicDate data) ? data : null;
    }
}
