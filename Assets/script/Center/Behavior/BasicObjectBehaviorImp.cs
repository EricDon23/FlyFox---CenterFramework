using UnityEngine;

/// <summary>
/// 基础行为契约：所有行为类（Objectbehavior 子类、CollisionBridgeBase 子类）必须实现。
/// 逐帧/逐物理帧驱动由行为在 LogicAwake 内自行订阅 EventCenter 事件，不在此接口强制。
/// </summary>
public interface BasicObjectBehaviorImp
{
    /// <summary>装配：绑定挂载物体与数据。</summary>
    void Init(GameObject obj, BasicDate objectDate);

    /// <summary>启动：订阅事件、创建逻辑。</summary>
    void LogicAwake();

    /// <summary>帧更新钩子（保留给需要手动轮询的场景；帧驱动走 EventCenter 订阅）。</summary>
    void LogicUpdate();

    /// <summary>销毁：退订事件、释放资源。</summary>
    void LogicDestroy();
}
