using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// GameOver 界面：显示本局分数、存活时间。
/// 结构：GameOver/Canvas/Panel/ScoreText、TimeText、MenuBtn、BoardBtn
/// Menu → 返回主菜单，销毁游戏实例
/// Board → 跳转 Record 成绩榜
/// 自动将本局分数追加到 Resources/Config/scores.json
/// </summary>
[AutoRegisterBehaviour("GameOver")]
public class GameOverUi : UIBehavior
{
    public GameOverUi() { isBuild = false; }

    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _timeText;

    public override void OnUIAwake()
    {
        if (g_obj == null) return;

        Transform canvas = g_obj.transform.Find("Canvas") ?? g_obj.transform;
        Transform panel = canvas.Find("Panel") ?? canvas;

        // 打印所有子物体，便于定位名字
        Debug.Log($"[GameOver] g_obj={g_obj.name}，canvas={canvas.name}，panel={panel.name}");
        for (int i = 0; i < panel.childCount; i++)
            Debug.Log($"[GameOver] Panel 子物体 {i}: {panel.GetChild(i).name}");

        _scoreText = panel.Find("Score")?.GetComponent<TextMeshProUGUI>()
                  ?? panel.Find("score")?.GetComponent<TextMeshProUGUI>();
        _timeText = panel.Find("Time")?.GetComponent<TextMeshProUGUI>()
                 ?? panel.Find("time")?.GetComponent<TextMeshProUGUI>();

        var menuBtn = panel.Find("menu")?.GetComponent<Button>();
        if (menuBtn != null) menuBtn.onClick.AddListener(OnMenu);
        else Debug.LogWarning("[GameOver] 未找到 menu 按钮");

        var boardBtn = panel.Find("broad")?.GetComponent<Button>();
        if (boardBtn != null) boardBtn.onClick.AddListener(OnBoard);
        else Debug.LogWarning("[GameOver] 未找到 broad 按钮");

        RefreshUI();
        SaveScoreRecord();
        Debug.Log("[GameOver] 界面初始化完成");
    }

    private void RefreshUI()
    {
        // 分数格式："分数：XXX"
        if (_scoreText != null)
            _scoreText.text = $"分数：{Global.SessionBaseScore}";

        // 时间格式："time：分:秒.毫秒"
        if (_timeText != null)
        {
            float t = Global.SessionDuration;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            int milliseconds = Mathf.FloorToInt((t * 1000f) % 1000f);
            _timeText.text = $"time：{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
        }

        Debug.Log($"[GameOver] 显示：分数={Global.SessionBaseScore}，时长={Global.SessionDuration:F3}s");
    }

    /// <summary>保存本局分数到 JSON 配置表（同一文件续写，不新建）。</summary>
    private void SaveScoreRecord()
    {
        Debug.Log("[GameOver] ====== 开始保存分数 ======");
        EventCenter.Instance.Broadcast("ScoreSaving");  // 广播：正在写入

        var record = new ScoreRecord
        {
            score = Global.SessionBaseScore,
            duration = Global.SessionDuration,
            time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            date = System.DateTime.Now.ToString("yyyyMMddHHmmss")
        };

        string path = "Assets/Resources/Config/scores.json";
        bool exists = System.IO.File.Exists(path);
        Debug.Log($"[GameOver] 目标路径：{path}，文件存在={exists}");

        if (exists)
        {
            ConfigTableGenerator.AppendRow(record, "scores");
        }
        else
        {
            var list = new System.Collections.Generic.List<ScoreRecord> { record };
            ConfigTableGenerator.GenerateBatch(list, "scores");
        }

        EventCenter.Instance.Broadcast("ScoreSaved");  // 广播：写入完成
        Debug.Log($"[GameOver] ====== 保存完成：score={record.score} ======");
    }

    /// <summary>Menu 按钮：返回主菜单，销毁游戏实例。</summary>
    private void OnMenu()
    {
        Debug.Log("[GameOver] 返回主菜单");
        UICenter.Instance.CloseUI("GameOver");
        UICenter.Instance.CloseUI("GamingUI");
        EventCenter.Instance.Broadcast("RequestGameOver");
        UICenter.Instance.OpenUI("UIManager");
    }

    /// <summary>Board 按钮：跳转 Record 成绩榜。</summary>
    private void OnBoard()
    {
        Debug.Log("[GameOver] 点击 Board，尝试打开 Record UI");
        UICenter.Instance.CloseUI("GameOver");
        UICenter.Instance.CloseUI("GamingUI");
        UICenter.Instance.OpenUI("Record");
        Debug.Log("[GameOver] OpenUI(Record) 调用完成");
    }
}
