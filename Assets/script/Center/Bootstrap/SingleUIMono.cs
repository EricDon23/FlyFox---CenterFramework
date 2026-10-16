using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI专用Mono单例。
/// 职责：UI生命周期事件广播、UI协程托管、UI全局更新开关，挂载在UI根节点。
/// 区别于全局逻辑单例：不跨场景常驻（无 DontDestroyOnLoad），场景销毁自动清理UI资源。
/// 只广播UI层独立事件，不持有行为实例；订阅/退订由UI行为自身完成。
/// </summary>
public class SingleUIMono : MonoBehaviour
{
    private static SingleUIMono _instance;
    public static SingleUIMono Instance
    {
        get
        {
            if (_instance == null)
            {
                // 优先在场景中查找已存在的UI单例
                _instance = Object.FindObjectOfType<SingleUIMono>(includeInactive: true);
                if (_instance == null)
                {
                    GameObject uiRootObj = new GameObject("SingleUIMono(UI全局代理)");
                    // UI单例不使用DontDestroyOnLoad，切换场景时随UI根销毁，自动释放UI资源
                    _instance = uiRootObj.AddComponent<SingleUIMono>();
                     DontDestroyOnLoad(uiRootObj);
                }
            }
            return _instance;
        }
    }

    [Header("UI管理器总开关")]
    public bool UiGlobalEnable = true;
    [Header("UI Update全局开关（UI动画/输入/帧刷新）")]
    public bool UiUpdateEnable = true;
    [Header("UI FixedUpdate全局开关（较少用，UI物理相关）")]
    public bool UiFixedUpdateEnable = true;

    private void Awake()
    {
        // 单例防重复：场景内如果存在第二个实例，直接销毁
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // UI事件注册，仅注册UI生命周期事件（与逻辑层事件隔离）
        EventCenter.Instance.RegisterEvent("UIAwakeEvent");
        EventCenter.Instance.RegisterEvent("UIStartEvent");
        EventCenter.Instance.RegisterEvent("UIUpdateEvent");
        EventCenter.Instance.RegisterEvent("UIFixedUpdateEvent");
        EventCenter.Instance.RegisterEvent("UIOnDestroyEvent");
        EventCenter.Instance.Broadcast("UIAwakeEvent");
    }

    #region UI生命周期事件广播（仅UI层使用，和逻辑层生命周期事件隔离）
    private void Start()
    {
        if (!UiGlobalEnable) return;
        EventCenter.Instance.Broadcast("UIStartEvent");
    }

    private void Update()
    {
        if (!UiGlobalEnable || !UiUpdateEnable) return;
        EventCenter.Instance.Broadcast("UIUpdateEvent");
    }

    private void FixedUpdate()
    {
        if (!UiGlobalEnable || !UiFixedUpdateEnable) return;
        EventCenter.Instance.Broadcast("UIFixedUpdateEvent");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        if (!UiGlobalEnable) return;
        EventCenter.Instance.Broadcast("UIOnDestroyEvent");
        // 清理UI协程，防止协程残留访问销毁UI对象；行为实例不由本泵持有
        StopAllCoroutines();
    }
    #endregion

    #region UI协程代理封装（普通非Mono的UI逻辑类可以启动协程）
    public Coroutine StartUiCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void StopUiCoroutine(IEnumerator routine)
    {
        StopCoroutine(routine);
    }

    public void StopUiCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }

    public void StopAllUiCoroutine()
    {
        StopAllCoroutines();
    }
    #endregion

    #region UI全局暂停/恢复接口（弹窗遮挡、UI冻结时使用）
    /// <summary>冻结UI帧更新，UI动画/输入不再刷新。</summary>
    public void PauseUiUpdate()
    {
        UiUpdateEnable = false;
    }

    /// <summary>恢复UI帧更新。</summary>
    public void ResumeUiUpdate()
    {
        UiUpdateEnable = true;
    }

    /// <summary>完全关闭UI管理器（慎用，一般切换场景自动销毁）。</summary>
    public void DisableUiGlobal()
    {
        UiGlobalEnable = false;
    }

    public void EnableUiGlobal()
    {
        UiGlobalEnable = true;
    }
    #endregion

    #region UI额外优化接口
    /// <summary>清除实例引用，手动释放（场景切换前调用）。</summary>
    public void ClearInstance()
    {
        _instance = null;
    }
    #endregion
}
