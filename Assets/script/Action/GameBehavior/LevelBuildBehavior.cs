using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡构建行为（波次驱动）：参考主流飞行游戏（雷霆战机/飞机大战）的关卡生成方式。
/// 核心逻辑：
/// 1. 时间轴驱动：游戏开始后按 Wave 波次生成，每波有固定编队配置
/// 2. 难度递增：波次越高，生成速度越快、编队越复杂、高价值奖励越多
/// 3. 经典编队：横排阵、竖列阵、V字阵、散点阵，交替出现保持节奏
/// 4. 奖励节奏：低血量时提高 Cherry（治疗）出现概率
/// </summary>
[AutoRegisterBehaviour("LevelBuild")]
public class LevelBuildBehavior : Objectbehavior
{
    public LevelBuildBehavior() { isBuild = true; isGlobal = true; }

    private SpawnPositionData _spawnData;
    private Transform _levelRoot;
    private bool _gameStarted;

    // 波次系统
    private int _currentWave;
    private float _waveTimer;          // 当前波次已进行时间
    private float _waveDuration;      // 当前波次持续时间
    private float _waveBreakTimer;    // 波次间隔计时
    private bool _waveBreak;          // 是否处于波次间隔

    // 物品生成
    private float _itemSpawnTimer;
    private float _itemsPerWave;      // 本波次计划生成组数
    private int _itemsSpawned;        // 本波次已生成组数
    private float _waveBaseY;         // 本波次统一 Y 基准

    // 难度参数
    private const float BaseMoveSpeed = 3f;
    private const float Wave1Duration = 8f;
    private const float WaveBreakTime = 2f;

