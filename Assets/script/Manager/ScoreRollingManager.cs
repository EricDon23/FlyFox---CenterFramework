using UnityEngine;

/// <summary>
/// 【挂载到父物体上】分数滚动管理器
/// 职责：初始化数据、调用 ScoreRollingLogic 完成所有流程
/// </summary>
public class ScoreRollingManager : MonoBehaviour
{
    [Header("=== 分数滚动数据 ===")]
    public ScoreRollingData rollingData;

    private ScoreRollingLogic _logic;


    void Awake()
    {
        // 初始化全局逻辑
        _logic = new ScoreRollingLogic(rollingData);
        _logic.Initialize();
    }
}