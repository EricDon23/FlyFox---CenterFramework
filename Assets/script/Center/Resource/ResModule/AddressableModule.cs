using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

/// <summary>
/// Addressables运行时管理器
/// 封装Addressables资源加载逻辑，对外统一返回ResHandle，和ResourceModule/AssetBundleModule保持一致的Release生命周期
/// 支持：初始化、同步加载、异步加载、实例化、释放；
/// 地址支持：Addressable Key（字符串/GameObject引用）
/// </summary>
public class AddressableModule
{
    /// <summary>
    /// 缓存单个Addressable资源状态
    /// </summary>
    private struct AddressableAssetState
    {
        /// <summary>加载好的资源对象</summary>
        public UnityEngine.Object AssetObj;
        /// <summary>资源引用计数</summary>
        public int RefCount;
        /// <summary>Addressables原生加载句柄，用于释放</summary>
        public AsyncOperationHandle Handle;
    }

    /// <summary>资源缓存字典 key:addressable key字符串</summary>
    private readonly Dictionary<string, AddressableAssetState> _assetCache = new Dictionary<string, AddressableAssetState>();

    /// <summary>是否已经初始化Addressables</summary>
    private bool _isInited;

    /// <summary>是否已经初始化Addressables</summary>
    public bool IsInited => _isInited;

    /// <summary>
    /// 初始化Addressables（异步）
    /// 首次加载前必须调用，一般在游戏框架初始化阶段
    /// </summary>
    /// <param name="onComplete">初始化完成回调</param>
    public void Initialize(Action onComplete = null)
    {
        if (_isInited)
        {
            onComplete?.Invoke();
            return;
        }

        var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
        initHandle.Completed += h =>
        {
            _isInited = h.Status == AsyncOperationStatus.Succeeded;
            if (_isInited)
                Debug.Log("[AddressableModule] Addressables初始化完成");
            else
                Debug.LogError("[AddressableModule] Addressables初始化失败");
            onComplete?.Invoke();
        };
    }

    /// <summary>
    /// 阻塞式初始化Addressables，供同步加载前确保Addressables可用
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInited)
            return;
        // 必须传 false 关闭句柄自动释放：默认 autoReleaseHandle=true 会在初始化完成后
        // 自动释放句柄，WaitForCompletion() 之后再访问 handle.Status 会抛
        // "Attempting to use an invalid operation handle"
        var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync(false);
        initHandle.WaitForCompletion();
        _isInited = initHandle.Status == AsyncOperationStatus.Succeeded;
    }

    /// <summary>
    /// 同步加载Addressable资源（注意：Addressables原生同步加载会阻塞主线程，仅小资源使用）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="addressKey">Addressable资源Key（字符串地址）</param>
    /// <returns>统一ResHandle；加载失败返回null，用完必须Release()</returns>
    public ResHandle LoadAssetSync<T>(string addressKey) where T : UnityEngine.Object
    {
        if (!_isInited)
        {
            Debug.LogError("[AddressableModule] Addressables尚未初始化，请先调用Initialize()");
            return null;
        }

        // 命中缓存，引用计数+1
        if (_assetCache.TryGetValue(addressKey, out var assetState))
        {
            assetState.RefCount++;
            _assetCache[addressKey] = assetState;
            return new ResHandle(addressKey, assetState.AssetObj, OnAssetRelease);
        }

        // 原生同步加载
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(addressKey);
        handle.WaitForCompletion();

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[AddressableModule] 同步加载失败 key:{addressKey}");
            return null;
        }

        UnityEngine.Object assetObj = handle.Result;
        AddressableAssetState newState = new AddressableAssetState
        {
            AssetObj = assetObj,
            RefCount = 1,
            Handle = handle
        };
        _assetCache.Add(addressKey, newState);

        return new ResHandle(addressKey, assetObj, OnAssetRelease);
    }

    /// <summary>
    /// 异步加载Addressable资源，回调返回ResHandle
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="addressKey">Addressable资源Key</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadAssetAsync<T>(string addressKey, Action<ResHandle> onComplete) where T : UnityEngine.Object
    {
        if (!_isInited)
        {
            Debug.LogError("[AddressableModule] Addressables尚未初始化，请先调用Initialize()");
            onComplete?.Invoke(null);
            return;
        }

        if (_assetCache.TryGetValue(addressKey, out var assetState))
        {
            assetState.RefCount++;
            _assetCache[addressKey] = assetState;
            onComplete?.Invoke(new ResHandle(addressKey, assetState.AssetObj, OnAssetRelease));
            return;
        }

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(addressKey);
        handle.Completed += h =>
        {
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableModule] 异步加载失败 key:{addressKey}");
                onComplete?.Invoke(null);
                return;
            }

            UnityEngine.Object assetObj = h.Result;

            // 并发安全：若等待期间已有缓存，复用并只增计数，避免重复 Add 抛异常
            if (_assetCache.TryGetValue(addressKey, out var existing))
            {
                existing.RefCount++;
                _assetCache[addressKey] = existing;
                onComplete?.Invoke(new ResHandle(addressKey, existing.AssetObj, OnAssetRelease));
                return;
            }

            AddressableAssetState newState = new AddressableAssetState
            {
                AssetObj = assetObj,
                RefCount = 1,
                Handle = h
            };
            _assetCache[addressKey] = newState;
            onComplete?.Invoke(new ResHandle(addressKey, assetObj, OnAssetRelease));
        };
    }

    /// <summary>
    /// 资源释放回调，由ResHandle.Release触发
    /// </summary>
    private void OnAssetRelease(ResHandle handle)
    {
        string key = handle.AssetPath;
        if (!_assetCache.TryGetValue(key, out var assetState))
        {
            Debug.LogWarning($"[AddressableModule] Release找不到资源 key:{key}");
            return;
        }

        assetState.RefCount--;
        _assetCache[key] = assetState;

        if (assetState.RefCount <= 0)
        {
            // 引用计数归零，调用Addressables原生Release
            UnityEngine.AddressableAssets.Addressables.Release(assetState.Handle);
            _assetCache.Remove(key);
            Debug.Log($"[AddressableModule] 资源已释放 key:{key}");
        }
    }

    /// <summary>
    /// 清空全部缓存，场景切换时调用
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in _assetCache)
        {
            UnityEngine.AddressableAssets.Addressables.Release(kv.Value.Handle);
        }
        _assetCache.Clear();
        _isInited = false;
    }

    /// <summary>
    /// 查询资源当前引用计数
    /// </summary>
    public int GetAssetRefCount(string addressKey)
    {
        if (_assetCache.TryGetValue(addressKey, out var state))
        {
            return state.RefCount;
        }
        return 0;
    }
}
