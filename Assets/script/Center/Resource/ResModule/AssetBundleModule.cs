using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AssetBundle运行时管理器
/// 负责AssetBundle包加载、缓存、AB包引用计数、包内资源加载
/// 对外统一返回ResHandle，和ResourceModule使用相同句柄模型，统一Release生命周期
/// 两层计数：AB包自身引用计数；AB内资源引用计数
/// </summary>
public class AssetBundleModule
{
    /// <summary>
    /// AssetBundle包状态信息
    /// </summary>
    private class BundleState
    {
        /// <summary>加载完成的AB包实例</summary>
        public AssetBundle Bundle;
        /// <summary>AB包自身引用计数，加载+1，包卸载-1</summary>
        public int BundleRefCount;
        /// <summary>包内资源字典 key:资源在AB内的路径，value:资源状态</summary>
        public Dictionary<string, AssetState> AssetDict = new Dictionary<string, AssetState>();
    }

    /// <summary>
    /// AB包内单个资源状态
    /// </summary>
    private struct AssetState
    {
        /// <summary>资源对象</summary>
        public UnityEngine.Object AssetObj;
        /// <summary>资源引用计数</summary>
        public int RefCount;
    }

    /// <summary>AB包缓存字典 key:AB包名称/AB路径</summary>
    private readonly Dictionary<string, BundleState> _bundleDict = new Dictionary<string, BundleState>();

    /// <summary>
    /// 同步加载AssetBundle包
    /// </summary>
    /// <param name="bundlePath">AB包完整本地路径</param>
    /// <returns>true加载成功；false失败</returns>
    public bool LoadBundleSync(string bundlePath)
    {
        string bundleKey = bundlePath;
        if (_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            // 包已加载：幂等返回，不重复累加计数（bundle 生命周期由包内资源引用驱动）
            return true;
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError($"[AssetBundleModule] AB包加载失败：{bundlePath}");
            return false;
        }

        BundleState newBundleState = new BundleState
        {
            Bundle = bundle,
            BundleRefCount = 1
        };
        _bundleDict.Add(bundleKey, newBundleState);
        return true;
    }

    /// <summary>
    /// 同步从已加载AB包中加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="bundlePath">AB包路径key</param>
    /// <param name="assetName">AB包内资源名称（支持带路径）</param>
    /// <returns>资源句柄；加载失败返回null，用完必须调用handle.Release()</returns>
    public ResHandle LoadAssetSync<T>(string bundlePath, string assetName) where T : UnityEngine.Object
    {
        string bundleKey = bundlePath;
        if (!_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            Debug.LogError($"[AssetBundleModule] AB包未加载：{bundlePath}");
            return null;
        }

        // 查找包内已有资源缓存
        if (bundleState.AssetDict.TryGetValue(assetName, out var assetState))
        {
            assetState.RefCount++;
            bundleState.AssetDict[assetName] = assetState;
            return new ResHandle(assetName, assetState.AssetObj, (handle) => OnAssetRelease(bundleKey, assetName, handle));
        }

        // AB包加载资源
        T asset = bundleState.Bundle.LoadAsset<T>(assetName);
        if (asset == null)
        {
            Debug.LogError($"[AssetBundleModule] AB内资源加载失败，包:{bundlePath} asset:{assetName}");
            return null;
        }

        AssetState newAssetState = new AssetState
        {
            AssetObj = asset,
            RefCount = 1
        };
        bundleState.AssetDict.Add(assetName, newAssetState);
        return new ResHandle(assetName, asset, (handle) => OnAssetRelease(bundleKey, assetName, handle));
    }

    /// <summary>
    /// AB资源释放回调，由ResHandle.Release触发
    /// 资源计数减一；资源计数归零后卸载资源；
    /// 包内所有资源全部释放后，才减少AB包引用计数
    /// </summary>
    private void OnAssetRelease(string bundleKey, string assetName, ResHandle handle)
    {
        if (!_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            Debug.LogWarning($"[AssetBundleModule] Release：找不到AB包 {bundleKey}");
            return;
        }

        if (!bundleState.AssetDict.TryGetValue(assetName, out var assetState))
        {
            Debug.LogWarning($"[AssetBundleModule] Release：包内找不到资源 {assetName}");
            return;
        }

        assetState.RefCount--;
        bundleState.AssetDict[assetName] = assetState;

        // 资源引用计数为0，卸载资源，从包内清单移除
        if (assetState.RefCount <= 0)
        {
            Resources.UnloadAsset(assetState.AssetObj);
            bundleState.AssetDict.Remove(assetName);
        }

        // 判断包内是否还有存活资源；无存活资源，则AB包计数-1
        if (bundleState.AssetDict.Count == 0)
        {
            bundleState.BundleRefCount--;
            if (bundleState.BundleRefCount <= 0)
            {
                bundleState.Bundle.Unload(true);
                _bundleDict.Remove(bundleKey);
                Debug.Log($"[AssetBundleModule] AB包已完全卸载：{bundleKey}");
            }
        }
    }

    /// <summary>
    /// 卸载指定AB包（强制卸载，不等待资源计数）
    /// 慎用：会直接销毁所有该包内资源实例，句柄会失效
    /// </summary>
    /// <param name="bundlePath">AB包key</param>
    public void ForceUnloadBundle(string bundlePath)
    {
        string bundleKey = bundlePath;
        if (_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            bundleState.Bundle.Unload(true);
            _bundleDict.Remove(bundleKey);
        }
    }

    /// <summary>
    /// 清空全部AB包与资源缓存，场景切换时整体清理
    /// </summary>
    public void ClearAll()
    {
        foreach (var pair in _bundleDict)
        {
            pair.Value.Bundle.Unload(true);
        }
        _bundleDict.Clear();
    }

    /// <summary>
    /// 获取AB包当前引用计数
    /// </summary>
    /// <param name="bundlePath">AB包key</param>
    /// <returns>引用计数，不存在返回0</returns>
    public int GetBundleRefCount(string bundlePath)
    {
        string bundleKey = bundlePath;
        if (_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            return bundleState.BundleRefCount;
        }
        return 0;
    }

    /// <summary>
    /// 获取AB包内资源引用计数
    /// </summary>
    /// <param name="bundlePath">AB包key</param>
    /// <param name="assetName">包内资源名</param>
    /// <returns>资源引用计数，不存在返回0</returns>
    public int GetAssetRefCount(string bundlePath, string assetName)
    {
        string bundleKey = bundlePath;
        if (_bundleDict.TryGetValue(bundleKey, out var bundleState))
        {
            if (bundleState.AssetDict.TryGetValue(assetName, out var assetState))
            {
                return assetState.RefCount;
            }
        }
        return 0;
    }
}
