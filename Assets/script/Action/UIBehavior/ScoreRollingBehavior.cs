using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分数滚动行为（全局纯逻辑）：不需要预制体，代码自建容器。
/// 仅在 Playing 状态滚动；templateA 从 Resources 加载，无则不生成滚动物体。
/// </summary>
[AutoRegisterBehaviour("ScoreRolling")]
public class ScoreRollingBehavior : Objectbehavior
{
    public ScoreRollingBehavior() { isBuild = true; isGlobal = true; }

    private ScoreRollingData _data;
    private readonly List<RollingItem> _items = new List<RollingItem>();

    private class RollingItem
    {
        public GameObject Obj;
        public Transform Tf;
        public bool HasSpawnedNext;
    }

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _data = objectDate as ScoreRollingData ?? new ScoreRollingData();
        // 全局行为：代码自建容器，不依赖预制体子物体
        if (_data.parentRoot == null)
        {
            var root = new GameObject("ScoreRollingRoot");
            root.transform.SetParent(SingleUIMono.Instance.transform, false);
            _data.parentRoot = root;
            Debug.Log("[ScoreRolling] 自建容器 ScoreRollingRoot");
        }
    }

    public override void LogicAwake()
    {
        if (_data.parentRoot == null)
        {
            Debug.LogError("[ScoreRolling] LogicAwake 失败：parentRoot 为 null");
            return;
        }

        // templateA 走 ResCenter 加载，无则只更新分数不生成滚动物体
        if (_data.templateA == null)
        {
            var handle = ResCenter.Instance.LoadSync<GameObject>("ScoreRollingTemplate");
            if (handle != null) { _data.templateA = handle.AssetObj as GameObject; handle.Release(); }
        }

        Debug.Log($"[ScoreRolling] LogicAwake 开始，templateA={(_data.templateA != null ? _data.templateA.name : "无")}");

        if (_data.templateA != null)
        {
            _data.spawnOrigin = Vector3.zero;
            SpawnItem(_data.spawnOrigin, "_FirstInstance");
        }

        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
        _items.Clear();
    }

    private void SpawnItem(Vector3 localPos, string suffix)
    {
        if (_data.templateA == null || _data.parentRoot == null) return;
        GameObject go = Object.Instantiate(_data.templateA, _data.parentRoot.transform);
        go.transform.localPosition = localPos;
        go.SetActive(true);
        go.name = _data.templateA.name + suffix;
        _items.Add(new RollingItem { Obj = go, Tf = go.transform, HasSpawnedNext = false });
    }

    private void OnUpdate()
    {
        // 仅在游戏进行中滚动
        if (Global.CurrentState != GameEnum.Playing) return;

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            item.Tf.Translate(Vector3.left * _data.scrollSpeed * Time.deltaTime);

            if (!item.HasSpawnedNext && item.Tf.localPosition.x < _data.spawnTriggerX)
            {
                item.HasSpawnedNext = true;
                SpawnItem(_data.spawnOrigin, "_Instance");
            }

            if (item.Tf.localPosition.x < _data.destroyX)
            {
                Object.Destroy(item.Obj);
                _items.RemoveAt(i);
            }
        }
    }
}
