using UnityEngine;

/// <summary>
/// 2D 碰撞桥接：接收 Unity 2D 碰撞回调，通过委托向外抛出事件。
/// </summary>
public class CollisionBridge2D : CollisionBridgeBase<Collision2D>
{
    void OnCollisionEnter2D(Collision2D collision) { enterEventParent?.Invoke(collision); }
    void OnCollisionStay2D(Collision2D collision) { stayEventParent?.Invoke(collision); }
    void OnCollisionExit2D(Collision2D collision) { exitEventParent?.Invoke(collision); }
}
