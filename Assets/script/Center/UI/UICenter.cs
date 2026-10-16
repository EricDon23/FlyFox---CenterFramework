using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 中心（纯C# BasicSingle）：管理 UI 面板缓存、打开、关闭、销毁。
/// 单例缓存 + 隐藏复用；主UI由 GlobalConter 在引导阶段遍历 Registry 后调 OpenUI 打开。
/// </summary>
public class UICenter : BasicSingle<UICenter>
{
    private UICenter() { }

    private class UIEntry
    {
        public GameObject RootObj;
        public UIBehavior UiBehavior;
    }

    private readonly Dictionary<string, UIEntry> UICache = new Dictionary<string, UIEntry>();

    /// <summary>
    /// 打开UI：命中缓存直接显示；未命中交 BehaviourFactory.BuildUI 懒加载构造后缓存显示。
    /// </summary>
    public GameObject OpenUI(string registerKey)
    {
        if (UICache.TryGetValue(registerKey, out UIEntry entry))
        {
            entry.RootObj.SetActive(true);
            entry.UiBehavior.OnShow();
            return entry.RootObj;
        }

        // 懒加载：缓存未命中，交工厂构造（内部完成加载预制体+new行为+Init+LogicAwake）
        UIBehavior behavior = BehaviourFactory.Instance.BuildUI(registerKey);
        if (behavior == null || behavior.RootObj == null)
        {
            Debug.LogWarning($"[UICenter] 打开失败，UI构造失败：{registerKey}");
            return null;
        }

        UICache.Add(registerKey, new UIEntry { RootObj = behavior.RootObj, UiBehavior = behavior });
        behavior.RootObj.SetActive(true);
        behavior.OnShow();
        return behavior.RootObj;
    }

    /// <summary>关闭UI：隐藏 + OnHide()，不销毁。</summary>
    public void CloseUI(string registerKey)
    {
        if (UICache.TryGetValue(registerKey, out UIEntry entry))
        {
            entry.RootObj.SetActive(false);
            entry.UiBehavior.OnHide();
            return;
        }
        Debug.LogWarning($"[UICenter] 关闭失败，UI未注册：{registerKey}");
    }

    /// <summary>销毁单个UI：LogicDestroy退订 + Destroy物体 + 移出缓存。</summary>
    public void ClearOne(string registerKey)
    {
        if (UICache.TryGetValue(registerKey, out UIEntry entry))
        {
            UICache.Remove(registerKey);
            entry.UiBehavior?.LogicDestroy();
            if (entry.RootObj != null) Object.Destroy(entry.RootObj);
        }
    }

    /// <summary>销毁全部UI：退订 + Destroy物体 + 清空缓存（全局退出/场景切换调用）。</summary>
    public void DestroyAllUI()
    {
        foreach (UIEntry entry in UICache.Values)
        {
            entry.UiBehavior?.LogicDestroy();
            if (entry.RootObj != null) Object.Destroy(entry.RootObj);
        }
        UICache.Clear();
    }
}
