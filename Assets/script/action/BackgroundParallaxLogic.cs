using UnityEngine;

/// <summary>
/// 视差背景行为模块
/// 职责：所有核心逻辑实现，操作数据模块，不挂载到物体
/// </summary>
public class BackgroundParallaxLogic
{
    private BackgroundParallaxData _data;

    public BackgroundParallaxLogic(BackgroundParallaxData data)
    {
        _data = data;
    }

    #region 初始化逻辑
    /// <summary>
    /// 初始化核心组件缓存
    /// </summary>
    public void InitializeComponents()
    {
        _data.rootTransform = _data.backgroundRoot?.transform;
        _data.cameraTransform = _data.usedCamera?.transform;

        if (_data.rootTransform == null || _data.cameraTransform == null)
        {
            Debug.LogError("背景根物体/主相机未赋值！");
        }
    }

    /// <summary>
    /// 自动识别并初始化所有子层
    /// </summary>
    public void InitializeAllLayers()
    {
        int layerCount = _data.rootTransform.childCount;
        for (int i = 0; i < layerCount; i++)
        {
            Transform layerTransform = _data.rootTransform.GetChild(i);
            BackgroundParallaxData.BackgroundLayer layer = new BackgroundParallaxData.BackgroundLayer();
            
            layer.layerTransform = layerTransform;
            layer.layerSpeed = _data.baseScrollSpeed * (layerCount - i);
            
            InitializeLayerBlocks(layer);
            AutoCalculateLayerWidth(layer);

            _data.backgroundLayers.Add(layer);
            Debug.Log($"已初始化背景层：{layerTransform.name}，速度：{layer.layerSpeed}，宽度：{layer.backgroundWidth}");
        }
    }

    /// <summary>
    /// 初始化单个层的所有背景块
    /// </summary>
    private void InitializeLayerBlocks(BackgroundParallaxData.BackgroundLayer layer)
    {
        int blockCount = layer.layerTransform.childCount;
        for (int i = 0; i < blockCount; i++)
        {
            Transform blockTransform = layer.layerTransform.GetChild(i);
            BackgroundParallaxData.BackgroundBlock block = new BackgroundParallaxData.BackgroundBlock
            {
                gameObject = blockTransform.gameObject,
                transform = blockTransform,
                localScale = blockTransform.localScale,
                hasSpawned = false
            };
            layer.blocks.Add(block);
        }
    }

    /// <summary>
    /// 自动计算单个层的背景宽度
    /// </summary>
    private void AutoCalculateLayerWidth(BackgroundParallaxData.BackgroundLayer layer)
    {
        if (layer.blocks.Count == 0) return;

        SpriteRenderer sr = layer.blocks[0].gameObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            layer.backgroundWidth = sr.bounds.size.x;
        }
        else
        {
            layer.backgroundWidth = layer.blocks[0].transform.localScale.x * 100f;
        }
    }
    #endregion

    #region 核心滚动逻辑
    /// <summary>
    /// 背景根物体跟随相机Y轴
    /// </summary>
    public void UpdateCameraYFollow()
    {
        Vector3 rootPos = _data.rootTransform.position;
        rootPos.y = _data.cameraTransform.position.y;
        _data.rootTransform.position = rootPos;
    }

    /// <summary>
    /// 更新所有层的滚动、生成、回收逻辑
    /// </summary>
    public void UpdateAllLayersScroll()
    {
        foreach (var layer in _data.backgroundLayers)
        {
            if (layer.blocks.Count == 0) continue;

            for (int i = layer.blocks.Count - 1; i >= 0; i--)
            {
                BackgroundParallaxData.BackgroundBlock block = layer.blocks[i];
                
                block.transform.Translate(Vector3.left * Time.fixedDeltaTime * layer.layerSpeed);

                if (block.transform.position.x < _data.spawnBorderX && !block.hasSpawned)
                {
                    SpawnNewBlock(layer, block);
                    block.hasSpawned = true;
                    layer.blocks[i] = block;
                }

                if (block.transform.position.x < _data.destroyBorderX)
                {
                    RecycleBlock(layer, i);
                }
            }
        }
    }
    #endregion

    #region 对象池管理
    /// <summary>
    /// 从对象池生成/复用新背景块
    /// </summary>
    private void SpawnNewBlock(BackgroundParallaxData.BackgroundLayer layer, BackgroundParallaxData.BackgroundBlock originalBlock)
    {
        GameObject newBlock;
        if (layer.pool.Count > 0)
        {
            newBlock = layer.pool.Dequeue();
            newBlock.SetActive(true);
            newBlock.transform.localScale = originalBlock.localScale;
        }
        else
        {
            newBlock = Object.Instantiate(originalBlock.gameObject, layer.layerTransform);
            newBlock.transform.localScale = originalBlock.localScale;
        }

        Vector3 spawnPos = originalBlock.transform.position + Vector3.right * layer.backgroundWidth;
        newBlock.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);

        BackgroundParallaxData.BackgroundBlock newBlockData = new BackgroundParallaxData.BackgroundBlock
        {
            gameObject = newBlock,
            transform = newBlock.transform,
            localScale = originalBlock.localScale,
            hasSpawned = false
        };
        layer.blocks.Add(newBlockData);
    }

    /// <summary>
    /// 回收背景块到对象池
    /// </summary>
    private void RecycleBlock(BackgroundParallaxData.BackgroundLayer layer, int index)
    {
        BackgroundParallaxData.BackgroundBlock block = layer.blocks[index];
        block.gameObject.SetActive(false);
        block.transform.position = Vector3.zero;
        layer.pool.Enqueue(block.gameObject);
        layer.blocks.RemoveAt(index);
    }
    #endregion
}