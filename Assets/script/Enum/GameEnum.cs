/// <summary>
/// 游戏全局状态机：
/// start   —— 初始化界面（UIManagerBehavior 主菜单显示）
/// Playing —— 游戏进行中（LevelBuild/玩家操作/GamingUI 激活）
/// stop    —— 暂停状态
/// </summary>
public enum GameEnum
{
    start,
    Playing,
    stop
}
