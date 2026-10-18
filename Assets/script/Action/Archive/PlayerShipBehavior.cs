using UnityEngine;

/// <summary>
/// 玩家飞机行为：订阅 UpdateEvent，读 InputCenter 轴移动并夹紧边界；
/// 按 fireInterval 自动向右发射玩家子弹；被敌机弹命中扣血，血量归零广播 GameOver。
/// </summary>
[AutoRegisterBehaviour("PlayerShip")]
public class PlayerShipBehavior : Objectbehavior
{
    private PlayerShipData _data;
    private float _fireTimer;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _data = objectDate as PlayerShipData ?? new PlayerShipData();
    }

    public override void LogicAwake()
    {
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
        EventCenter.Instance.Subscribe<object>("PlayerShip_Hit", OnHit);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
        EventCenter.Instance.Unsubscribe<object>("PlayerShip_Hit", OnHit);
    }

    private void OnUpdate()
    {
        if (Global.CurrentState != GameEnum.Playing) return;
        if (g_obj == null) return;

        // 移动：Horizontal/Axis，上下左右
        float h = InputCenter.Instance.GetAxisRaw("Horizontal");
        float v = InputCenter.Instance.GetAxisRaw("Vertical");
        Vector3 pos = g_obj.transform.position;
        pos.x += h * _data.moveSpeed * Time.deltaTime;
        pos.y += v * _data.moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, _data.leftBound, _data.rightBound);
        pos.y = Mathf.Clamp(pos.y, _data.bottomBound, _data.topBound);
        g_obj.transform.position = pos;

        // 自动射击
        _fireTimer += Time.deltaTime;
        if (_fireTimer >= _data.fireInterval)
        {
            _fireTimer = 0f;
            SpawnBullet(1f);
        }
    }

    private void SpawnBullet(float dirX)
    {
        Vector3 spawnPos = g_obj.transform.position + new Vector3(0.5f, 0f, 0f);
        EventCenter.Instance.Broadcast("FireBullet", new FireRequest(spawnPos, dirX, _data.bulletSpeed));
    }

    private void OnHit(object damageObj)
    {
        int dmg = damageObj is int i ? i : 1;
        _data.ApplyDamage(dmg);
        EventCenter.Instance.Broadcast<int>("PlayerHealthChanged", _data.currentHealth);
        if (_data.currentHealth <= 0)
        {
            EventCenter.Instance.Broadcast("RequestGameOver");
        }
    }
}
