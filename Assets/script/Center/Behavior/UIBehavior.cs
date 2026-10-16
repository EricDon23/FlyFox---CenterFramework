using System;
using UnityEngine;

/// <summary>
/// UI行为基类，纯C#，继承Objectbehavior，不挂载GameObject
/// 由SingleUIMono的UIUpdateEvent / UIFixedUpdateEvent驱动帧更新
/// LogicAwake自动订阅，LogicDestroy自动退订
/// </summary>
public abstract class UIBehavior : Objectbehavior
{
    private Action _uiUpdate;
    private Action _uiFixedUpdate;

    /// <summary>UI面板当前是否可见，UICenter Open/Close自动维护</summary>
    public bool IsVisible { get; private set; }

    /// <summary>UI 根节点（由工厂在 Init 时注入，isGlobal 时为 null）。</summary>
    public GameObject RootObj => g_obj;

    public override void LogicAwake()
    {
        _uiUpdate = OnUIUpdate;
        _uiFixedUpdate = OnUIFixedUpdate;
        EventCenter.Instance.Subscribe("UIUpdateEvent", _uiUpdate);
        EventCenter.Instance.Subscribe("UIFixedUpdateEvent", _uiFixedUpdate);

        OnUIAwake();
    }

    /// <summary>UI初始化，预制体实例完成、行为构造完成触发，只执行一次</summary>
    public virtual void OnUIAwake() { }

    /// <summary>每次UI打开时触发（缓存复用会重复执行）</summary>
    public virtual void OnShow()
    {
        IsVisible = true;
    }

    /// <summary>每次UI关闭隐藏时触发</summary>
    public virtual void OnHide()
    {
        IsVisible = false;
    }

    /// <summary>UI帧更新，SingleUIMono每帧广播；隐藏状态自动跳过</summary>
    public virtual void OnUIUpdate()
    {
        if (!IsVisible) return;
    }

    /// <summary>UI固定帧更新</summary>
    public virtual void OnUIFixedUpdate()
    {
        if (!IsVisible) return;
    }

    public override void LogicDestroy()
    {
        // 退订UI泵事件，防止内存泄漏
        if (_uiUpdate != null)
        {
            EventCenter.Instance.Unsubscribe("UIUpdateEvent", _uiUpdate);
            _uiUpdate = null;
        }
        if (_uiFixedUpdate != null)
        {
            EventCenter.Instance.Unsubscribe("UIFixedUpdateEvent", _uiFixedUpdate);
            _uiFixedUpdate = null;
        }
    }
}
