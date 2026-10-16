using UnityEngine;

/// <summary>
/// 子弹行为：直线飞行，按 directionX 方向；飞出左右边界销毁。
/// 碰撞由调用方在 OnTriggerEnter2D 里转发事件（PlayerShip_Hit / EnemyShip_Hit）。
/// </summary>
[AutoRegisterBehaviour("Bullet")]
public class BulletBehavior : Objectbehavior
{
    private BulletData _data;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _data = objectDate as BulletData ?? new BulletData();
    }

    public override void LogicAwake()
    {
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
    }

    private void OnUpdate()
    {
        if (g_obj == null) return;
        g_obj.transform.Translate(new Vector3(_data.directionX * _data.speed * Time.deltaTime, 0f, 0f));

        float x = g_obj.transform.position.x;
        if ((_data.directionX > 0 && x > _data.killX) ||
            (_data.directionX < 0 && x < _data.killLeftX))
        {
            Object.Destroy(g_obj);
        }
    }
}
