using UnityEngine;
using TMPro;

/// <summary>
/// 成绩记录 UI：显示历史最高分。
/// 结构：Record/Canvas/Panel/Text (TMP)、Scroll View/Viewport/Content
/// 由 UIManager.OnRecord 打开，按 ESC 关闭返回主菜单。
/// </summary>
[AutoRegisterBehaviour("Record")]
public class RecordBehavior : UIBehavior
{
    public RecordBehavior() { isBuild = false; } // 按需打开

    private TextMeshProUGUI _titleText;

    public override void OnUIAwake()
    {
        if (g_obj == null) return;

        Transform canvas = g_obj.transform.Find("Canvas") ?? g_obj.transform;
        Transform panel = canvas.Find("Panel") ?? canvas;

        Transform textNode = panel.Find("Text");
        if (textNode != null) _titleText = textNode.GetComponent<TextMeshProUGUI>();

        Debug.Log($"[Record] UI 初始化完成，title={_titleText != null}");
        RefreshScoreList();

        EventCenter.Instance.Subscribe("ESCKeyDown", OnESC);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("ESCKeyDown", OnESC);
        base.LogicDestroy();
    }

    /// <summary>ESC 关闭返回主菜单。</summary>
    private void OnESC()
    {
        if (!IsVisible) return;
        Debug.Log("[Record] ESC 关闭成绩页面");
        UICenter.Instance.CloseUI("Record");
        UICenter.Instance.OpenUI("UIManager");
    }

    /// <summary>刷新成绩列表。</summary>
    private void RefreshScoreList()
    {
        if (_titleText != null)
            _titleText.text = $"当前分数：{Global.SessionBaseScore}\n历史最高：{Global.FinalSessionScore}";
    }
}
