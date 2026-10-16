using UnityEngine;

/// <summary>
/// 敌机行为：从右侧生成，向左匀速飞行；到达左边界销毁；
/// 定时向左发射敌机子弹；被击毁时广播加分事件。
/// </summary>
[AutoRegisterBehaviour("EnemyShip")]
public class EnemyShipBehavior : Objectbehavior
{
    private EnemyShipData _data;
    private float _fireTimer;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _data = objectDate as EnemyShipData ?? new EnemyShipData();
    }

    public override void LogicAwake()
    {
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
        EventCenter.Instance.Subscribe<object>("EnemyShip_Hit", OnHit);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
        EventCenter.Instance.Unsubscribe<object>("EnemyShip_Hit", OnHit);
    }

    private void OnUpdate()
    {
        if (Global.CurrentState != GameEnum.Playing) return;
        if (g_obj == null) return;

        g_obj.transform.Translate(Vector3.left * _data.moveSpeed * Time.deltaTime);

        // 自动射击
        _fireTimer += Time.deltaTime;
        if (_fireTimer >= _data.fireInterval)
        {
            _fireTimer = 0f;
            Vector3 spawnPos = g_obj.transform.position + new Vector3(-0.5f, 0f, 0f);
            EventCenter.Instance.Broadcast("FireEnemyBullet", new FireRequest(spawnPos, -1f, 6f));
        }

        // 飞出左边界
        if (g_obj.transform.position.x < _data.leftKillX)
        {
            Object.Destroy(g_obj);
        }
    }

    private void OnHit(object damageObj)
    {
        int dmg = damageObj is int i ? i : 1;
        _data.currentHealth -= dmg;
        if (_data.currentHealth <= 0)
        {
            EventCenter.Instance.Broadcast<int>("EnemyKilled", _data.scoreValue);
            Object.Destroy(g_obj);
        }
    }
}
