using UnityEngine;

/// <summary>
/// 敌机行为：根据 enemyType 执行差异化移动逻辑。
/// bettle1 水平左移 / eagle1 Sin曲线 / vulture1 垂直移动 / eagle2 45°斜移
/// </summary>
[AutoRegisterBehaviour("EnemyShip")]
public class EnemyShipBehavior : Objectbehavior
{
    private string _enemyType;
    private float _moveSpeed = 5f;
    private float _sinAmplitude = 2.5f;  // sin 振幅调大
    private float _sinPeriod = 2f;
    private float _sinTimer;
    private Vector2 _moveDir = Vector2.left;
    private GameObject _warWarning; // 绑定的警告预制体

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        var data = objectDate as EnemyShipData;
        if (data != null)
        {
            _moveSpeed = data.moveSpeed;
        }
    }

    /// <summary>设置敌人类型和参数（由 EnemyBuild 调用）。</summary>
    public void SetEnemyType(string type, Vector2 dir, float speed)
    {
        _enemyType = type;
        _moveDir = dir;
        _moveSpeed = speed;
        _sinTimer = 0f;
    }

    /// <summary>绑定警告预制体，敌人销毁时同步销毁。</summary>
    public void BindWarning(GameObject war) => _warWarning = war;

    public override void LogicAwake()
    {
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
        Debug.Log($"[EnemyShip] LogicAwake 完成，type={_enemyType}");
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
        Debug.Log($"[EnemyShip] LogicDestroy，type={_enemyType}，销毁 war 警告");
        if (_warWarning != null) Object.Destroy(_warWarning);
    }

    private void OnUpdate()
    {
        if (Global.CurrentState != GameEnum.Playing || g_obj == null) return;

        _sinTimer += Time.deltaTime;
        Vector3 oldPos = g_obj.transform.position;

        switch (_enemyType)
        {
            case "bettle1":
                g_obj.transform.Translate(Vector3.left * _moveSpeed * Time.deltaTime);
                break;
            case "eagle1":
                float yOffset = Mathf.Sin(_sinTimer * Mathf.PI / _sinPeriod) * _sinAmplitude;
                g_obj.transform.Translate(new Vector3(-_moveSpeed * Time.deltaTime, yOffset * Time.deltaTime, 0));
                break;
            case "vulture1":
                g_obj.transform.Translate((Vector3)_moveDir * _moveSpeed * Time.deltaTime);
                break;
            case "eagle2":
                g_obj.transform.Translate(new Vector3(-_moveSpeed, -_moveSpeed, 0) * Time.deltaTime);
                break;
        }

        // 出视口销毁
        if (Mathf.Abs(g_obj.transform.position.x) > 12f || g_obj.transform.position.y > 10f || g_obj.transform.position.y < -8f)
        {
            Debug.Log($"[EnemyShip] {_enemyType} 出视口，位置={g_obj.transform.position}，销毁");
            LogicDestroy();
            Object.Destroy(g_obj);
        }
    }
}
