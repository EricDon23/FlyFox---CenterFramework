using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 网络资源加载模块，基于UnityWebRequest
/// 支持远程下载文本、二进制、贴图、音频、远程AssetBundle；统一ResHandle句柄与引用计数
/// </summary>
public class UnityWebRequestModule
{
    /// <summary>
    /// 网络资源缓存状态
    /// </summary>
    private struct WebAssetState
    {
        /// <summary>资源对象</summary>
        public UnityEngine.Object AssetObj;
        /// <summary>资源引用计数</summary>
        public int RefCount;
    }

    /// <summary>资源缓存字典 key:资源完整Url</summary>
    private readonly Dictionary<string, WebAssetState> _webAssetCache = new Dictionary<string, WebAssetState>();

    /// <summary>
    /// 异步下载文本资源
    /// </summary>
    /// <param name="url">远程资源地址</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadTextAsync(string url, Action<ResHandle> onComplete)
    {
        // 命中缓存，引用计数+1
        if (_webAssetCache.TryGetValue(url, out var assetState))
        {
            assetState.RefCount++;
            _webAssetCache[url] = assetState;
            onComplete?.Invoke(new ResHandle(url, assetState.AssetObj, OnAssetRelease));
            return;
        }

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UnityWebRequestModule] 文本下载失败 url:{url} error:{req.error}");
                onComplete?.Invoke(null);
                req.Dispose();
                return;
            }

            TextAsset textAsset = new TextAsset(req.downloadHandler.text);
            WebAssetState newState = new WebAssetState
            {
                AssetObj = textAsset,
                RefCount = 1
            };
            _webAssetCache[url] = newState;
            onComplete?.Invoke(new ResHandle(url, textAsset, OnAssetRelease));
            req.Dispose();
        };
    }

    /// <summary>
    /// 异步下载Texture2D
    /// </summary>
    /// <param name="url">图片地址</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadTextureAsync(string url, Action<ResHandle> onComplete)
    {
        if (_webAssetCache.TryGetValue(url, out var assetState))
        {
            assetState.RefCount++;
            _webAssetCache[url] = assetState;
            onComplete?.Invoke(new ResHandle(url, assetState.AssetObj, OnAssetRelease));
            return;
        }

        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UnityWebRequestModule] 贴图下载失败 url:{url} error:{req.error}");
                onComplete?.Invoke(null);
                req.Dispose();
                return;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            WebAssetState newState = new WebAssetState
            {
                AssetObj = tex,
                RefCount = 1
            };
            _webAssetCache[url] = newState;
            onComplete?.Invoke(new ResHandle(url, tex, OnAssetRelease));
            req.Dispose();
        };
    }

    /// <summary>
    /// 异步下载AudioClip
    /// </summary>
    /// <param name="url">音频地址</param>
    /// <param name="audioType">音频格式</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadAudioAsync(string url, AudioType audioType, Action<ResHandle> onComplete)
    {
        if (_webAssetCache.TryGetValue(url, out var assetState))
        {
            assetState.RefCount++;
            _webAssetCache[url] = assetState;
            onComplete?.Invoke(new ResHandle(url, assetState.AssetObj, OnAssetRelease));
            return;
        }

        UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UnityWebRequestModule] 音频下载失败 url:{url} error:{req.error}");
                onComplete?.Invoke(null);
                req.Dispose();
                return;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            WebAssetState newState = new WebAssetState
            {
                AssetObj = clip,
                RefCount = 1
            };
            _webAssetCache[url] = newState;
            onComplete?.Invoke(new ResHandle(url, clip, OnAssetRelease));
            req.Dispose();
        };
    }

    /// <summary>
    /// 异步下载远程AssetBundle
    /// </summary>
    /// <param name="url">远程AB包地址</param>
    /// <param name="onComplete">加载完成回调，成功返回句柄，失败传入null</param>
    public void LoadRemoteAssetBundleAsync(string url, Action<ResHandle> onComplete)
    {
        if (_webAssetCache.TryGetValue(url, out var assetState))
        {
            assetState.RefCount++;
            _webAssetCache[url] = assetState;
            onComplete?.Invoke(new ResHandle(url, assetState.AssetObj, OnAssetRelease));
            return;
        }

        UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url);
        req.SendWebRequest().completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[UnityWebRequestModule] 远程AB下载失败 url:{url} error:{req.error}");
                onComplete?.Invoke(null);
                req.Dispose();
                return;
            }

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(req);
            WebAssetState newState = new WebAssetState
            {
                AssetObj = bundle,
                RefCount = 1
            };
            _webAssetCache[url] = newState;
            onComplete?.Invoke(new ResHandle(url, bundle, OnAssetRelease));
            req.Dispose();
        };
    }

    /// <summary>
    /// 资源释放回调，由ResHandle.Release触发
    /// </summary>
    private void OnAssetRelease(ResHandle handle)
    {
        string url = handle.AssetPath;
        if (!_webAssetCache.TryGetValue(url, out var assetState))
        {
            Debug.LogWarning($"[UnityWebRequestModule] Release找不到资源 url:{url}");
            return;
        }

        assetState.RefCount--;
        _webAssetCache[url] = assetState;

        if (assetState.RefCount <= 0)
        {
            // 销毁资源
            if (assetState.AssetObj != null)
            {
                if (assetState.AssetObj is AssetBundle ab)
                {
                    ab.Unload(true);
                }
                else
                {
                    UnityEngine.Object.Destroy(assetState.AssetObj);
                }
            }
            _webAssetCache.Remove(url);
            Debug.Log($"[UnityWebRequestModule] 网络资源已释放 url:{url}");
        }
    }

    /// <summary>
    /// 清空全部网络资源缓存，场景切换时调用
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in _webAssetCache)
        {
            var assetObj = kv.Value.AssetObj;
            if (assetObj != null)
            {
                if (assetObj is AssetBundle ab)
                    ab.Unload(true);
                else
                    UnityEngine.Object.Destroy(assetObj);
            }
        }
        _webAssetCache.Clear();
    }

    /// <summary>
    /// 查询资源当前引用计数
    /// </summary>
    public int GetAssetRefCount(string url)
    {
        if (_webAssetCache.TryGetValue(url, out var state))
        {
            return state.RefCount;
        }
        return 0;
    }
}
