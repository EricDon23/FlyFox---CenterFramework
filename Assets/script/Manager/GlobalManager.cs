using UnityEngine;

[System.Serializable]
public class GlobalManager : MonoBehaviour
{
    [Header("调试信息（只读）")]
    [SerializeField] private GameEnum _debugCurrentState;

    void Awake()
    {
        // 唯一职责：初始化全局数据和事件绑定
        InitializeGlobal();
    }

    /// <summary>
    /// 全局初始化（仅此一次）
    /// </summary>
    private void InitializeGlobal()
    {
        // 1. 绑定状态变化事件（仅用于调试显示）
        GlobalConter.Instance.ChanageState += (state) => 
        { 
            Global.CurrentState = state;
            _debugCurrentState = state;
        };

        // 2. 初始化初始状态
        Global.CurrentState = GameEnum.Menu;
        _debugCurrentState = GameEnum.Menu;

        // 3. 调用 GlobalMonitor 统一初始化
        GlobalMonitor.Initialize();

        Debug.Log("[GlobalManager] 全局初始化完成，后续逻辑由子模块处理");
    }

    // 移除了 Update()，后期操作完全由其他子模块/总管线负责
    // 移除了 OnApplicationQuit()，如果需要可以在其他管线中处理
}