using UnityEngine;

/// <summary>
/// 物品碰撞桥接（业务层 MonoBehaviour）：动态实例化的物品自动挂载。
/// 职责：接收 OnTriggerEnter2D，检测碰撞方为玩家时转发到 ItemBehavior.OnPlayerTouch。
/// 不使用 [RequireComponent]——Collider2D 由 LevelBuild 在实例化时确保存在，避免 AddComponent 失败。
/// </summary>
public class ItemTriggerBridge : MonoBehaviour
{
    private ItemBehavior _item;

    /// <summary>绑定对应的 ItemBehavior 实例（由 LevelBuildBehavior 生成后调用）。</summary>
    public void Bind(ItemBehavior item)
    {
        _item = item;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_item == null) return;
        // 碰撞方带 Player 标签或 PlayerBehavior 所在物体时触发
        if (other.CompareTag("Player"))
        {
            // 从玩家物体上取 Player 数据；PlayerBehavior 是纯 C# 类，
            // 通过 EventCenter 查询不到，改为直接从 Hub 取第一个 PlayerBehavior 实例。
            var player = PlayerRefHolder.Instance.Player;
            if (player != null)
            {
                _item.OnPlayerTouch(player);
                // 收集后销毁物品
                Destroy(gameObject);
            }
        }
    }
}
