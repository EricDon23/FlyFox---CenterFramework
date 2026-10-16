using UnityEngine;

/// <summary>
/// 玩家动作纯计算类：不继承 MonoBehaviour，不持事件订阅，由 PlayerBehavior 每帧驱动。
/// 职责：移动方向采样、冲刺状态机（计数/冷却/恢复）、边界外拦截判断。
/// </summary>
public class PlayerAction
{
    private readonly Player _player;
    private readonly Transform _transform;
    private readonly Rigidbody2D _rb;

    public bool isDashing;
    public Vector2 dashTarget;

    private int _usedDashCount;
    private float _dashIntervalTimer;
    private float _dashRecoverTimer;

    private const float INPUT_THRESHOLD = 0.0001f;
    private const float DASH_INPUT_THRESHOLD = 0.1f;
    private const float DASH_DISTANCE = 2f;

    public PlayerAction(Player player, GameObject host)
    {
        _player = player;
        _transform = host.transform;
        _rb = host.GetComponent<Rigidbody2D>();
    }

    public void Tick(float deltaTime)
    {
        if (_dashIntervalTimer > 0f) _dashIntervalTimer -= deltaTime;
        if (_usedDashCount <= 0) return;

        if (_dashRecoverTimer > 0f)
        {
            _dashRecoverTimer -= deltaTime;
        }
        else
        {
            _usedDashCount--;
            if (_usedDashCount > 0) _dashRecoverTimer = _player.DashRecoverTime;
            else _dashRecoverTimer = 0f;
        }
    }

    public void Move()
    {
        Vector2 dir = new Vector2(
            InputCenter.Instance.GetAxisRaw("Horizontal"),
            InputCenter.Instance.GetAxisRaw("Vertical"));

        if (dir.sqrMagnitude < INPUT_THRESHOLD)
        {
            if (_rb != null) _rb.velocity = Vector2.zero;
            return;
        }

        dir.Normalize();
        float dist = _player.MoveSpeed * Time.fixedDeltaTime;

        if (_rb != null) _rb.MovePosition(_rb.position + dir * dist);
        else _transform.Translate((Vector3)dir * dist);
    }

    public void Stop()
    {
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    public void TryStartDash(PolygonCollider2D collider)
    {
        if (isDashing || _usedDashCount >= _player.MaxDashCount || _dashIntervalTimer > 0f) return;
        if (!InputCenter.Instance.GetKeyHold("Dash")) return;

        Vector2 dir = new Vector2(
            InputCenter.Instance.GetAxisRaw("Horizontal"),
            InputCenter.Instance.GetAxisRaw("Vertical")).normalized;

        if (dir.sqrMagnitude < DASH_INPUT_THRESHOLD) return;

        Vector2 target = (Vector2)_transform.position + dir * DASH_DISTANCE;
        if (collider != null && BaseUtils.WouldPolygonExceedBounds(collider, (Vector3)target - _transform.position))
            return;

        _usedDashCount++;
        _dashIntervalTimer = _player.DashInterval;
        _dashRecoverTimer = _player.DashRecoverTime;
        dashTarget = target;
        isDashing = true;
    }
}
