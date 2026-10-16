using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件中心。
/// 用于跨脚本通信，支持无参数事件和单参数事件。
/// 事件名不会被伪造为空委托，注册时只负责创建事件槽位，订阅时才真正绑定监听器。
/// 订阅/退订由各行为在自身 LogicAwake / LogicDestroy 内完成，本中心不持有行为实例。
/// </summary>
public class EventCenter : BasicSingle<EventCenter>
{
    private readonly Dictionary<string, Delegate> eventDictionary = new Dictionary<string, Delegate>();
    private readonly object syncRoot = new object();

    /// <summary>注册一个事件名，确保事件槽位存在，但不会插入一个空的占位函数。</summary>
    public void RegisterEvent(string eventName)
    {
        ValidateEventName(eventName);
        lock (syncRoot)
        {
            if (!eventDictionary.ContainsKey(eventName))
            {
                eventDictionary[eventName] = null;
                Debug.Log($"注册事件：{eventName}");
            }
        }
    }

    /// <summary>订阅无参数事件。</summary>
    public void Subscribe(string eventName, Action callback)
    {
        AddEventListener(eventName, callback);
    }

    /// <summary>订阅带一个参数的事件。</summary>
    public void Subscribe<T>(string eventName, Action<T> callback)
    {
        AddEventListener(eventName, callback);
    }

    /// <summary>取消订阅无参数事件。</summary>
    public void Unsubscribe(string eventName, Action callback)
    {
        RemoveEventListener(eventName, callback);
    }

    /// <summary>取消订阅带一个参数的事件。</summary>
    public void Unsubscribe<T>(string eventName, Action<T> callback)
    {
        RemoveEventListener(eventName, callback);
    }

    /// <summary>广播无参数事件。</summary>
    public void Broadcast(string eventName)
    {
        Invoke(eventName, null, false);
    }

    /// <summary>广播带一个参数的事件。</summary>
    public void Broadcast<T>(string eventName, T arg)
    {
        Invoke(eventName, arg, true);
    }

    /// <summary>销毁指定事件，并清空其所有监听器。</summary>
    public void DestroyEvent(string eventName)
    {
        ValidateEventName(eventName);
        lock (syncRoot)
        {
            if (eventDictionary.Remove(eventName))
            {
                Debug.Log($"销毁事件：{eventName}");
            }
        }
    }

    /// <summary>清空全部事件表，适用于场景切换或重置状态。</summary>
    public void Clear()
    {
        lock (syncRoot)
        {
            eventDictionary.Clear();
        }
    }

    /// <summary>安全注册监听器，避免重复添加和空引用。</summary>
    private void AddEventListener(string eventName, Delegate callback)
    {
        ValidateEventName(eventName);
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "监听器不能为空。");
        }
        lock (syncRoot)
        {
            if (!eventDictionary.TryGetValue(eventName, out var existing))
            {
                eventDictionary[eventName] = callback;
                return;
            }
            if (existing != null && existing.GetType() != callback.GetType())
            {
                throw new InvalidOperationException($"事件 {eventName} 已存在不同签名的监听器，不能混用。");
            }
            eventDictionary[eventName] = existing == null ? callback : Delegate.Combine(existing, callback);
        }
    }

    /// <summary>安全移除监听器。</summary>
    private void RemoveEventListener(string eventName, Delegate callback)
    {
        ValidateEventName(eventName);
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "监听器不能为空。");
        }
        lock (syncRoot)
        {
            if (!eventDictionary.TryGetValue(eventName, out var existing))
            {
                Debug.LogWarning($"事件 {eventName} 不存在，无法移除。");
                return;
            }
            if (existing == null)
            {
                return;
            }
            if (existing.GetType() != callback.GetType())
            {
                Debug.LogWarning($"事件 {eventName} 监听器签名不匹配，无法移除。");
                return;
            }
            var newDelegate = Delegate.Remove(existing, callback);
            if (newDelegate == null)
            {
                eventDictionary.Remove(eventName);
            }
            else
            {
                eventDictionary[eventName] = newDelegate;
            }
        }
    }

    /// <summary>统一广播入口，避免在回调过程中修改事件表导致异常。</summary>
    private void Invoke(string eventName, object arg, bool hasArg)
    {
        ValidateEventName(eventName);
        Delegate callback;
        lock (syncRoot)
        {
            if (!eventDictionary.TryGetValue(eventName, out callback) || callback == null)
            {
                return;
            }
        }

        // 无参广播：只允许 Action（零参）；逐个调用 GetInvocationList 实现异常隔离
        if (!hasArg)
        {
            if (callback is Action action)
            {
                foreach (Action sub in action.GetInvocationList())
                {
                    try
                    {
                        sub();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventCenter] 事件 {eventName} 回调异常：{e}");
                    }
                }
                return;
            }
            Debug.LogWarning($"事件 {eventName} 不是无参事件，无法进行无参数广播。");
            return;
        }

        // 带参广播：不允许打到无参事件
        if (callback is Action)
        {
            throw new InvalidOperationException($"事件 {eventName} 不能用带参数广播调用无参事件。");
        }

        // 兼容性分派：Action<T> 的参数类型与载荷做可赋值判断（支持协变），而非精确类型匹配
        Type callbackType = callback.GetType();
        if (callbackType.IsGenericType && callbackType.GetGenericTypeDefinition() == typeof(Action<>))
        {
            Type paramType = callbackType.GetGenericArguments()[0];
            if (arg == null)
            {
                if (paramType.IsValueType)
                {
                    Debug.LogWarning($"事件 {eventName} 参数为值类型 {paramType.Name}，不能传入 null。");
                    return;
                }
            }
            else if (!paramType.IsInstanceOfType(arg))
            {
                Debug.LogWarning($"事件 {eventName} 的参数类型不匹配，广播被忽略。");
                return;
            }

            // 逐个调用 GetInvocationList：单个回调抛异常不中断其他监听器
            foreach (Delegate sub in callback.GetInvocationList())
            {
                try
                {
                    sub.DynamicInvoke(arg);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventCenter] 事件 {eventName} 回调异常：{e}");
                }
            }
            return;
        }
        Debug.LogWarning($"事件 {eventName} 的参数类型不匹配，广播被忽略。");
    }

    /// <summary>校验事件名是否合法，不允许空字符串或者仅空白字符。</summary>
    private static void ValidateEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("事件名不能为空。", nameof(eventName));
        }
    }
}
