using UnityEngine;

/// <summary>
/// 敌人生成器（全局行为，isGlobal=true）：Playing 状态下每隔 spawnInterval 从右侧生成一架敌机。
/// 不挂 GameObject，纯逻辑；通过 EventCenter 广播 SpawnEnemy 请求由工厂实例化。
/// </summary>
[AutoRegisterBehaviour("EnemySpawner")]
public class EnemySpawnerBehavior : Objectbehavior
{
    private float _timer;
    private const float SpawnInterval = 1.5f;

    public EnemySpawnerBehavior()
    {
        isGlobal = true;
        isBuild = true;
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
        if (Global.CurrentState != GameEnum.Playing) return;
        _timer += Time.deltaTime;
        if (_timer >= SpawnInterval)
        {
            _timer = 0f;
            float y = Random.Range(-3f, 3f);
            Vector3 pos = new Vector3(10f, y, 0f);
            EventCenter.Instance.Broadcast<Vector3>("SpawnEnemy", pos);
        }
    }
}
