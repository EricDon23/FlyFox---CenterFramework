using UnityEngine;

/// <summary>
/// 行为抽象基类：实现 BasicObjectBehaviorImp 契约，
/// 持有挂载物体 GameObject 与数据 BasicDate，提供可重写的生命周期钩子。
/// 所有游戏行为都要继承这个基类；运行靠 SingleMono 事件泵驱动，不依赖 MonoBehaviour。
///
/// 【isGlobal 分流】
///   isGlobal = true ：全局逻辑行为，不加载预制体、不创建 GameObject、不绑定独立 BasicDate，
///                     g_obj = null、date = null，数据直接读写 Global 静态全局参数；
///   isGlobal = false：实体行为，由工厂加载 registerKey 预制体并绑定对应 BasicDate。
/// </summary>
public abstract class Objectbehavior : BasicObjectBehaviorImp
{
    protected GameObject g_obj;
    protected BasicDate date;

    /// <summary>是否「初始化加载」：true=随启动实例化注入；false=后加载（待显式触发）。</summary>
    public bool isBuild = false;

    /// <summary>
    /// 是否全局逻辑行为：
    /// true=不抓预制体、不建物体、不绑数据（g_obj/date 为 null，直读 Global 静态数据）；
    /// false=实体行为，工厂加载预制体并绑定 BasicDate。
    /// </summary>
    public bool isGlobal = false;

    public virtual void Init(GameObject obj, BasicDate objectDate)
    {
        g_obj = obj;
        date = objectDate;
    }

    public virtual void LogicAwake()
    {
    }

    public virtual void LogicUpdate()
    {
    }

    public virtual void LogicDestroy()
    {
    }
}
