using UnityEngine;

/// <summary>
/// 3D 碰撞桥接：接收 Unity 3D 碰撞回调，通过委托向外抛出事件。
/// </summary>
public class CollisionBridge3D : CollisionBridgeBase<Collision>
{
    void OnCollisionEnter(Collision collision) { enterEventParent?.Invoke(collision); }
    void OnCollisionStay(Collision collision) { stayEventParent?.Invoke(collision); }
    void OnCollisionExit(Collision collision) { exitEventParent?.Invoke(collision); }
}
