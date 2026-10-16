using System;
using UnityEngine;

/// <summary>
/// 碰撞/触发桥接公共基类：统一持有挂载物体、数据与生命周期契约，
/// 提供 Init、Bind/Unbind/ClearEvents 事件注册。
/// 泛型参数 T 为碰撞/触发回调载荷类型（Collision / Collision2D / Collider / Collider2D）。
/// 子类只需定义各自的 Unity 碰撞回调（OnCollisionEnter / OnTriggerEnter 系列）并派发到对应委托。
///
/// 注意：本类为 MonoBehaviour，不经过工厂 Activator 创建；打 [AutoRegisterBehaviour] 仅占位不实例化，
/// 正确用法是直接挂到场景/预制体物体上。
/// </summary>
public abstract class CollisionBridgeBase<T> : MonoBehaviour, BasicObjectBehaviorImp
{
    protected GameObject g_obj;
    protected BasicDate date;

    /// <summary>是否「初始化加载」：保留字段仅为与工厂探测链路对齐，本类不经工厂实例化。</summary>
    public bool isBuild = false;

    /// <summary>保留字段仅为与工厂 isGlobal 探测链路对齐。</summary>
    public bool isGlobal = false;

    protected Action<T> enterEventParent;
    protected Action<T> stayEventParent;
    protected Action<T> exitEventParent;

    public virtual void Init(GameObject obj, BasicDate objectDate)
    {
        g_obj = obj;
        date = objectDate;
    }

    public virtual void LogicAwake() { }
    public virtual void LogicUpdate() { }

    /// <summary>销毁：默认清空自身碰撞委托；子类重写须先调用 base.LogicDestroy()，并在其内完成 EventCenter 退订。</summary>
    public virtual void LogicDestroy()
    {
        ClearEvents();
    }

    // —— 事件注册 ——
    public void BindEnterEvent(Action<T> callback) { enterEventParent += callback; }
    public void BindStayEvent(Action<T> callback) { stayEventParent += callback; }
    public void BindExitEvent(Action<T> callback) { exitEventParent += callback; }

    // —— 事件退订 ——
    public void UnbindEnterEvent(Action<T> callback) { enterEventParent -= callback; }
    public void UnbindStayEvent(Action<T> callback) { stayEventParent -= callback; }
    public void UnbindExitEvent(Action<T> callback) { exitEventParent -= callback; }

    public void ClearEvents()
    {
        enterEventParent = null;
        stayEventParent = null;
        exitEventParent = null;
    }
}
