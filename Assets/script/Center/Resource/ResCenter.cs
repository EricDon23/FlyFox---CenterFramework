using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源中心入口，对外统一API，内部按路径前缀路由到对应存储后端
/// 作为全局唯一访问入口，隔离上层业务与底层模块实现
/// 上层无需感知具体存储模式（Resources / AssetBundle / Web / Addressable / Editor）
/// </summary>
public class ResCenter : BasicSingle<ResCenter>
{
    /// <summary>路径前缀路由枚举</summary>
    private enum ResScheme
    {
        Resources,
        AssetBundle,
        Web,
        Addressable,
    }

    /// <summary>Resources目录资源模块</summary>
    public ResourceModule ResourceModule { get; private set; }
    /// <summary>AssetBundle资源模块</summary>
    public AssetBundleModule AssetBundle { get; private set; }
    /// <summary>Addressables资源模块</summary>
    public AddressableModule Addressable { get; private set; }
    /// <summary>网络资源模块（UnityWebRequest）</summary>
    public UnityWebRequestModule Web { get; private set; }
#if UNITY_EDITOR
    /// <summary>编辑器资源工具模块（仅编辑器环境可用）</summary>
    public EditorModule Editor { get; private set; }
#endif

    /// <summary>构造资源中心，实例化全部存储模块</summary>
    public ResCenter()
    {
        ResourceModule = new ResourceModule();
        AssetBundle = new AssetBundleModule();
        Addressable = new AddressableModule();
        Web = new UnityWebRequestModule();
#if UNITY_EDITOR
        Editor = new EditorModule();
#endif
    }

    /// <summary>
    /// 统一初始化入口，转发到需要初始化的模块（当前为Addressables）
    /// </summary>
    /// <param name="onComplete">初始化完成回调</param>
    public void Initialize(Action onComplete = null)
    {
        Addressable.Initialize(onComplete);
    }

    // ------------------------------------------------------------------
    // 统一加载入口：按路径前缀路由
    // res://path                 -> Resources
    // ab://bundlePath#assetName  -> AssetBundle
    // web://url / http(s)://url  -> Web（仅异步）
    // addr://key                 -> Addressable
    // 无前缀                      -> 默认 Resources
    // ------------------------------------------------------------------

    /// <summary>
    /// 同步加载资源，按路径前缀路由到对应模块
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">带前缀的资源路径</param>
    /// <returns>资源句柄，加载失败返回null；用完必须调用Release()</returns>
    public ResHandle LoadSync<T>(string path) where T : UnityEngine.Object
    {
        if (!TryParseScheme(path, out var scheme, out var payload))
        {
            Debug.LogError($"[ResCenter] 无效资源路径：{path}");
            return null;
        }

        switch (scheme)
        {
            case ResScheme.Resources:
                return ResourceModule.LoadSync<T>(payload);

            case ResScheme.AssetBundle:
            {
                int idx = payload.IndexOf('#');
                if (idx <= 0 || idx == payload.Length - 1)
                {
                    Debug.LogError($"[ResCenter] AssetBundle路径格式错误，应为 ab://bundlePath#assetName：{path}");
                    return null;
                }
                string bundlePath = payload.Substring(0, idx);
                string assetName = payload.Substring(idx + 1);
                if (!AssetBundle.LoadBundleSync(bundlePath))
                    return null;
                return AssetBundle.LoadAssetSync<T>(bundlePath, assetName);
            }

            case ResScheme.Addressable:
                Addressable.EnsureInitialized();
                return Addressable.LoadAssetSync<T>(payload);

            case ResScheme.Web:
                Debug.LogError("[ResCenter] Web资源无法同步加载，请使用LoadAsync");
                return null;
        }

        return null;
    }

    /// <summary>
    /// 异步加载资源，按路径前缀路由到对应模块
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">带前缀的资源路径</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadAsync<T>(string path, Action<ResHandle> onComplete) where T : UnityEngine.Object
    {
        if (!TryParseScheme(path, out var scheme, out var payload))
        {
            Debug.LogError($"[ResCenter] 无效资源路径：{path}");
            onComplete?.Invoke(null);
            return;
        }

        switch (scheme)
        {
            case ResScheme.Resources:
                ResourceModule.LoadAsync<T>(payload, onComplete);
                break;

            case ResScheme.AssetBundle:
                // AB无原生异步加载，退化为同步后立即回调
                ResHandle handle = LoadSync<T>(path);
                onComplete?.Invoke(handle);
                break;

            case ResScheme.Addressable:
                Addressable.Initialize(() => Addressable.LoadAssetAsync<T>(payload, onComplete));
                break;

            case ResScheme.Web:
                LoadWebAsync<T>(payload, onComplete);
                break;
        }
    }

