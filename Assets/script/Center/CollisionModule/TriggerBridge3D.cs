using UnityEngine;

/// <summary>
/// 3D 触发器桥接：接收 Unity 3D Trigger 回调，通过委托向外抛出事件。
/// </summary>
public class TriggerBridge3D : CollisionBridgeBase<Collider>
{
    void OnTriggerEnter(Collider other) { enterEventParent?.Invoke(other); }
    void OnTriggerStay(Collider other) { stayEventParent?.Invoke(other); }
    void OnTriggerExit(Collider other) { exitEventParent?.Invoke(other); }
}
