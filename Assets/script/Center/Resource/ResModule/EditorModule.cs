#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器资源工具模块，仅Editor环境可用
/// 配合ResCenter资源中心，用于工程资源扫描、路径/GUID转换、资源清单收集、资源校验
/// 职责：生成资源索引，供运行时ResourceModule使用；不参与游戏运行时资源加载
/// </summary>
public class EditorModule
{
    /// <summary>
    /// 资源信息缓存结构体，编辑器扫描时临时存储
    /// </summary>
    [Serializable]
    public struct EditorAssetInfo
    {
        /// <summary>资源GUID</summary>
        public string Guid;
        /// <summary>工程完整AssetPath，例：Assets/Prefab/Player.prefab</summary>
        public string AssetPath;
        /// <summary>资源类型</summary>
        public Type AssetType;
        /// <summary>资源文件名（不带后缀）</summary>
        public string AssetName;
    }

    /// <summary>资源扫描缓存字典 key:AssetPath</summary>
    private Dictionary<string, EditorAssetInfo> _assetPathCache = new Dictionary<string, EditorAssetInfo>();
    /// <summary>资源扫描缓存字典 key:GUID</summary>
    private Dictionary<string, EditorAssetInfo> _guidCache = new Dictionary<string, EditorAssetInfo>();

    /// <summary>
    /// 清空编辑器资源扫描缓存
    /// </summary>
    public void ClearCache()
    {
        _assetPathCache.Clear();
        _guidCache.Clear();
    }

    /// <summary>
    /// 扫描指定文件夹下所有资源，写入缓存
    /// </summary>
    /// <param name="searchFolder">Assets内目录，如 Assets/Prefab</param>
    /// <param name="searchType">筛选资源类型</param>
    public void ScanAssetsFolder<T>(string searchFolder) where T : UnityEngine.Object
    {
        if (!AssetDatabase.IsValidFolder(searchFolder))
        {
            Debug.LogWarning($"[EditorModule] 目录不存在：{searchFolder}");
            return;
        }

        string[] findAssets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchFolder });
        foreach (string guid in findAssets)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object assetObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (assetObj == null) continue;

            EditorAssetInfo info = new EditorAssetInfo
            {
                Guid = guid,
                AssetPath = assetPath,
                AssetType = assetObj.GetType(),
                AssetName = Path.GetFileNameWithoutExtension(assetPath)
            };

            // 索引器赋值幂等：资产移动后（同 GUID 新路径）重扫时不会因 _guidCache.Add 重复键抛异常
            _assetPathCache[assetPath] = info;
            _guidCache[guid] = info;
        }
    }

    /// <summary>
    /// 根据AssetPath获取编辑器资源信息
    /// </summary>
    /// <param name="assetPath">Assets开头路径</param>
    /// <returns>资源信息，不存在返回null</returns>
    public EditorAssetInfo? GetAssetInfoByPath(string assetPath)
    {
        if (_assetPathCache.TryGetValue(assetPath, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 根据GUID获取编辑器资源信息
    /// </summary>
    /// <param name="guid">资源GUID</param>
    /// <returns>资源信息，不存在返回null</returns>
    public EditorAssetInfo? GetAssetInfoByGuid(string guid)
    {
        if (_guidCache.TryGetValue(guid, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// 编辑器加载资源（仅编辑器模式使用，运行时禁止调用）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="assetPath">Assets开头路径</param>
    /// <returns>资源对象</returns>
    public T LoadEditorAsset<T>(string assetPath) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    /// <summary>
    /// 批量导出扫描到的资源清单（可序列化为json，供运行时预注册资源）
    /// </summary>
    /// <param name="savePath">保存的完整文件路径</param>
    public void ExportAssetManifest(string savePath)
    {
        ManifestWrapper wrapper = new ManifestWrapper { Items = _assetPathCache.Values.ToList() };
        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        File.WriteAllText(savePath, json);
        EditorUtility.DisplayDialog("导出完成", $"资源清单已导出至：{savePath}", "确定");
    }

    /// <summary>
    /// 校验资源：检查资源是否缺失、是否有重复名称
    /// </summary>
    public void ValidateAssets()
    {
        HashSet<string> nameSet = new HashSet<string>();
        int errorCount = 0;
        foreach (var info in _assetPathCache.Values)
        {
            if (string.IsNullOrEmpty(info.Guid) || !File.Exists(info.AssetPath))
            {
                Debug.LogError($"[EditorModule] 资源丢失：{info.AssetPath}");
                errorCount++;
                continue;
            }
            if (!nameSet.Add(info.AssetName))
            {
                Debug.LogWarning($"[EditorModule] 存在同名资源：{info.AssetName}，路径：{info.AssetPath}");
                errorCount++;
            }
        }
        Debug.Log($"[EditorModule] 资源校验完成，发现{errorCount}个问题");
    }

    /// <summary>
    /// 自动生成预注册代码片段，直接复制粘贴到运行时初始化代码，给ResourceModule.RegisterUnloaded使用
    /// </summary>
    public string GenerateRegisterCode()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("// Auto Generate by EditorModule");
        sb.AppendLine("// ResourceModule RegisterUnloaded Code");
        foreach (var info in _assetPathCache.Values)
        {
            sb.AppendLine($"ResCenter.Instance.ResourceModule.RegisterUnloaded<{info.AssetType.Name}>(\"{info.AssetPath}\");");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 资源清单可序列化包装类，供JsonUtility序列化List
    /// </summary>
    [Serializable]
    private class ManifestWrapper
    {
        public List<EditorAssetInfo> Items;
    }
}
#endif
