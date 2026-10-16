using UnityEngine;

/// <summary>
/// 主菜单 UI 数据：仅保存 UI 结构配置（按钮相对路径等），不含业务逻辑。
/// </summary>
[System.Serializable]
[AutoRegisterConfig("UIManager")]
public class UIManagerData : BasicDate
{
    [Header("=== 按钮路径配置（相对 ui 根节点） ===")]
    [Tooltip("开始游戏按钮路径")]
    public string beginButtonPath = "mainMenu/begingame";

    [Tooltip("结束游戏按钮路径")]
    public string overButtonPath = "mainMenu/OverGame";

    [Tooltip("记录按钮路径")]
    public string recordButtonPath = "mainMenu/Record";
}