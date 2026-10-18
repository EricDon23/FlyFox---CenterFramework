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
    private TriggerBridge2D _triggerBridge;
    private Animator _animator;  // 玩家动画器
    private Vector3 _startPos;
    private float _invincibleTimer;
    private const float InvincibleDuration = 1f;
    private float _damageDelayTimer;  // 掉血延时
    private Collider2D _pendingDamageOther;  // 待销毁的敌人
    private Collider2D _currentEnemyCollider;  // 当前碰撞的敌人（待确认）

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _player = objectDate as Player ?? new Player();
        _action = new PlayerAction(_player, obj);
        _collider = obj.GetComponent<PolygonCollider2D>();
        _rb = obj.GetComponent<Rigidbody2D>();
        _animator = obj.GetComponentInChildren<Animator>();  // Animator 在子物体上
        _triggerBridge = obj.GetComponent<TriggerBridge2D>() ?? obj.AddComponent<TriggerBridge2D>();
        _triggerBridge.BindEnterEvent(OnTriggerEnter);
        Debug.Log($"[PlayerBehavior] 初始化完成，collider={(_collider != null ? "有" : "无")}，rb={(_rb != null ? "有" : "无")}，animator={(_animator != null ? "有" : "无")}");
        PlayerRefHolder.Instance.Player = _player;
    }

    public override void LogicAwake()
    {
        Debug.Log($"[PlayerBehavior] LogicAwake 初始化完成，HP={_player.Health}/{_player.MaxHealth}, g_obj={(g_obj != null ? g_obj.name : "null")}");
        EventCenter.Instance.Subscribe("FixedUpdateEvent", OnFixedUpdate);
        EventCenter.Instance.Subscribe("GameStart", OnGameStart);
        EventCenter.Instance.Subscribe("RequestGameOver", OnGameOver);
    }

    public override void LogicDestroy()
    {
        Debug.Log("[PlayerBehavior] LogicDestroy 销毁");
        EventCenter.Instance.Unsubscribe("FixedUpdateEvent", OnFixedUpdate);
        EventCenter.Instance.Unsubscribe("GameStart", OnGameStart);
        EventCenter.Instance.Unsubscribe("RequestGameOver", OnGameOver);
        if (_triggerBridge != null) _triggerBridge.UnbindEnterEvent(OnTriggerEnter);
        if (PlayerRefHolder.Instance.Player == _player)
            PlayerRefHolder.Instance.Player = null;
    }

    /// <summary>游戏开始：记录初始位置，重置血量和状态。</summary>
    private void OnGameStart()
    {
        if (g_obj != null)
        {
            _startPos = g_obj.transform.position;
            g_obj.transform.position = _startPos;
        }
        // 重置血量到满
        while (_player.Health < _player.MaxHealth) _player.Heal(1);
        _invincibleTimer = 0f;
        Debug.Log($"[PlayerBehavior] 游戏开始，位置重置={_startPos}，HP={_player.Health}");
    }

    /// <summary>游戏结束：恢复到初始位置。</summary>
    private void OnGameOver()
    {
        if (g_obj != null)
        {
            g_obj.transform.position = _startPos;
            Debug.Log($"[PlayerBehavior] 游戏结束，位置恢复={_startPos}");
        }
    }

    /// <summary>玩家碰撞检测：碰撞物品收集加分，碰撞敌人扣血。</summary>
    private void OnTriggerEnter(Collider2D other)
    {
        Debug.Log($"[PlayerBehavior] Trigger 碰撞 {other.name}，标签={other.tag}");

        // ① 碰撞物品：收集加分
        if (BehaviourRegistry.TryGetItem(other.gameObject, out var item))
        {
            Debug.Log("[PlayerBehavior] 碰到物品，调用 OnPlayerTouch");
            item.OnPlayerTouch(_player);
            BehaviourRegistry.UnregisterItem(other.gameObject);
            Object.Destroy(other.gameObject);
            return;
        }

        // ② 碰撞敌人：记录，下一帧确认是否还在敌人体内
        if (!other.CompareTag("Enemy"))
        {
            return;
        }
        if (_invincibleTimer > 0)
        {
            return;
        }

        // 记录当前碰撞的敌人，下一帧确认
        _currentEnemyCollider = other;
        Debug.Log($"[PlayerBehavior] 进入敌人范围，等待下一帧确认...");
    }

    /// <summary>在 FixedUpdate 里执行延时掉血。</summary>
    private void ProcessPendingDamage()
    {
        // 检查当前碰撞的敌人是否还在（确认逻辑）
        if (_currentEnemyCollider != null)
        {
            // 下一帧还在敌人体内，确认受伤
            _invincibleTimer = InvincibleDuration;
            _pendingDamageOther = _currentEnemyCollider;
            _currentEnemyCollider = null;

            Debug.Log($"[PlayerBehavior] 确认在敌人体内，触发 hurt 动画");

            // 1. 先触发动画
            if (_animator != null)
            {
                _animator.SetBool("hurt", true);
            }

            // 2. 标记延时掉血（极短，快速响应）
            _damageDelayTimer = 0.1f;
        }

        if (_damageDelayTimer <= 0) return;

        _damageDelayTimer -= Time.fixedDeltaTime;
        if (_damageDelayTimer > 0) return;

        // 执行掉血
        _player.ApplyDamage(1);
        int newHp = _player.Health;
        Debug.Log($"[PlayerBehavior] 掉血完成 HP={newHp}/{_player.MaxHealth}");
        EventCenter.Instance.Broadcast<object>("PlayerDamaged", newHp);

        // 销毁敌人
        if (_pendingDamageOther != null && BehaviourRegistry.TryGetEnemy(_pendingDamageOther.gameObject, out var enemy))
        {
            enemy.LogicDestroy();
            BehaviourRegistry.UnregisterEnemy(_pendingDamageOther.gameObject);
            Object.Destroy(_pendingDamageOther.gameObject);
        }

        // 判断 GameOver
        if (newHp <= 0)
        {
            EventCenter.Instance.Broadcast("RequestGameOver");
            UICenter.Instance.OpenUI("GameOver");
        }

        _damageDelayTimer = 0;
        _pendingDamageOther = null;
    }

    private void OnFixedUpdate()
    {
        if (g_obj == null) return;
        if (Global.CurrentState != GameEnum.Playing) return;

        // 处理延时掉血
        ProcessPendingDamage();

        if (_invincibleTimer > 0)
        {
            _invincibleTimer -= Time.fixedDeltaTime;
            // 无敌时间结束，恢复正常动画
            if (_invincibleTimer <= 0 && _animator != null)
            {
                _animator.SetBool("hurt", false);
                Debug.Log("[PlayerBehavior] 无敌结束，恢复正常动画");
            }
        }

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

        // 强制位置边界限制（双保险）
        ClampPositionToCameraBounds();

        _action.TryStartDash(_collider);
    }

    /// <summary>把玩家位置限制在相机可视范围内。</summary>
    private void ClampPositionToCameraBounds()
    {
        if (Camera.main == null || g_obj == null) return;

        float vertExtent = Camera.main.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        Vector3 pos = g_obj.transform.position;
        pos.x = Mathf.Clamp(pos.x, -horzExtent + 0.5f, horzExtent - 0.5f);
        pos.y = Mathf.Clamp(pos.y, -vertExtent + 0.5f, vertExtent - 0.5f);
        g_obj.transform.position = pos;
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
