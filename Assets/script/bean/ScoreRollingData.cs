using UnityEngine;

/// <summary>
/// 分数滚动数据模块
/// 职责：纯数据存储、配置定义、结构声明
/// </summary>
[System.Serializable]
public class ScoreRollingData
{
     [Header("=== 核心绑定 ===")]
    [Tooltip("父物体（下面挂载着模板物体a）")]
    public GameObject parentRoot;
    
    [Tooltip("模板物体a（在父物体下，作为生成原型）")]
    public GameObject templateA;

    [Header("=== 全局滚动配置 ===")]
    [Tooltip("向左移动速度")]
    public float scrollSpeed = 5f;
    
    [Tooltip("生成新a的触发点X坐标（局部坐标）")]
    public float spawnTriggerX = 0f;
    
    [Tooltip("销毁a的触发点X坐标（局部坐标）")]
    public float destroyX = -10f;

    [Header("=== 运行时数据 ===")]
    public Vector3 spawnOrigin; // 新a的生成原点（模板的初始位置）
}