using UnityEngine;

/// <summary>
/// 物品行为（水果/金币/宝石）：被玩家触碰时加分或治疗。
/// 触发由 TriggerBridge2D 桥接，本类只暴露 OnPlayerTouch 回调。
/// </summary>
[AutoRegisterBehaviour("Item")]
public class ItemBehavior : Objectbehavior
{
    public ItemBehavior() { isBuild = false; } // 由 LevelBuild 动态创建，不随启动实例化

    private Fruit _fruit;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _fruit = objectDate as Fruit ?? new Fruit();
    }

    /// <summary>玩家触碰回调（由 TriggerBridge2D 转发）：满血加分，非满血治疗。</summary>
    public void OnPlayerTouch(Player player)
    {
        if (player == null) return;

        bool shouldScore = true;
        if (player.Health < player.MaxHealth && _fruit.Heal > 0)
        {
            player.Heal(_fruit.Heal);
            shouldScore = false;
            Debug.Log($"[ItemBehavior] 治疗 +{_fruit.Heal}，HP={player.Health}/{player.MaxHealth}");
            EventCenter.Instance.Broadcast<object>("PlayerHealed", player.Health);
        }

        if (shouldScore)
        {
            int weight = Mathf.RoundToInt(_fruit.BaseScore * (1f + Global.Level * 0.25f));
            Global.SessionBaseScore += weight;
            Debug.Log($"[ItemBehavior] 加分 +{weight}，总分={Global.SessionBaseScore}");
            EventCenter.Instance.Broadcast<int>("ScoreAdded", weight);
        }
    }
}
