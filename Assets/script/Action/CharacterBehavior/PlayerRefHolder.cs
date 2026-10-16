/// <summary>
/// 玩家数据引用持有者（业务层纯 C# 单例）：供碰撞桥接等被动回调查询当前玩家数据。
/// 【改造说明】旧代码中玩家是 MonoBehaviour，碰撞时 GetComponent<PlayerBehavior> 即可拿到数据；
/// 新框架下 PlayerBehavior 是纯 C# 类，挂在物体上的 Collider2D 无法 GetComponent 到它。
/// 由 PlayerBehavior 在 Init 时写入引用，LogicDestroy 时清空。
/// </summary>
public class PlayerRefHolder
{
    private static PlayerRefHolder _instance;
    public static PlayerRefHolder Instance => _instance ??= new PlayerRefHolder();

    public Player Player { get; set; }
}
