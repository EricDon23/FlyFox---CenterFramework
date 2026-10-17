using UnityEngine;

/// <summary>
/// 全局输入控制器（游戏逻辑层）：通过 InputCenter 注册按键映射，检测后广播事件。
/// 行为层订阅事件即可，不直接读 Input。
/// </summary>
[AutoRegisterBehaviour("InputController")]
public class InputController : Objectbehavior
{
    public InputController() { isGlobal = true; isBuild = true; }

    public override void LogicAwake()
    {
        // 通过 InputCenter 注册按键映射
        InputCenter.Instance.SetKeyMap("ESC", "Escape");
        InputCenter.Instance.SetKeyMap("Pause", "P");

        EventCenter.Instance.RegisterEvent("ESCKeyDown");
        EventCenter.Instance.RegisterEvent("PauseKeyDown");
        EventCenter.Instance.Subscribe("UpdateEvent", OnUpdate);
        Debug.Log("[InputController] 输入监听启动，ESC=Escape, Pause=P");
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("UpdateEvent", OnUpdate);
    }

    private void OnUpdate()
    {
        // ESC：返回/暂停
        if (InputCenter.Instance.GetKeyDown("ESC"))
        {
            Debug.Log("[InputController] ESC 按下");
            EventCenter.Instance.Broadcast("ESCKeyDown");

            if (Global.CurrentState == GameEnum.Playing)
            {
                // Playing → stop：阻塞游戏逻辑，打开暂停菜单
                EventCenter.Instance.Broadcast("RequestPause");
                UICenter.Instance.OpenUI("GameStop");
            }
            else if (Global.CurrentState == GameEnum.stop)
            {
                // stop → Playing：恢复游戏，关闭暂停菜单
                EventCenter.Instance.Broadcast("RequestPause");
                UICenter.Instance.CloseUI("GameStop");
            }
        }

        // P 键：暂停
        if (InputCenter.Instance.GetKeyDown("Pause"))
        {
            Debug.Log("[InputController] Pause 按下");
            EventCenter.Instance.Broadcast("PauseKeyDown");
            if (Global.CurrentState == GameEnum.Playing)
                EventCenter.Instance.Broadcast("RequestPause");
        }
    }
}
