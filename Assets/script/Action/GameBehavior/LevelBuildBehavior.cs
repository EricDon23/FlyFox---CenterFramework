using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡构建行为：纯逻辑行为（isGlobal=true），不需要预制体。
/// 逻辑：开始游戏后从右侧镜头外持续生成 Cherry/Coin/Gem，
/// 物品自动左移进入镜头，玩家触碰得分，出左边界自动销毁。
/// </summary>
[AutoRegisterBehaviour("LevelBuild")]
public class LevelBuildBehavior : Objectbehavior
{
    public LevelBuildBehavior() { isBuild = true; isGlobal = true; }

    private SpawnPositionData _spawnData;
    private Transform _levelRoot;
    private float _playerHp = 100f;
    private float _playerMaxHp = 100f;
    private bool _gameStarted;
    private float _spawnTimer;
    private float _nextSpawnInterval = 1.5f;

    private readonly Dictionary<GridType, GameObject> _itemPrefabs = new Dictionary<GridType, GameObject>();
    private readonly List<GameObject> _activeItems = new List<GameObject>();

    private static readonly Dictionary<ScoreShape, int> _shapeCounts = new Dictionary<ScoreShape, int>();

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

        // 订阅开始游戏事件，开始后才生成物品
        EventCenter.Instance.Subscribe("GameStart", OnGameStart);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("GameStart", OnGameStart);
        EventCenter.Instance.Unsubscribe("FixedUpdateEvent", OnFixedUpdate);
        ClearAllItems();
    }

    private void OnGameStart()
    {
        Debug.Log("[LevelBuild] 游戏开始，开始生成物品");
        _gameStarted = true;
        _spawnTimer = 0f;
        EventCenter.Instance.Subscribe("FixedUpdateEvent", OnFixedUpdate);
    }

    private void OnFixedUpdate()
    {
        if (!_gameStarted) return;

        // 生成计时：每隔一段时间在右侧生成一组物品
        _spawnTimer += Time.fixedDeltaTime;
        if (_spawnTimer >= _nextSpawnInterval)
        {
            _spawnTimer = 0f;
            SpawnItemGroup();
            _nextSpawnInterval = UnityEngine.Random.Range(1.0f, 2.5f);
        }

        // 物品左移 + 出界销毁
        float moveSpeed = 3f;
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            var item = _activeItems[i];
            if (item == null) { _activeItems.RemoveAt(i); continue; }
            item.transform.Translate(Vector3.left * moveSpeed * Time.fixedDeltaTime);
            if (item.transform.position.x < -10f)
            {
                UnityEngine.Object.Destroy(item);
                _activeItems.RemoveAt(i);
            }
        }
    }

    /// <summary>在右侧镜头外生成一组物品（按形状排列）。</summary>
    private void SpawnItemGroup()
    {
        if (_levelRoot == null) return;

        // 右侧生成位置（镜头外）
        float spawnX = 12f;
        float spawnY = UnityEngine.Random.Range(-3f, 3f);

        ScoreShape shape = PickBalancedShape();
        int length = UnityEngine.Random.Range(3, 6);

        for (int i = 0; i < length; i++)
        {
            float x = spawnX + i;
            float y = spawnY;
            switch (shape)
            {
                case ScoreShape.vertical: y = spawnY + i * 0.8f; break;
                case ScoreShape.diagonal_left: y = spawnY - i * 0.8f; break;
                case ScoreShape.diagonal_right: y = spawnY + i * 0.8f; break;
            }

            GridType type = PickItemType(Global.Level, _playerHp, _playerMaxHp);
            SpawnSingleItem(type, new Vector2(x, y));
        }
    }

    private void SpawnSingleItem(GridType type, Vector2 position)
    {
        if (!_itemPrefabs.TryGetValue(type, out var prefab) || prefab == null) return;

        GameObject go = UnityEngine.Object.Instantiate(prefab, _levelRoot);
        go.transform.position = position;
        go.name = $"{type}_{UnityEngine.Random.Range(0, 9999)}";
        go.SetActive(true);

        // 碰撞体
        if (go.GetComponent<Collider2D>() == null)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        // 碰撞桥接
        var bridge = go.GetComponent<ItemTriggerBridge>();
        if (bridge == null) bridge = go.AddComponent<ItemTriggerBridge>();
        if (bridge != null)
        {
            var fruitData = new Fruit { Type = type };
            var itemBehavior = FactoryCenter.Instance.CreateBehaviour<ItemBehavior>(go, fruitData);
            bridge.Bind(itemBehavior);
        }

        _activeItems.Add(go);
    }

    private static GridType PickItemType(int level, float hp, float maxHp)
    {
        float hpRatio = hp / maxHp;
        if (UnityEngine.Random.value < (hpRatio < 0.3f ? 0.4f : 0.1f))
            return GridType.Cherry;

        float coinProb = level >= 5 ? 0.5f : 0.8f;
        return UnityEngine.Random.value < coinProb ? GridType.Coin : GridType.Gem;
    }

    private static ScoreShape PickBalancedShape()
    {
        InitializeShapeCounts();
        int min = int.MaxValue;
        var candidates = new List<ScoreShape>();
        foreach (var kv in _shapeCounts)
        {
            if (kv.Value < min) { min = kv.Value; candidates.Clear(); candidates.Add(kv.Key); }
            else if (kv.Value == min) candidates.Add(kv.Key);
        }
        ScoreShape picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _shapeCounts[picked]++;
        return picked;
    }

    private static void InitializeShapeCounts()
    {
        _shapeCounts.Clear();
        foreach (ScoreShape s in Enum.GetValues(typeof(ScoreShape)))
            _shapeCounts[s] = 0;
    }

    private void ClearAllItems()
    {
        foreach (var item in _activeItems)
            if (item != null) UnityEngine.Object.Destroy(item);
        _activeItems.Clear();
    }
}
