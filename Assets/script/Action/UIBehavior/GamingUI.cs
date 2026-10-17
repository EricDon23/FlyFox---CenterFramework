using UnityEngine;
using TMPro;

/// <summary>
/// 游戏内 HUD：血量图标、冲刺图标、实时分数。
/// 预制体结构：Canvas/healthstatus/heal、Canvas/Dashststus/dash、Canvas/score
/// heal 和 dash 逻辑完全一致。
/// </summary>
[AutoRegisterBehaviour("GamingUI")]
public class GamingUI : UIBehavior
{
    public GamingUI() { isBuild = false; }

    private Transform _healTemplate;  // healthstatus/heal 模板
    private Transform _dashTemplate;  // Dashststus/dash 模板
    private TextMeshProUGUI _scoreText;

    private int _maxHeal;
    private int _maxDash;
    private int _currentHeal;
    private int _currentDash;

    public override void OnUIAwake()
    {
        if (g_obj == null) return;

        Transform canvas = g_obj.transform.Find("Canvas") ?? g_obj.transform;

        Transform healContainer = canvas.Find("healthstatus");
        Transform dashContainer = canvas.Find("Dashstatus");

        // 和 heal 完全一样的方式找模板
        _healTemplate = healContainer != null && healContainer.childCount > 0 ? healContainer.GetChild(0) : null;
        _dashTemplate = dashContainer != null && dashContainer.childCount > 0 ? dashContainer.GetChild(0) : null;

        Transform scoreNode = canvas.Find("score");
        _scoreText = scoreNode != null ? scoreNode.GetComponent<TextMeshProUGUI>() : null;

        Debug.Log($"[GamingUI] healTemplate={_healTemplate != null}, dashTemplate={_dashTemplate != null}");

        EventCenter.Instance.Subscribe<GameEnum>("GameStateChanged", OnStateChanged);
        EventCenter.Instance.Subscribe<object>("PlayerDamaged", OnPlayerDamaged);
        EventCenter.Instance.Subscribe<object>("PlayerDashUsed", OnPlayerDashUsed);
        EventCenter.Instance.Subscribe<object>("PlayerHealed", OnPlayerHealed);
        EventCenter.Instance.Subscribe<object>("DashRestored", OnDashRestored);
        EventCenter.Instance.Subscribe<int>("ScoreAdded", OnScoreAdded);
    }

    public override void LogicDestroy()
    {
        EventCenter.Instance.Unsubscribe<GameEnum>("GameStateChanged", OnStateChanged);
        EventCenter.Instance.Unsubscribe<object>("PlayerDamaged", OnPlayerDamaged);
        EventCenter.Instance.Unsubscribe<object>("PlayerDashUsed", OnPlayerDashUsed);
        EventCenter.Instance.Unsubscribe<object>("PlayerHealed", OnPlayerHealed);
        EventCenter.Instance.Unsubscribe<object>("DashRestored", OnDashRestored);
        EventCenter.Instance.Unsubscribe<int>("ScoreAdded", OnScoreAdded);
        base.LogicDestroy();
    }

    private void OnStateChanged(GameEnum state)
    {
        if (state != GameEnum.Playing) return;

        var player = PlayerRefHolder.Instance.Player;
        _maxHeal = player != null ? player.MaxHealth : 3;
        _maxDash = player != null ? player.MaxDashCount : 3;
        _currentHeal = _maxHeal;
        _currentDash = _maxDash;

        BuildIcons(_healTemplate, _maxHeal, 50f);
        BuildIcons(_dashTemplate, _maxDash, 20f);
        RefreshHeal();
        RefreshDash();
        UpdateScoreText(Global.SessionBaseScore);
    }

    /// <summary>克隆模板 N 个，横向排列。</summary>
    private void BuildIcons(Transform template, int count, float spacing)
    {
        if (template == null) return;
        Transform parent = template.parent;

        for (int i = parent.childCount - 1; i >= 1; i--)
            Object.Destroy(parent.GetChild(i).gameObject);

        for (int i = 1; i < count; i++)
        {
            var clone = Object.Instantiate(template.gameObject, parent);
            var rt = clone.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(i * spacing, 0);
                rt.localScale = Vector3.one;
            }
        }
    }

    private void OnPlayerDamaged(object hp)
    {
        if (hp is int health) _currentHeal = health;
        RefreshHeal();
    }

    private void OnPlayerDashUsed(object dashCount)
    {
        if (dashCount is int remaining) _currentDash = remaining;
        RefreshDash();
    }

    private void OnPlayerHealed(object hp)
    {
        if (hp is int health) _currentHeal = health;
        RefreshHeal();
    }

    private void OnDashRestored(object dashCount)
    {
        if (dashCount is int remaining) _currentDash = remaining;
        RefreshDash();
    }

    private void OnScoreAdded(int score)
    {
        UpdateScoreText(Global.SessionBaseScore);
    }

    private void RefreshHeal()
    {
        if (_healTemplate == null) return;
        Transform parent = _healTemplate.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var slot = parent.GetChild(i);
            var nohide = slot.Find("heal(nohide)");
            var hide = slot.Find("heal(hide)");
            bool hasHp = i < _currentHeal;
            if (nohide != null) nohide.gameObject.SetActive(true);
            if (hide != null) hide.gameObject.SetActive(hasHp);
        }
    }

    private void RefreshDash()
    {
        if (_dashTemplate == null) return;
        Transform parent = _dashTemplate.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var slot = parent.GetChild(i);
            var nohide = slot.Find("dash(nohide)");
            var hide = slot.Find("dash(hide)");
            bool hasDash = i < _currentDash;
            if (nohide != null) nohide.gameObject.SetActive(true);
            if (hide != null) hide.gameObject.SetActive(hasDash);
        }
    }

    private void UpdateScoreText(int score)
    {
        if (_scoreText != null)
            _scoreText.text = $"Score: {score}";
    }
}
