using UnityEngine;

/// <summary>
/// 玩家行为（横版跑酷）：订阅 FixedUpdateEvent，经 InputCenter 读轴移动，边界夹紧。
/// 冲刺状态机由 PlayerAction 纯计算类承载，行为只负责注入输入与执行位移。
/// </summary>
[AutoRegisterBehaviour("Player")]
public class PlayerBehavior : Objectbehavior
{
    public PlayerBehavior() { isBuild = true; }

    private Player _player;
    private PlayerAction _action;
    private PolygonCollider2D _collider;
    private Rigidbody2D _rb;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _player = objectDate as Player ?? new Player();
        _action = new PlayerAction(_player, obj);
        _collider = obj.GetComponent<PolygonCollider2D>();
        _rb = obj.GetComponent<Rigidbody2D>();
        // 【改造】注册玩家数据引用，供物品碰撞桥接查询；遵循框架规则：被动回调通过共享引用访问，不走事件。
        PlayerRefHolder.Instance.Player = _player;
    }

    public override void LogicAwake()
    {
        Debug.Log($"[PlayerBehavior] LogicAwake 初始化完成，HP={_player.Health}/{_player.MaxHealth}, g_obj={(g_obj != null ? g_obj.name : "null")}");
        EventCenter.Instance.Subscribe("FixedUpdateEvent", OnFixedUpdate);
    }

    public override void LogicDestroy()
    {
        Debug.Log("[PlayerBehavior] LogicDestroy 销毁");
        EventCenter.Instance.Unsubscribe("FixedUpdateEvent", OnFixedUpdate);
        if (PlayerRefHolder.Instance.Player == _player)
            PlayerRefHolder.Instance.Player = null;
    }

    private void OnFixedUpdate()
    {
        if (g_obj == null) return;
        if (Global.CurrentState != GameEnum.Playing) return;

        _action.Tick(Time.fixedDeltaTime);

        if (_action.isDashing && _rb != null)
        {
            _rb.MovePosition(Vector2.MoveTowards(_rb.position, _action.dashTarget, _player.DashSpeed * Time.fixedDeltaTime));
            if (Vector2.Distance(_rb.position, _action.dashTarget) <= _player.DASHSTOPTHRESHOLD)
                _action.isDashing = false;
            return;
        }

        Vector2 inputDir = InputCenter.Instance.GetMoveVector().normalized;
        float moveDist = _player.MoveSpeed * Time.fixedDeltaTime;
        Vector3 delta = (Vector3)inputDir * moveDist;

        if (_collider == null || !BaseUtils.WouldPolygonExceedBounds(_collider, delta))
        {
            _action.Move();
        }
        else
        {
            _action.Stop();
        }

        _action.TryStartDash(_collider);
    }

    /// <summary>外部读取：当前已用 Dash 次数。</summary>
    public int RemainingDashUsed => _action != null ? _player.MaxDashCount - _action.RemainingDash : 0;

    /// <summary>外部调用：玩家受伤，HP-1，广播最新血量。</summary>
    public void TakeDamage(int dmg)
    {
        if (_player == null) return;
        _player.ApplyDamage(dmg);
        Debug.Log($"[PlayerBehavior] 受到伤害 -{dmg}，HP={_player.Health}/{_player.MaxHealth}");
        EventCenter.Instance.Broadcast<object>("PlayerDamaged", _player.Health);
    }
}
