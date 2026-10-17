using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单 UI 行为：递归查找所有 Button，按名字绑定事件。
/// 不依赖层级路径，不持有私有数据类。
/// </summary>
[AutoRegisterBehaviour("UIManager")]
public class UIManagerBehavior : UIBehavior
{
    public UIManagerBehavior() { isBuild = true; }

    private Button _beginBtn;
    private Button _overBtn;
    private Button _recordBtn;

    public override void Init(GameObject obj, BasicDate objectDate)
    {
        base.Init(obj, objectDate);
        Debug.Log($"[UIManager] Init 完成，g_obj={(g_obj != null ? g_obj.name : "null")}");
    }

    public override void OnUIAwake()
    {
        if (g_obj == null) { Debug.LogError("[UIManager] OnUIAwake 失败：g_obj 为 null"); return; }

        Debug.Log("[UIManager] OnUIAwake 开始递归查找按钮");

        // 递归查找所有 Button，不依赖层级路径
        var allButtons = g_obj.GetComponentsInChildren<Button>(true);
        Debug.Log($"[UIManager] 共找到 {allButtons.Length} 个 Button");

        foreach (var btn in allButtons)
        {
            string btnName = btn.name;
            if (btnName == "") { Debug.LogWarning($"[UIManager] 忽略空名字的 Button：{btn.transform.parent.name}/{btn.name}"); continue; }
            
            Debug.Log($"[UIManager] 找到 Button：{btn.transform.parent.name}/{btnName}");

            // 清空已有监听（防止预制体上残留）
            btn.onClick.RemoveAllListeners();

            switch (btnName)
            {
                case "begingame":
                    _beginBtn = btn;
                    btn.onClick.AddListener(OnBegin);
                    Debug.Log($"[UIManager] 绑定 {btnName} → OnBegin");
                    break;
                case "OverGame":
                    _overBtn = btn;
                    btn.onClick.AddListener(OnOver);
                    Debug.Log($"[UIManager] 绑定 {btnName} → OnOver");
                    break;
                case "Record":
                    _recordBtn = btn;
                    btn.onClick.AddListener(OnRecord);
                    Debug.Log($"[UIManager] 绑定 {btnName} → OnRecord");
                    break;
                default:
                    Debug.LogWarning($"[UIManager] 未知按钮 {btnName}，跳过");
                    break;
            }
        }

        Debug.Log($"[UIManager] 按钮绑定完成：begin={_beginBtn != null}, over={_overBtn != null}, record={_recordBtn != null}");
    }

    public override void LogicDestroy()
    {
        if (_beginBtn != null) _beginBtn.onClick.RemoveListener(OnBegin);
        if (_overBtn != null) _overBtn.onClick.RemoveListener(OnOver);
        if (_recordBtn != null) _recordBtn.onClick.RemoveListener(OnRecord);
        base.LogicDestroy();
    }

    private void OnBegin()
    {
        Debug.Log("[UIManager] 点击开始游戏");
        UICenter.Instance.OpenUI("GamingUI"); // 先打开 UI，确保订阅事件
        EventCenter.Instance.Broadcast("RequestGameStart"); // 再触发状态切换
        g_obj.SetActive(false);
    }

    private void OnOver()
    {
        Debug.Log("[UIManager] 点击结束游戏，退出程序");
        GlobalConter.Instance.DestroyAllBehaviours();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnRecord()
    {
        Debug.Log("[UIManager] 打开成绩记录页面");
        UICenter.Instance.OpenUI("Record");
        g_obj.SetActive(false);
    }
}
