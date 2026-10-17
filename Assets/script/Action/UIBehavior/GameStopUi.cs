using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单 UI：stop 状态显示。
/// 结构：GameStop/Canvas/Panel/Continuous(Text)、Button(Text)
/// Continue → 恢复 Playing；Exit → 返回主菜单并销毁本局数据。
/// </summary>
[AutoRegisterBehaviour("GameStop")]
public class GameStopUi : UIBehavior
{
    public GameStopUi() { isBuild = false; }

    public override void OnUIAwake()
    {
        if (g_obj == null) return;

        Transform canvas = g_obj.transform.Find("Canvas") ?? g_obj.transform;
        Transform panel = canvas.Find("Panel") ?? canvas;

        // 绑定继续按钮
        Transform contBtn = panel.Find("Continuous");
        if (contBtn != null)
        {
            var btn = contBtn.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnContinue);
        }

        // 绑定 Exit 按钮
        Transform exitBtn = panel.Find("Button");
        if (exitBtn != null)
        {
            var btn = exitBtn.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnExit);
        }

        Debug.Log("[GameStop] 暂停菜单初始化完成");
    }

    /// <summary>继续游戏：切回 Playing。</summary>
    private void OnContinue()
    {
        Debug.Log("[GameStop] 继续游戏");
        EventCenter.Instance.Broadcast("RequestPause"); // stop → Playing
        UICenter.Instance.CloseUI("GameStop");
    }

    /// <summary>退出到主菜单：销毁本局数据。</summary>
    private void OnExit()
    {
        Debug.Log("[GameStop] 退出到主菜单");
        UICenter.Instance.CloseUI("GameStop");
        UICenter.Instance.CloseUI("GamingUI");
        EventCenter.Instance.Broadcast("RequestGameOver"); // stop → start
        UICenter.Instance.OpenUI("UIManager");
    }
}
