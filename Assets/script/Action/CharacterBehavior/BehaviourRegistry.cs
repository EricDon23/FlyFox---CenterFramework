using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 行为引用注册表：动态创建的实体（物品/敌人）注册 GameObject → Behavior 映射。
/// 玩家碰撞后查表调用行为方法，无需在实体上挂 Bridge 组件。
/// </summary>
public static class BehaviourRegistry
{
    private static readonly Dictionary<GameObject, ItemBehavior> _items = new Dictionary<GameObject, ItemBehavior>();
    private static readonly Dictionary<GameObject, EnemyShipBehavior> _enemies = new Dictionary<GameObject, EnemyShipBehavior>();

    public static void RegisterItem(GameObject go, ItemBehavior behavior) => _items[go] = behavior;
    public static void UnregisterItem(GameObject go) => _items.Remove(go);
    public static bool TryGetItem(GameObject go, out ItemBehavior behavior) => _items.TryGetValue(go, out behavior);

    public static void RegisterEnemy(GameObject go, EnemyShipBehavior behavior) => _enemies[go] = behavior;
    public static void UnregisterEnemy(GameObject go) => _enemies.Remove(go);
    public static bool TryGetEnemy(GameObject go, out EnemyShipBehavior behavior) => _enemies.TryGetValue(go, out behavior);
}
