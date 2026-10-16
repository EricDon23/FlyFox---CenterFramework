using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视差背景行为：订阅 FixedUpdateEvent 驱动多层背景滚动。
/// 预制体 BackgroundParallax.prefab 下设 3 层背景，每层下挂背景块子物体。
/// 注意：GlobalConter 在 BeforeSceneLoad 阶段启动，此时场景未加载，
/// Camera.main/FindObjectOfType 都返回 null；相机查找延迟到 OnFixedUpdate 首次执行时。
/// </summary>
[AutoRegisterBehaviour("BackgroundParallax")]
public class BackgroundParallaxBehavior : Objectbehavior
{
    public BackgroundParallaxBehavior() { isBuild = true; }

    private BackgroundParallaxData _data;
    private readonly List<BackgroundParallaxData.BackgroundLayer> _layers = new List<BackgroundParallaxData.BackgroundLayer>();
    private bool _cameraReady;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        _data = objectDate as BackgroundParallaxData ?? new BackgroundParallaxData();
        _data.backgroundRoot = obj;
        // 不在此处调 Camera.main——BeforeSceneLoad 阶段场景未加载，必返回 null。
    }

    public override void LogicAwake()
    {
        if (_data.backgroundRoot == null)
        {
            Debug.LogError("[BackgroundParallax] LogicAwake 失败：backgroundRoot 为 null");
            return;
        }

        _data.rootTransform = _data.backgroundRoot.transform;

        int childCount = _data.rootTransform.childCount;
        Debug.Log($"[BackgroundParallax] LogicAwake 开始，{childCount} 层背景，root={_data.backgroundRoot.name}");

        for (int i = 0; i < childCount; i++)
        {
            Transform layerTf = _data.rootTransform.GetChild(i);
            var layer = new BackgroundParallaxData.BackgroundLayer
            {
                layerTransform = layerTf,
                layerSpeed = _data.baseScrollSpeed * (childCount - i)
            };

            for (int j = 0; j < layerTf.childCount; j++)
            {
                Transform blockTf = layerTf.GetChild(j);
                layer.blocks.Add(new BackgroundParallaxData.BackgroundBlock
                {
                    gameObject = blockTf.gameObject,
                    transform = blockTf,
                    localScale = blockTf.localScale,
                    hasSpawned = false
                });
            }

            SpriteRenderer sr = layer.blocks.Count > 0 ? layer.blocks[0].gameObject.GetComponent<SpriteRenderer>() : null;
            layer.backgroundWidth = sr != null ? sr.bounds.size.x : 100f;
            _layers.Add(layer);
        }

        EventCenter.Instance.Subscribe("FixedUpdateEvent", OnFixedUpdate);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("FixedUpdateEvent", OnFixedUpdate);
        foreach (var layer in _layers)
        {
            if (layer.layerTransform != null)
                AssetPool.Instance.ClearPool(layer.layerTransform.name);
        }
        _layers.Clear();
    }

    /// <summary>首次 OnFixedUpdate 时场景已加载，此时查找相机。</summary>
    private void EnsureCamera()
    {
        if (_cameraReady) return;

        _data.usedCamera = Camera.main;
        if (_data.usedCamera == null)
        {
            var existing = Object.FindObjectOfType<Camera>();
            if (existing != null)
            {
                _data.usedCamera = existing;
                Debug.Log($"[BackgroundParallax] 复用场景相机：{existing.name}");
            }
            else
            {
                var camGo = new GameObject("MainCamera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0, 0, -10f);
                _data.usedCamera = cam;
                Debug.LogWarning("[BackgroundParallax] 场景无相机，已自动创建");
            }
        }
        else
        {
            Debug.Log($"[BackgroundParallax] 使用 MainCamera：{_data.usedCamera.name}");
        }

        _data.cameraTransform = _data.usedCamera.transform;
        _cameraReady = true;
    }

    private void OnFixedUpdate()
    {
        EnsureCamera();
        if (_data.rootTransform == null || _data.cameraTransform == null) return;

        // 背景根跟随相机 Y 轴
        Vector3 rootPos = _data.rootTransform.position;
        rootPos.y = _data.cameraTransform.position.y;
        _data.rootTransform.position = rootPos;

        foreach (var layer in _layers)
        {
            for (int i = layer.blocks.Count - 1; i >= 0; i--)
            {
                var block = layer.blocks[i];
                block.transform.Translate(Vector3.left * Time.fixedDeltaTime * layer.layerSpeed);

                if (block.transform.position.x < _data.spawnBorderX && !block.hasSpawned)
                {
                    SpawnBlock(layer, block);
                    block.hasSpawned = true;
                    layer.blocks[i] = block;
                }

                if (block.transform.position.x < _data.destroyBorderX)
                {
                    AssetPool.Instance.PushGameObject(layer.layerTransform.name, block.gameObject);
                    layer.blocks.RemoveAt(i);
                }
            }
        }
    }

    private void SpawnBlock(BackgroundParallaxData.BackgroundLayer layer, BackgroundParallaxData.BackgroundBlock original)
    {
        GameObject go = AssetPool.Instance.GetObject(layer.layerTransform.name, original.gameObject);
        if (go == null) return;

        go.transform.SetParent(layer.layerTransform, false);
        go.transform.localScale = original.localScale;
        Vector3 spawnPos = original.transform.position + Vector3.right * layer.backgroundWidth;
        go.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);

        layer.blocks.Add(new BackgroundParallaxData.BackgroundBlock
        {
            gameObject = go,
            transform = go.transform,
            localScale = original.localScale,
            hasSpawned = false
        });
    }
}
