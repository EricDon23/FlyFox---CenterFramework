using UnityEngine;

/// <summary>
/// 【挂载到模板物体a上】单个滚动物品的自主逻辑
/// 职责：自己移动、自己触发生成下一个、自己销毁
/// </summary>
public class ItemRollingItem : MonoBehaviour
{
    // 全局配置引用（由管理器注入）
    private ScoreRollingData _globalData;
    // 标记是否已经触发过生成，防止重复生成
    private bool _hasSpawnedNext = false;

    /// <summary>
    /// 初始化：由管理器调用，注入全局配置
    /// </summary>
    public void Initialize(ScoreRollingData globalData)
    {
        _globalData = globalData;
        _hasSpawnedNext = false;
    }

    void Update()
    {
        // 只要被生成，就自动向左移动
        gameObject.transform.Translate(Vector3.left * _globalData.scrollSpeed * Time.deltaTime);

        // 1. 检查是否需要生成下一个
        if (!_hasSpawnedNext && transform.localPosition.x < _globalData.spawnTriggerX)
        {
            _hasSpawnedNext = true;
            SpawnNextItem();
        }

        // 2. 检查是否需要销毁自己
        if (transform.localPosition.x < _globalData.destroyX)
        {
            DestroySelf();
        }
    }

    /// <summary>
    /// 在原点生成下一个物体
    /// </summary>
    private void SpawnNextItem()
    {
        if (_globalData.templateA == null || _globalData.parentRoot == null) return;

        // 直接实例化新物体
        GameObject newItem = Instantiate(_globalData.templateA, _globalData.parentRoot.transform);
        
        // 设置到生成原点
        newItem.transform.localPosition = _globalData.spawnOrigin;
        
        // 显示新物体
        newItem.SetActive(true);
        
        // 重命名
        newItem.name = _globalData.templateA.name + "_Instance";
        
        // 初始化新物体，注入全局配置
        ItemRollingItem newItemLogic = newItem.GetComponent<ItemRollingItem>();
        if (newItemLogic != null)
        {
            newItemLogic.Initialize(_globalData);
        }

        Debug.Log($"[自主生成] 新物体：{newItem.name}");
    }

    /// <summary>
    /// 销毁自己
    /// </summary>
    private void DestroySelf()
    {
        Debug.Log($"[自主销毁] 物体：{gameObject.name}");
        Destroy(gameObject);
    }
}