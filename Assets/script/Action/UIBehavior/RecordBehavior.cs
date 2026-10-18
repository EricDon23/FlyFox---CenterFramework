using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 成绩记录 UI：读取 scores.json，按分数排序显示。
/// 结构：Record/Canvas/Panel/ScrollView/Viewport/Content/default
/// default 是模板，默认隐藏；有几条数据就克隆几个并显示。
/// </summary>
[AutoRegisterBehaviour("Record")]
public class RecordBehavior : UIBehavior
{
    public RecordBehavior() { isBuild = false; }

    private Transform _content;
    private TextMeshProUGUI _template;  // default 模板
    private TextMeshProUGUI _waitingText;  // waiting 提示
    private bool _isSaving;  // 是否正在保存

    public override void OnUIAwake()
    {
        if (g_obj == null) return;

        Transform canvas = g_obj.transform.Find("Canvas") ?? g_obj.transform;
        Transform panel = canvas.Find("Panel") ?? canvas;

        // 查找 Content 和 default 模板
        Transform scrollView = panel.Find("Scroll View") ?? panel.Find("ScrollView");
        Transform viewport = scrollView?.Find("Viewport");
        _content = viewport?.Find("Content");
        _template = _content?.Find("Default")?.GetComponent<TextMeshProUGUI>()
                 ?? _content?.Find("default")?.GetComponent<TextMeshProUGUI>();

        // 查找 waiting 提示文本
        _waitingText = panel.Find("Waiting")?.GetComponent<TextMeshProUGUI>();
        if (_waitingText != null) _waitingText.gameObject.SetActive(false);

        Debug.Log($"[Record] content={_content != null}，template={_template != null}，waiting={_waitingText != null}");

        // 订阅保存事件
        EventCenter.Instance.Subscribe("ScoreSaving", OnScoreSaving);
        EventCenter.Instance.Subscribe("ScoreSaved", OnScoreSaved);

        // 检查是否正在保存
        if (_isSaving)
        {
            ShowWaiting();
        }
        else
        {
            RefreshScoreList();
        }

        EventCenter.Instance.Subscribe("ESCKeyDown", OnESC);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe("ESCKeyDown", OnESC);
        EventCenter.Instance.Unsubscribe("ScoreSaving", OnScoreSaving);
        EventCenter.Instance.Unsubscribe("ScoreSaved", OnScoreSaved);
        base.LogicDestroy();
    }

    private void OnScoreSaving()
    {
        _isSaving = true;
        ShowWaiting();
    }

    private void OnScoreSaved()
    {
        _isSaving = false;
        HideWaiting();
        RefreshScoreList();
    }

    private void ShowWaiting()
    {
        if (_waitingText != null)
        {
            _waitingText.gameObject.SetActive(true);
            _waitingText.text = "waiting...";
        }
        // 清空旧列表
        for (int i = _content.childCount - 1; i >= 1; i--)
            Object.Destroy(_content.GetChild(i).gameObject);
    }

    private void HideWaiting()
    {
        if (_waitingText != null) _waitingText.gameObject.SetActive(false);
    }

    private void OnESC()
    {
        if (!IsVisible) return;
        Debug.Log("[Record] ESC 关闭成绩页面");
        UICenter.Instance.CloseUI("Record");
        UICenter.Instance.OpenUI("UIManager");
    }

    /// <summary>读取 scores.json，按分数排序，克隆 default 模板显示。</summary>
    private async void RefreshScoreList()
    {
        if (_content == null || _template == null)
        {
            Debug.LogWarning("[Record] Content 或 default 模板不存在");
            return;
        }

        // 显示加载中
        _template.gameObject.SetActive(true);
        _template.text = "加载中...";
        Debug.Log("[Record] 开始读取分数文件...");

        // 删除旧的克隆（保留 default 模板）
        for (int i = _content.childCount - 1; i >= 1; i--)
            Object.Destroy(_content.GetChild(i).gameObject);

        // 直接读文件
        string path = "Assets/Resources/Config/scores.json";
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("[Record] 找不到 scores.json");
            _template.text = "暂无记录";
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        Debug.Log($"[Record] 直接读取文件，内容：\n{json}");

        Debug.Log($"[Record] 读取到原始 JSON：\n{json}");

        // 反序列化
        var wrapper = JsonUtility.FromJson<ListWrapper<ScoreRecord>>(json);
        Debug.Log($"[Record] wrapper==null? {wrapper == null}，wrapper.Items==null? {wrapper?.Items == null}，Items.Count={wrapper?.Items?.Count ?? 0}");

        if (wrapper?.Items != null)
            for (int i = 0; i < wrapper.Items.Count; i++)
                Debug.Log($"[Record] 原始第{i}条：score={wrapper.Items[i].score}，duration={wrapper.Items[i].duration}");

        List<ScoreRecord> sorted = wrapper.Items == null
            ? new List<ScoreRecord>()
            : wrapper.Items.OrderByDescending(x => x.score).ToList();

        Debug.Log($"[Record] 排序后 {sorted.Count} 条");

        if (sorted.Count == 0)
        {
            Debug.Log("[Record] 无成绩记录");
            _template.text = "暂无记录";
            return;
        }

        // 确保 Content 有 Vertical Layout Group（纵向排列）
        var layout = _content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout == null) layout = _content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);

        // 确保 Content 有 Content Size Fitter
        var fitter = _content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = _content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[Record] 已配置纵向排列布局");

        // 隐藏模板本身
        _template.gameObject.SetActive(false);

        // 克隆 N 条显示
        for (int i = 0; i < sorted.Count; i++)
        {
            var clone = Object.Instantiate(_template.gameObject, _content);
            clone.SetActive(true);
            var tmp = clone.GetComponent<TextMeshProUGUI>();

            // 时间格式：分:秒.毫秒
            float dur = sorted[i].duration;
            int minutes = Mathf.FloorToInt(dur / 60f);
            int seconds = Mathf.FloorToInt(dur % 60f);
            int milliseconds = Mathf.FloorToInt((dur * 1000f) % 1000f);
            string durStr = $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";

            // 格式：#名次 score:分数 Time:分:秒.毫秒 Data:游戏开始时间
            tmp.text = $"#{i + 1} score:{sorted[i].score} Time:{durStr} Data:{sorted[i].time}";
            clone.name = $"record_{i + 1}";
            Debug.Log($"[Record] 第{i + 1}名：{sorted[i].score}分");
        }

        Debug.Log($"[Record] 显示完成，共 {sorted.Count} 条");
    }
}
