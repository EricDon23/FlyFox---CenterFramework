using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可交互标记组件：挂在物体上表示该物体支持互动。
/// 记录互动事件名列表与当前互动状态，供 InterActionUtil 与 EventCenter 协作使用。
/// </summary>
public class Tag : MonoBehaviour
{
    /// <summary>是否可交互（对应旧命名 isAction）</summary>
    public bool Interaction = true;

    /// <summary>当前互动状态</summary>
    public InterActionType Status = InterActionType.None;

    /// <summary>注册到事件中心的互动事件名列表</summary>
    public List<string> EventNames { get; private set; } = new List<string>();

    /// <summary>
    /// 新增一个互动事件名（自动去重）
    /// </summary>
    /// <param name="name">事件名</param>
    public void AddEventName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!EventNames.Contains(name))
        {
            EventNames.Add(name);
        }
    }

    /// <summary>
    /// 判断是否已注册指定事件名
    /// </summary>
    public bool ContainsEvent(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && EventNames.Contains(name);
    }
}
