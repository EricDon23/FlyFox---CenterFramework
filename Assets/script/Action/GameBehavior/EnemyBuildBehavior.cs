using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成器：固定点位生成敌人，带警告提示和重叠检测。
/// 资源路径：Resources/Entity/Enemy/normal/、Resources/Entity/Enemy/war
/// </summary>
[AutoRegisterBehaviour("EnemyBuild")]
public class EnemyBuildBehavior : Objectbehavior
{
    public EnemyBuildBehavior() { isGlobal = true; isBuild = true; }

    private readonly List<GameObject> _activeEnemies = new List<GameObject>();
    private readonly List<GameObject> _activeWarnings = new List<GameObject>();
    private bool _gameRunning;
    private float _spawnTimer;
    private const float OverlapRadius = 1.5f;

    // 竖直位置档位：左、中左、中右、右
    private readonly float[] _verticalXSlots = { -5f, -2f, 2f, 5f };

    /// <summary>根据游戏时间计算当前速度（越玩越快）。</summary>
    private float GetCurrentSpeed()
    {
        // 基础速度 5f，每过 10 秒加 1f，上限 10f
        float speed = 5f + Time.timeSinceLevelLoad * 0.1f;
        return Mathf.Min(speed, 10f);
    }

    private static readonly string[] EnemyTypes = { "bettle1", "eagle2", "vulture1", "eagle1" };

    /// <summary>根据游戏时间随机选敌人类型（概率随时间变化）。</summary>
    private string PickEnemyType()
    {
        float time = Time.timeSinceLevelLoad;

        // 前 10 秒：每种各出现一个，按顺序
        if (time < 10f)
        {
            int index = Mathf.FloorToInt(time / 2.5f);
            return EnemyTypes[Mathf.Clamp(index, 0, 3)];
        }

        // 概率权重：甲虫最多，然后秃鹫和鹰一逐渐增多
        float bettleWeight = 8f;
        float eagle2Weight = 5f;  // 调回来，能看出效果
        float vultureWeight = Mathf.Min(3f + time * 0.1f, 6f);
        float eagle1Weight = Mathf.Min(2f + time * 0.08f, 5f);

        float total = bettleWeight + eagle2Weight + vultureWeight + eagle1Weight;
        float roll = UnityEngine.Random.Range(0f, total);

        if ((roll -= bettleWeight) < 0) return "bettle1";
        if ((roll -= eagle2Weight) < 0) return "eagle2";
        if ((roll -= vultureWeight) < 0) return "vulture1";
        return "eagle1";
    }

    public override void LogicAwake()
    {
        EventCenter.Instance.Subscribe("GameStart", OnGameStart);
        EventCenter.Instance.Subscribe("RequestGameOver", OnGameStop);
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
        Debug.Log("[EnemyBuild] 初始化完成");
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("GameStart", OnGameStart);
        EventCenter.Instance.Unsubscribe("RequestGameOver", OnGameStop);
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
        ClearAll();
    }

    private void OnGameStart()
    {
        _gameRunning = true;
        _spawnTimer = 0f;
        Debug.Log("[EnemyBuild] 开始生成敌人");
    }

    private void OnGameStop()
    {
        _gameRunning = false;
        ClearAll();
    }

    /// <summary>根据游戏时间计算生成间隔（越玩越快，密度越高）。</summary>
    private float GetSpawnInterval()
    {
        // 基础间隔 3f，每过 10 秒减 0.2f，最短 1f
        float interval = 3f - Time.timeSinceLevelLoad * 0.02f;
        return Mathf.Max(interval, 1f);
    }

