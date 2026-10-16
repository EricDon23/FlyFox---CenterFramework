using UnityEngine;

/// <summary>
/// 2D 触发器桥接：接收 Unity 2D Trigger 回调，通过委托向外抛出事件。
/// </summary>
public class TriggerBridge2D : CollisionBridgeBase<Collider2D>
{
    void OnTriggerEnter2D(Collider2D other) { enterEventParent?.Invoke(other); }
    void OnTriggerStay2D(Collider2D other) { stayEventParent?.Invoke(other); }
    void OnTriggerExit2D(Collider2D other) { exitEventParent?.Invoke(other); }
}
