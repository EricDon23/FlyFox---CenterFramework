using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 互动工具类：负责给物体挂载/获取互动标记（Tag）、注册互动事件、查询可交互事件。
/// 与 Tag 组件、EventCenter 协作完成整套互动流程。
/// </summary>
public static class InterActionUtil
{
    /// <summary>
    /// 获取或创建目标物体上的互动标记，并标记为可交互。
    /// </summary>
    /// <param name="obj">目标物体</param>
    /// <param name="type">互动类型字符串（如 "Open" / "Cold"），会解析为对应状态写入 Tag</param>
    /// <returns>物体上的 Tag 组件；物体为空时返回 null</returns>
    public static Tag InteractionTag(GameObject obj, string type)
    {
        if (obj == null)
        {
            Debug.LogWarning("InteractionTag：目标物体为空。");
            return null;
        }

        Tag tag = obj.GetComponent<Tag>();
        if (tag == null)
        {
            tag = obj.AddComponent<Tag>();
        }

        tag.Interaction = true;
        tag.Status = ParseInteractionType(type);
        return tag;
    }

    /// <summary>
    /// 注册互动事件：将事件名写入物体 Tag，并把无参回调订阅到 EventCenter。
    /// </summary>
    /// <param name="obj">目标物体（需已带 Tag 且可交互）</param>
    /// <param name="eventName">事件名</param>
    /// <param name="callback">无参回调</param>
    public static void SignInAction(GameObject obj, string eventName, Action callback)
    {
        if (!TryGetActionableTag(obj, out Tag tag))
        {
            Debug.LogWarning($"SignInAction：物体 {(obj != null ? obj.name : "null")} 未标记为可交互，添加事件 {eventName} 失败。");
            return;
        }

        tag.AddEventName(eventName);
        EventCenter.Instance.Subscribe(eventName, callback);
    }

    /// <summary>
    /// 注册互动事件：将事件名写入物体 Tag，并把带一个参数的回调订阅到 EventCenter。
    /// </summary>
    public static void SignInAction<T>(GameObject obj, string eventName, Action<T> callback)
    {
        if (!TryGetActionableTag(obj, out Tag tag))
        {
            Debug.LogWarning($"SignInAction：物体 {(obj != null ? obj.name : "null")} 未标记为可交互，添加事件 {eventName} 失败。");
            return;
        }

        tag.AddEventName(eventName);
        EventCenter.Instance.Subscribe(eventName, callback);
    }

    /// <summary>
    /// 检测碰撞体是否可交互，并返回其互动事件名列表。
    /// </summary>
    /// <param name="cobj">目标碰撞体</param>
    /// <returns>事件名列表；不可交互或无效时返回 null</returns>
    public static List<string> GetInteractionIndex(Collider cobj)
    {
        if (cobj == null)
        {
            Debug.LogWarning("GetInteractionIndex：碰撞体为空。");
            return null;
        }

        Tag tag = cobj.GetComponent<Tag>();
        if (tag == null || !tag.Interaction)
        {
            Debug.LogWarning($"GetInteractionIndex：物体 {cobj.name} 不可交互。");
            return null;
        }

        return tag.EventNames;
    }

    /// <summary>
    /// 解析互动类型字符串到枚举（忽略大小写），未知类型返回 None。
    /// </summary>
    private static InterActionType ParseInteractionType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return InterActionType.None;
        }

        if (Enum.TryParse(type, true, out InterActionType result))
        {
            return result;
        }

        Debug.LogWarning($"未知的互动类型：{type}，按 None 处理。");
        return InterActionType.None;
    }

    /// <summary>
    /// 校验物体是否带 Tag 且可交互。
    /// </summary>
    private static bool TryGetActionableTag(GameObject obj, out Tag tag)
    {
        tag = null;
        if (obj == null)
        {
            return false;
        }

        tag = obj.GetComponent<Tag>();
        return tag != null && tag.Interaction;
    }
}
