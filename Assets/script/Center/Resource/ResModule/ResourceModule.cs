using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Resources目录资源管理器
/// 采用字典+struct引用计数模型，管理资源注册、加载、缓存、引用计数、卸载生命周期
/// 职责：仅管理UnityEngine.Object资产模板，不做实例化Instantiate、不管理场景实体
/// </summary>
public class ResourceModule 
{
    /// <summary>资源状态结构体，存储单条资源的加载状态、资源对象、引用计数、资源类型</summary>
    private struct ResourceState
    {
        /// <summary>资源是否已经加载进内存</summary>
        public bool IsLoaded;
        /// <summary>资源本体，未加载时为null</summary>
        public UnityEngine.Object AssetObj;
        /// <summary>资源引用计数，每获取一次句柄+1，Release一次-1；计数为0则卸载资源</summary>
        public int RefCount;
    }

    /// <summary>资源主字典 key:Resources资源路径 value:资源状态</summary>
    private readonly Dictionary<string, ResourceState> _resDict = new Dictionary<string, ResourceState>();

    /// <summary>异步加载中的路径集合，用于并发去重，防止重复加载与计数漂移</summary>
    private readonly HashSet<string> _loadingPaths = new HashSet<string>();

    /// <summary>
    /// 预注册未加载资源
    /// 仅把资源路径和类型写入字典台账，**不会加载资源到内存**
    /// 用于关卡启动时预先登记资源清单，后续可按需加载
    /// </summary>
    /// <typeparam name="T">资源类型（UnityEngine.Object子类）</typeparam>
    /// <param name="path">Resources资源路径</param>
    /// <returns>true=注册成功；false=该路径已存在</returns>
    public bool RegisterUnloaded<T>(string path) where T : UnityEngine.Object
    {
        if (_resDict.ContainsKey(path))
        {
            Debug.LogWarning($"[ResourceModule] 资源已存在：{path}");
            return false;
        }

        _resDict.Add(path, new ResourceState
        {
            IsLoaded = false,
            AssetObj = null,
            RefCount = 0
        });
        return true;
    }

    /// <summary>Resources 搜索路径配置（从 PathSetting ScriptableObject 读取子文件夹列表）。</summary>
    private List<string> _searchPaths;
    private PathSetting _pathSetting;

    /// <summary>懒加载 PathSetting 单例配置。</summary>
    private PathSetting GetPathSetting()
    {
        if (_pathSetting != null) return _pathSetting;
        _pathSetting = Resources.Load<PathSetting>("PathSetting");
        return _pathSetting;
    }

    /// <summary>懒加载 Resources 搜索路径列表。</summary>
    private List<string> GetSearchPaths()
    {
        if (_searchPaths != null) return _searchPaths;

        var setting = GetPathSetting();
        if (setting != null && setting.ResourcesPath != null && setting.ResourcesPath.Count > 0)
        {
            _searchPaths = setting.ResourcesPath;
            Debug.Log($"[ResourceModule] Resources 搜索路径：{string.Join(", ", _searchPaths)}");
        }
        else
        {
            _searchPaths = new List<string> { "", "Prefabs", "Entities", "Gameplay", "UI", "Config" };
        }
        return _searchPaths;
    }

