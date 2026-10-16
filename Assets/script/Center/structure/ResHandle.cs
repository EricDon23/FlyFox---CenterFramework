using System;
using UnityEngine;

/// <summary>
/// 资源句柄，用于持有一份资源引用，保证资源引用计数正确释放
/// 句柄一旦调用Release后，不可重复使用
/// </summary>
public class ResHandle
{
    /// <summary>资源唯一标识（Resources路径 / AB内资源名 / URL / Addressable Key）</summary>
    public string AssetPath { get; }
    /// <summary>资源本体UnityEngine.Object</summary>
    public UnityEngine.Object AssetObj { get; }
    /// <summary>资源真实类型</summary>
    public Type AssetType { get; }
    /// <summary>标记当前句柄是否已经执行释放</summary>
    public bool IsReleased { get; private set; }

    private readonly Action<ResHandle> _onRelease;

    /// <summary>
    /// 构造资源句柄
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="asset">资源对象</param>
    /// <param name="onRelease">释放回调，由ResourceModule提供</param>
    public ResHandle(string path, UnityEngine.Object asset, Action<ResHandle> onRelease)
    {
        AssetPath = path;
        AssetObj = asset;
        AssetType = asset?.GetType();
        _onRelease = onRelease;
        IsReleased = false;
    }

    /// <summary>
    /// 释放当前资源引用，引用计数减一
    /// 重复调用Release会被内部标记拦截，防止多次减计数
    /// </summary>
    public void Release()
    {
        if (IsReleased) return;
        IsReleased = true;
        _onRelease?.Invoke(this);
    }
}