    private readonly Dictionary<GridType, GameObject> _itemPrefabs = new Dictionary<GridType, GameObject>();
    private readonly List<GameObject> _activeItems = new List<GameObject>();

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _spawnData = objectDate as SpawnPositionData
            ?? ConfigCenter.Instance.GetConfig("LevelBuild") as SpawnPositionData
            ?? new SpawnPositionData();
        LoadItemPrefabs();
    }

    private void LoadItemPrefabs()
    {
        LoadPrefab(GridType.Cherry, "Cherry");
        LoadPrefab(GridType.Coin, "Coin");
        LoadPrefab(GridType.Gem, "Gem");
    }

    private void LoadPrefab(GridType type, string resourceName)
    {
        var handle = ResCenter.Instance.LoadSync<GameObject>(resourceName);
        GameObject prefab = handle?.AssetObj as GameObject;
        if (prefab != null) { _itemPrefabs[type] = prefab; handle.Release(); return; }

        Debug.LogWarning($"[LevelBuild] 未找到 {resourceName}.prefab，自动创建简易替代物体");
        var fallback = new GameObject(resourceName);
        var sr = fallback.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = type == GridType.Cherry ? Color.red : type == GridType.Coin ? Color.yellow : Color.cyan;
        var col = fallback.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        fallback.SetActive(false);
        _itemPrefabs[type] = fallback;
    }

    public override void LogicAwake()
    {
        if (_spawnData == null) { Debug.LogError("[LevelBuild] LogicAwake 失败：_spawnData 为 null"); return; }

        Debug.Log("[LevelBuild] LogicAwake 完成，等待 GameStart 事件");
        var levelGo = new GameObject("LevelRoot");
        _levelRoot = levelGo.transform;

        EventCenter.Instance.Subscribe("GameStart", OnGameStart);
        EventCenter.Instance.Subscribe("FixedUpdateEvent", OnFixedUpdate); // 只订阅一次
        EventCenter.Instance.Subscribe("RequestGameOver", OnGameOver);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("GameStart", OnGameStart);
        EventCenter.Instance.Unsubscribe("FixedUpdateEvent", OnFixedUpdate);
        EventCenter.Instance.Unsubscribe("RequestGameOver", OnGameOver);
        ClearAllItems();
    }

    private void OnGameStart()
    {
        Debug.Log("[LevelBuild] 游戏开始，启动波次系统");
        _gameStarted = true;
        _currentWave = 0;
        _waveBreak = false;
        _waveBreakTimer = 0f;
        ClearAllItems();
        StartNextWave();
    }

    /// <summary>退出游戏：停止生成，清理所有物品，重置状态。</summary>
    private void OnGameOver()
    {
        Debug.Log("[LevelBuild] 游戏结束，清理关卡");
        _gameStarted = false;
        _currentWave = 0;
        ClearAllItems();
    }

    /// <summary>根据波次编号计算难度参数。</summary>
    private void StartNextWave()
    {
        _currentWave++;
        _waveTimer = 0f;
        _itemsSpawned = 0;

        // 难度曲线：每波持续时间递增，生成组数递增
        _waveDuration = Wave1Duration + _currentWave * 2f;
        _itemsPerWave = 3 + _currentWave * 2;

        Global.Level = _currentWave;
        _waveBaseY = UnityEngine.Random.Range(-3f, 3f); // 本波次所有编队共用同一 Y
        Debug.Log($"[LevelBuild] === 第 {_currentWave} 波开始（{_itemsPerWave} 组物品，baseY={_waveBaseY:F2}）===");
        EventCenter.Instance.Broadcast("WaveChanged", _currentWave);
    }

    private void OnFixedUpdate()
    {
        if (!_gameStarted) return;
        // stop 状态冻结关卡逻辑，UI 仍运行
        if (Global.CurrentState != GameEnum.Playing) return;

        float dt = Time.fixedDeltaTime;

        // 波次间隔阶段
        if (_waveBreak)
        {
            _waveBreakTimer += dt;
            if (_waveBreakTimer >= WaveBreakTime)
            {
                _waveBreak = false;
                StartNextWave();
            }
        }
        else
        {
            // 当前波次进行中
            _waveTimer += dt;

            // 波次内按节奏生成物品组
            float spawnInterval = Mathf.Max(0.6f, 2.0f - _currentWave * 0.15f);
            _itemSpawnTimer += dt;
            if (_itemSpawnTimer >= spawnInterval && _itemsSpawned < _itemsPerWave)
            {
                _itemSpawnTimer = 0f;
                SpawnFormation();
                _itemsSpawned++;
            }

            // 波次结束
            if (_waveTimer >= _waveDuration || _itemsSpawned >= _itemsPerWave)
            {
                _waveBreak = true;
                _waveBreakTimer = 0f;
                Debug.Log($"[LevelBuild] === 第 {_currentWave} 波结束，{WaveBreakTime}s 后下一波 ===");
            }
        }

        // 物品左移 + 出界销毁（速度随波次微增）
        float moveSpeed = BaseMoveSpeed + _currentWave * 0.2f;
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            var item = _activeItems[i];
            if (item == null) { _activeItems.RemoveAt(i); continue; }
            item.transform.Translate(Vector3.left * moveSpeed * dt);
            if (item.transform.position.x < -10f) // 移出左边界销毁
            {
                UnityEngine.Object.Destroy(item);
                _activeItems.RemoveAt(i);
            }
        }
    }

    private List<FormationDef> _formations;

    /// <summary>按经典编队生成一组物品。</summary>
    // 水平位置档位：上中下
    private readonly float[] _levelYSlots = { 3f, 0f, -3f };

    private void SpawnFormation()
    {
        if (_levelRoot == null) return;
        if (_formations == null || _formations.Count == 0)
            _formations = FormationLibrary.GetAllFormations();

        float spawnX = 10f; // 右侧屏幕外一点
        // 随机选一个水平档位（上/中/下）
        float baseY = _levelYSlots[UnityEngine.Random.Range(0, _levelYSlots.Length)];

        // 按权重随机选形状
        FormationDef formation = PickWeightedFormation();
        Debug.Log($"[LevelBuild] 生成形状：{formation.Name}，baseY={baseY:F2}");

        foreach (var local in formation.Points)
        {
            GridType type = PickItemType(_currentWave);
            SpawnSingleItem(type, new Vector2(spawnX + local.x, baseY + local.y));
        }
    }

    /// <summary>按权重随机选编队。</summary>
    private FormationDef PickWeightedFormation()
    {
        int totalWeight = 0;
        foreach (var f in _formations) totalWeight += f.Weight;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (var f in _formations)
        {
            roll -= f.Weight;
            if (roll < 0) return f;
        }
        return _formations[0];
    }

    /// <summary>按波次和血量选择物品类型。</summary>
    private GridType PickItemType(int wave)
    {
        // Cherry（治疗）：低血量时概率高
        var player = PlayerRefHolder.Instance.Player;
        float hpRatio = player != null ? (float)player.Health / player.MaxHealth : 1f;
        if (hpRatio < 0.3f && UnityEngine.Random.value < 0.4f)
            return GridType.Cherry;

        // Coin/Gem：高波次 Gem 概率提升
        float gemProb = Mathf.Clamp(0.1f + wave * 0.05f, 0.1f, 0.4f);
        return UnityEngine.Random.value < gemProb ? GridType.Gem : GridType.Coin;
    }

    /// <summary>不同物品的 pivot 补偿（视觉对齐用）。</summary>
    private static readonly System.Collections.Generic.Dictionary<GridType, float> YOffset =
        new System.Collections.Generic.Dictionary<GridType, float>
        {
            { GridType.Coin, 0f },    // 基准
            { GridType.Gem, 0.42f },  // Gem pivot 偏下，视觉上移补偿
            { GridType.Cherry, 0f },
        };

    private void SpawnSingleItem(GridType type, Vector2 position)
    {
        if (!_itemPrefabs.TryGetValue(type, out var prefab) || prefab == null) return;

        float yCompensate = YOffset.TryGetValue(type, out var off) ? off : 0f;
        Vector2 spawnPos = new Vector2(position.x, position.y + yCompensate);

        GameObject go = UnityEngine.Object.Instantiate(prefab, _levelRoot);
        go.transform.position = spawnPos;
        go.name = $"{type}_W{_currentWave}_{UnityEngine.Random.Range(0, 9999)}";
        go.SetActive(true);

        if (go.GetComponent<Collider2D>() == null)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        var fruitData = new Fruit { Type = type };
        var itemBehavior = FactoryCenter.Instance.CreateBehaviour<ItemBehavior>(go, fruitData);
        BehaviourRegistry.RegisterItem(go, itemBehavior);

        _activeItems.Add(go);
    }

    private void ClearAllItems()
    {
        foreach (var item in _activeItems)
            if (item != null) UnityEngine.Object.Destroy(item);
        _activeItems.Clear();
    }

    private enum FormationType
    {
        Horizontal,  // 横排
        Vertical,    // 竖列
        VShape,      // V字
        Scatter,     // 散点
    }
}