    private void OnUpdate()
    {
        if (!_gameRunning || Global.CurrentState != GameEnum.Playing) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= GetSpawnInterval())
        {
            _spawnTimer = 0f;
            TrySpawnEnemy();
        }
    }

    private const int MaxEnemiesOnScreen = 6;  // 屏幕内最多同时存在 6 个敌人

    /// <summary>尝试生成一个敌人。</summary>
    private void TrySpawnEnemy()
    {
        // 屏幕内敌人数量检查
        int activeCount = 0;
        foreach (var e in _activeEnemies) if (e != null) activeCount++;
        if (activeCount >= MaxEnemiesOnScreen)
        {
            Debug.Log($"[EnemyBuild] 屏幕内敌人已达上限 {MaxEnemiesOnScreen}，跳过本次生成");
            return;
        }

        string type = PickEnemyType();

        // 秃鹫：先生成方向，再决定位置
        Vector2 dir = GetMoveDirection(type);
        Vector2 spawnPos = GetSpawnPosition(type, dir);

        // ① 敌人点位重叠检测
        if (IsPositionOccupied(spawnPos, OverlapRadius))
        {
            Debug.Log($"[EnemyBuild] 点位被占用，丢弃 {type}");
            return;
        }

        // 加载敌人预制体
        var handle = ResCenter.Instance.LoadSync<GameObject>($"Entity/Enemy/normal/{type}");
        if (handle?.AssetObj == null)
        {
            Debug.LogWarning($"[EnemyBuild] 找不到预制体：{type}");
            handle?.Release();
            return;
        }

        GameObject enemy = Object.Instantiate(handle.AssetObj as GameObject, spawnPos, Quaternion.identity);
        handle.Release();

        // 绑定 EnemyShipBehavior
        var behavior = FactoryCenter.Instance.CreateBehaviour<EnemyShipBehavior>(enemy, new EnemyShipData());
        float speed = GetCurrentSpeed();  // 越玩越快
        behavior.SetEnemyType(type, dir, speed);
        BehaviourRegistry.RegisterEnemy(enemy, behavior);

        enemy.tag = "Enemy";

        // 生成警告提示
        GameObject war = SpawnWarning(spawnPos, dir);
        if (war != null) behavior.BindWarning(war);

        _activeEnemies.Add(enemy);
        Debug.Log($"[EnemyBuild] 生成 {type} 于 {spawnPos}，方向={dir}");
    }

    /// <summary>获取敌人出生位置（视口边界外一点）。</summary>
    private Vector2 GetSpawnPosition(string type, Vector2 dir)
    {
        // 水平位置档位：上中下
        float[] ySlots = { 3f, 0f, -3f };
        float y = ySlots[UnityEngine.Random.Range(0, ySlots.Length)];

        // 竖直位置档位：左、中左、中右、右
        float x = _verticalXSlots[UnityEngine.Random.Range(0, _verticalXSlots.Length)];

        return type switch
        {
            "vulture1" => GetVultureSpawnPos(dir, x),  // 根据方向决定从哪进
            "eagle2" => GetEagle2SpawnPos(),  // 鹰二只在右侧区域出现
            _ => new Vector2(9f, y), // 右侧进入
        };
    }

    /// <summary>鹰二生成位置：相机偏上方，斜线经过可视区域。</summary>
    private Vector2 GetEagle2SpawnPos()
    {
        // 只选中右(2)和右(3)，从相机偏上方进入
        int rightOnlyIndex = UnityEngine.Random.Range(2, _verticalXSlots.Length);
        float x = _verticalXSlots[rightOnlyIndex];
        // y=6 而不是 8，确保斜线下移能经过可视区域
        return new Vector2(x, 6f);
    }

    /// <summary>秃鹫生成位置：向下飞从上方进，向上飞从下方进。</summary>
    private Vector2 GetVultureSpawnPos(Vector2 dir, float x)
    {
        // 方向向下 → 从上方生成；方向向上 → 从下方生成
        return dir == Vector2.down ? new Vector2(x, 8f) : new Vector2(x, -8f);
    }

    /// <summary>获取敌人移动方向。</summary>
    private Vector2 GetMoveDirection(string type)
    {
        return type switch
        {
            "vulture1" => Random.value > 0.5f ? Vector2.down : Vector2.up,
            _ => Vector2.left,
        };
    }

    /// <summary>点位是否被占用。</summary>
    private bool IsPositionOccupied(Vector2 pos, float radius)
    {
        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null) continue;
            if (Vector2.Distance(enemy.transform.position, pos) < radius) return true;
        }
        return false;
    }

    /// <summary>生成警告提示（和敌人同一条飞行轨迹，在镜头内边缘）。</summary>
    private GameObject SpawnWarning(Vector2 enemyPos, Vector2 dir)
    {
        var handle = ResCenter.Instance.LoadSync<GameObject>("Entity/Enemy/war");
        if (handle?.AssetObj == null)
        {
            Debug.LogWarning("[EnemyBuild] 找不到 war 警告预制体");
            handle?.Release();
            return null;
        }

        // 警告位置：和敌人同一条轨迹，只是在镜头内边缘
        Vector2 warnPos = GetWarningPositionOnPath(enemyPos, dir);
        GameObject war = Object.Instantiate(handle.AssetObj as GameObject, warnPos, Quaternion.identity);
        handle.Release();

        _activeWarnings.Add(war);
        Debug.Log($"[EnemyBuild] 警告生成于 {warnPos}（敌人轨迹上）");
        return war;
    }

    /// <summary>根据敌人位置和方向，计算同一条轨迹上的镜头内警告位置。</summary>
    private Vector2 GetWarningPositionOnPath(Vector2 enemyPos, Vector2 dir)
    {
        Camera cam = Camera.main;
        if (cam == null) return enemyPos;

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        // 水平飞行（方向向左）：警告在镜头右边缘内，Y 和敌人一样
        if (dir == Vector2.left)
            return new Vector2(horzExtent - 1f, enemyPos.y);

        // 垂直向下飞行：警告在镜头上边缘内，X 和敌人一样
        if (dir == Vector2.down)
            return new Vector2(enemyPos.x, vertExtent - 1f);

        // 垂直向上飞行：警告在镜头下边缘内，X 和敌人一样
        if (dir == Vector2.up)
            return new Vector2(enemyPos.x, -vertExtent + 1f);

        // 斜向飞行（鹰二，45° 向左下）：
        // 敌人在 (x=5, y=6)，方向 (-1,-1)
        // 计算斜线进入可视区域的第一个交点
        // 可视区域：x ∈ [-horz, horz], y ∈ [-vert, vert]
        // 斜线参数：x = enemyPos.x - t, y = enemyPos.y - t
        // 第一个交点：要么碰到右边界 x=horz，要么碰到上边界 y=vert
        // 因为敌人在上方偏右，先碰到上边界 y=vert
        float t = enemyPos.y - vertExtent;  // 从敌人 y 到上边界的距离
        float enterX = enemyPos.x - t;  // 进入可视区域时的 X 坐标
        return new Vector2(enterX, vertExtent - 0.5f);  // 上边界内一点
    }

    private void ClearAll()
    {
        foreach (var e in _activeEnemies) if (e != null) Object.Destroy(e);
        foreach (var w in _activeWarnings) if (w != null) Object.Destroy(w);
        _activeEnemies.Clear();
        _activeWarnings.Clear();
    }
}