    /// <summary>
    /// 同步加载 Resources 资源。
    /// 无前缀路径按 PathSetting 配置的子文件夹列表依次尝试。
    /// 多后端路由（ab:// addr:// http://）由 ResCenter 分发，不在此处理。
    /// </summary>
    public ResHandle LoadSync<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path)) return null;

        foreach (string folder in GetSearchPaths())
        {
            string candidate = string.IsNullOrEmpty(folder) ? path : $"{folder}/{path}";
            ResHandle h = TryLoadDirect<T>(candidate);
            if (h != null)
            {
                if (!string.IsNullOrEmpty(folder))
                    Debug.Log($"[ResourceModule] 在 {candidate} 找到资源");
                return h;
            }
        }

        Debug.LogError($"[ResourceModule] LoadSync 加载失败：{path}（已搜索所有配置路径）");
        return null;
    }

    /// <summary>按完整路径加载一条资源，成功返回句柄，失败返回null。</summary>
    private ResHandle TryLoadDirect<T>(string path) where T : UnityEngine.Object
    {
        ResourceState state;
        if (_resDict.TryGetValue(path, out state))
        {
            if (!state.IsLoaded)
            {
                T asset = Resources.Load<T>(path);
                if (asset == null) return null;
                state.AssetObj = asset;
                state.IsLoaded = true;
            }
            state.RefCount++;
            _resDict[path] = state;
            return new ResHandle(path, state.AssetObj, OnResourceRelease);
        }

        T newAsset = Resources.Load<T>(path);
        if (newAsset == null) return null;

        var newState = new ResourceState
        {
            IsLoaded = true,
            AssetObj = newAsset,
            RefCount = 1
        };
        _resDict.Add(path, newState);
        return new ResHandle(path, newAsset, OnResourceRelease);
    }

    /// <summary>
    /// 异步加载Resources资源
    /// 预注册资源可走异步加载，加载完成后回调返回资源句柄
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">Resources资源路径</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadAsync<T>(string path, Action<ResHandle> onComplete) where T : UnityEngine.Object
    {
        // 已加载：直接复用缓存，引用计数+1
        if (_resDict.TryGetValue(path, out ResourceState state) && state.IsLoaded)
        {
            state.RefCount++;
            _resDict[path] = state;
            onComplete?.Invoke(new ResHandle(path, state.AssetObj, OnResourceRelease));
            return;
        }

        // 并发保护：同一路径已在异步加载中，忽略重复请求，避免重复加载与计数漂移
        if (!_loadingPaths.Add(path))
        {
            Debug.LogWarning($"[ResourceModule] 路径已在异步加载中，忽略重复请求：{path}");
            onComplete?.Invoke(null);
            return;
        }

        ResourceRequest request = Resources.LoadAsync<T>(path);
        request.completed += op =>
        {
            _loadingPaths.Remove(path);

            ResourceRequest rq = op as ResourceRequest;
            UnityEngine.Object asset = rq.asset;
            if (asset == null)
            {
                Debug.LogError($"[ResourceModule] LoadAsync 加载失败：{path}");
                onComplete?.Invoke(null);
                return;
            }

            // 若等待期间已被其它加载登记，则复用并只增计数，避免重复 Add 抛异常
            if (_resDict.TryGetValue(path, out var existing) && existing.IsLoaded)
            {
                existing.RefCount++;
                _resDict[path] = existing;
                onComplete?.Invoke(new ResHandle(path, existing.AssetObj, OnResourceRelease));
                return;
            }

            var newState = new ResourceState
            {
                IsLoaded = true,
                AssetObj = asset,
                RefCount = 1
            };
            _resDict[path] = newState;
            onComplete?.Invoke(new ResHandle(path, asset, OnResourceRelease));
        };
    }

    /// <summary>
    /// 批量加载文件夹下全部Resources资源
    /// 遍历目录内资源，逐个调用LoadSync，返回一组资源句柄
    /// </summary>
    /// <typeparam name="T">目标资源类型</typeparam>
    /// <param name="folderPath">Resources文件夹路径</param>
    /// <returns>资源句柄列表</returns>
    public List<ResHandle> AllLoad<T>(string folderPath) where T : UnityEngine.Object
    {
        List<ResHandle> handles = new List<ResHandle>();
        T[] assets = Resources.LoadAll<T>(folderPath);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[ResourceModule] AllLoad 目录无资源：{folderPath}");
            return handles;
        }

        foreach (var asset in assets)
        {
            string assetPath = string.IsNullOrEmpty(folderPath) ? asset.name : $"{folderPath}/{asset.name}";

            // 直接登记已加载资源，避免二次 LoadSync 因子目录/嵌套 Resources 导致路径解析失败
            if (_resDict.TryGetValue(assetPath, out ResourceState st) && st.IsLoaded)
            {
                st.RefCount++;
                _resDict[assetPath] = st;
                handles.Add(new ResHandle(assetPath, st.AssetObj, OnResourceRelease));
            }
            else
            {
                var newState = new ResourceState { IsLoaded = true, AssetObj = asset, RefCount = 1 };
                _resDict[assetPath] = newState;
                handles.Add(new ResHandle(assetPath, asset, OnResourceRelease));
            }
        }
        return handles;
    }

    /// <summary>
    /// 在本模块缓存中按路径查找已加载资源
    /// 仅能查找已经加载完成的资源；预注册但未加载资源返回null
    /// 找到后引用计数+1，返回可用句柄
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="assetPath">资源路径</param>
    /// <returns>资源句柄，不存在/未加载/类型不匹配返回null</returns>
    public ResHandle Find<T>(string assetPath) where T : UnityEngine.Object
    {
        if (_resDict.TryGetValue(assetPath, out var state))
        {
            if (!state.IsLoaded)
            {
                Debug.LogWarning($"[ResourceModule] Find：资源预注册但未加载 {assetPath}");
                return null;
            }
            if (state.AssetObj is T targetAsset)
            {
                state.RefCount++;
                _resDict[assetPath] = state;
                return new ResHandle(assetPath, targetAsset, OnResourceRelease);
            }
            Debug.LogError($"[ResourceModule] Find：类型不匹配 {assetPath}");
        }
        return null;
    }

    /// <summary>
    /// 全局查找内存中所有T类型对象
    /// 直接透传 Resources.FindObjectsOfTypeAll，不生成句柄、不修改引用计数
    /// 返回的是内存中所有该类型对象（含场景内实例），仅用于遍历/检查，不参与本模块生命周期管理
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>内存中所有T类型对象列表</returns>
    public List<T> FindObjectsOfTypeAll<T>() where T : UnityEngine.Object
    {
        return new List<T>(Resources.FindObjectsOfTypeAll<T>());
    }

    /// <summary>
    /// 全局查找，并筛选出本模块托管的T类型资源
    /// 遍历资源台账，匹配已加载且类型为T的资源，逐条引用计数+1并返回句柄
    /// 用于一次性拿到「由本模块管理」的同类型资源集合
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>本模块托管的T类型资源句柄列表，用完需逐个Release()</returns>
    public List<ResHandle> FindObjectsOfTypeAllInCache<T>() where T : UnityEngine.Object
    {
        List<ResHandle> handles = new List<ResHandle>();
        foreach (var kv in _resDict)
        {
            var state = kv.Value;
            if (!state.IsLoaded || state.AssetObj == null || !(state.AssetObj is T))
                continue;

            state.RefCount++;
            _resDict[kv.Key] = state;
            handles.Add(new ResHandle(kv.Key, state.AssetObj, OnResourceRelease));
        }
        return handles;
    }


    /// <summary>
    /// 资源释放回调，由ResHandle.Release触发
    /// 引用计数减一；当计数<=0，移除字典记录并卸载资源
    /// </summary>
    /// <param name="handle">触发释放的资源句柄</param>
    private void OnResourceRelease(ResHandle handle)
    {
        string path = handle.AssetPath;
        if (!_resDict.TryGetValue(path, out var state))
        {
            Debug.LogWarning($"[ResourceModule] Release找不到资源记录：{path}");
            return;
        }

        state.RefCount--;
        if (state.RefCount <= 0)
        {
            _resDict.Remove(path);
            if (state.IsLoaded && state.AssetObj != null &&
                !(state.AssetObj is GameObject) && !(state.AssetObj is Component))
            {
                Resources.UnloadAsset(state.AssetObj);
            }
        }
        else
        {
            _resDict[path] = state;
        }
    }

    /// <summary>
    /// 清空全部资源台账，调用引擎卸载未使用资源
    /// 一般用于场景切换时整体清理
    /// </summary>
    public void ClearAll()
    {
        _resDict.Clear();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 获取指定资源当前引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>引用计数，不存在返回0</returns>
    public int GetRefCount(string path)
    {
        if (_resDict.TryGetValue(path, out var state))
            return state.RefCount;
        return 0;
    }

    /// <summary>
    /// 查询资源是否已经加载到内存
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>true：已加载；false：未注册 / 预注册但未加载</returns>
    public bool IsAssetLoaded(string path)
    {
        if (_resDict.TryGetValue(path, out var state))
            return state.IsLoaded;
        return false;
    }
}