    /// <summary>
    /// 清空全部资源台账并触发引擎资源卸载（覆盖所有运行时模块）
    /// 一般用于场景切换时整体清理
    /// </summary>
    public void ClearAll()
    {
        ResourceModule.ClearAll();
        AssetBundle.ClearAll();
        Addressable.ClearAll();
        Web.ClearAll();
    }

    // ------------------------------------------------------------------
    // Resources 专属 API（语义无其它后端对应物）
    // ------------------------------------------------------------------

    /// <summary>预注册未加载资源，仅登记台账，不加载资源</summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">Resources路径</param>
    /// <returns>注册成功true</returns>
    public bool RegisterUnloadedResources<T>(string path) where T : UnityEngine.Object
    {
        return ResourceModule.RegisterUnloaded<T>(path);
    }

    /// <summary>批量加载文件夹下所有Resources资源</summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="folderPath">文件夹路径</param>
    /// <returns>句柄列表</returns>
    public List<ResHandle> AllLoadResources<T>(string folderPath) where T : UnityEngine.Object
    {
        return ResourceModule.AllLoad<T>(folderPath);
    }

    /// <summary>缓存内按路径查找已加载Resources资源</summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="assetPath">资源路径</param>
    /// <returns>资源句柄</returns>
    public ResHandle FindResources<T>(string assetPath) where T : UnityEngine.Object
    {
        return ResourceModule.Find<T>(assetPath);
    }

    /// <summary>全局查找内存所有T类型对象，不生成句柄，不修改引用计数</summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>对象列表</returns>
    public List<T> FindObjectsOfTypeAll<T>() where T : UnityEngine.Object
    {
        return ResourceModule.FindObjectsOfTypeAll<T>();
    }

    /// <summary>全局查找，筛选本模块托管资源，返回句柄，引用计数+1</summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>句柄列表</returns>
    public List<ResHandle> FindObjectsOfTypeAllInCache<T>() where T : UnityEngine.Object
    {
        return ResourceModule.FindObjectsOfTypeAllInCache<T>();
    }

    /// <summary>获取资源当前引用计数</summary>
    /// <param name="path">资源路径</param>
    /// <returns>引用计数</returns>
    public int GetResourceRefCount(string path)
    {
        return ResourceModule.GetRefCount(path);
    }

    /// <summary>查询资源是否加载完成</summary>
    /// <param name="path">资源路径</param>
    /// <returns>是否加载</returns>
    public bool CheckAssetLoaded(string path)
    {
        return ResourceModule.IsAssetLoaded(path);
    }

    // ------------------------------------------------------------------
    // 内部实现
    // ------------------------------------------------------------------

    /// <summary>解析路径前缀，识别存储后端并剥离出真实负载</summary>
    private static bool TryParseScheme(string path, out ResScheme scheme, out string payload)
    {
        scheme = ResScheme.Resources;
        payload = path;

        if (string.IsNullOrEmpty(path))
            return false;

        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            scheme = ResScheme.Resources;
            payload = path.Substring("res://".Length);
        }
        else if (path.StartsWith("ab://", StringComparison.Ordinal))
        {
            scheme = ResScheme.AssetBundle;
            payload = path.Substring("ab://".Length);
        }
        else if (path.StartsWith("web://", StringComparison.Ordinal))
        {
            scheme = ResScheme.Web;
            payload = path.Substring("web://".Length);
        }
        else if (path.StartsWith("http://", StringComparison.Ordinal) || path.StartsWith("https://", StringComparison.Ordinal))
        {
            scheme = ResScheme.Web;
            payload = path;
        }
        else if (path.StartsWith("addr://", StringComparison.Ordinal))
        {
            scheme = ResScheme.Addressable;
            payload = path.Substring("addr://".Length);
        }
        // 无前缀 -> 默认 Resources

        return true;
    }

    /// <summary>按T类型分发到Web模块对应加载方法</summary>
    private void LoadWebAsync<T>(string url, Action<ResHandle> onComplete) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(TextAsset))
        {
            Web.LoadTextAsync(url, onComplete);
        }
        else if (typeof(T) == typeof(Texture2D))
        {
            Web.LoadTextureAsync(url, onComplete);
        }
        else if (typeof(T) == typeof(AudioClip))
        {
            Web.LoadAudioAsync(url, AudioType.UNKNOWN, onComplete);
        }
        else if (typeof(T) == typeof(AssetBundle))
        {
            Web.LoadRemoteAssetBundleAsync(url, onComplete);
        }
        else
        {
            Debug.LogError($"[ResCenter] 不支持的Web资源类型：{typeof(T).Name}");
            onComplete?.Invoke(null);
        }
    }
}
