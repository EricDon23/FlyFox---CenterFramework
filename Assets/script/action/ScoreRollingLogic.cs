using UnityEngine;

/// <summary>
/// 分数滚动行为模块
/// 职责：所有核心逻辑实现，操作数据模块，不挂载到物体
/// </summary>
public class ScoreRollingLogic
{
    private ScoreRollingData _data;

    public ScoreRollingLogic(ScoreRollingData data)
    {
        _data = data;
    }

  /// <summary>
    /// 全局初始化：记录原点、隐藏模板、生成第一个物体
    /// </summary>
    public void Initialize()
    {
        // 1. 校验数据
        if (_data.parentRoot == null)
        {
            Debug.LogError("parentRoot 未赋值！");
            return;
        }
        if (_data.templateA == null)
        {
            Debug.LogError("templateA 未赋值！请在父物体下放置模板物体a");
            return;
        }
        if (_data.templateA.GetComponent<ItemRollingItem>() == null)
        {
            Debug.LogError("模板物体a上没有挂载 ItemRollingItem 组件！");
            return;
        }

        // 2. 记录生成原点（模板的初始位置）
        _data.spawnOrigin = _data.templateA.transform.localPosition;

        // 3. 隐藏模板（只作为原型）
        _data.templateA.SetActive(false);

        // 4. 生成第一个物体
        SpawnFirstItem();

        Debug.Log($"全局初始化完成：生成原点={_data.spawnOrigin}");
    }

    /// <summary>
    /// 生成第一个物体，启动循环
    /// </summary>
    private void SpawnFirstItem()
    {
        GameObject firstItem = GameObject.Instantiate(_data.templateA, _data.parentRoot.transform);
        firstItem.transform.localPosition = _data.spawnOrigin;
        firstItem.SetActive(true);
        firstItem.name = _data.templateA.name + "_FirstInstance";

        // 初始化第一个物体
        ItemRollingItem firstItemLogic = firstItem.GetComponent<ItemRollingItem>();
        if (firstItemLogic != null )
        {
            firstItemLogic.Initialize(_data);
        }
    }
}