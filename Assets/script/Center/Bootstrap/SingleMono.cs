using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局唯一事件泵（最先启动）。
/// 职责：
/// 1. 最先执行，在 Awake 内主动创建全局入口 GlobalConter，锁定启动时序 SingleMono → GlobalConter；
/// 2. 纯事件泵：经 EventCenter 广播 Awake/Start/Update/FixedUpdate/OnDestroy；
/// 3. 协程代理：普通类可借用本 Mono 启动/停止协程。
/// 不持有行为实例，不负责销毁调度（销毁由 BehaviourInstanceHub.DestroyAll 统一执行）。
/// </summary>
public class SingleMono : MonoBehaviour
{
    private static SingleMono instance;
    public static SingleMono Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SingleMono>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("SingleMono");
                    instance = singletonObject.AddComponent<SingleMono>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return instance;
        }
    }

    public bool GlobalManagerEnable = true;
    public bool isGlobalUpdateEnable = true;

    #region Awake：最先启动，主动创建全局入口 GlobalConter
    void Awake()
    {
        // 锁定时序：本事件泵最先存在，由本处创建全局入口，替代 GlobalConter 自举
        EnsureGlobalConterCreated();

        // 预注册并广播生命周期事件（事件池由 EventCenter 维护）
        EventCenter.Instance.RegisterEvent("AwakeEvent");
        EventCenter.Instance.RegisterEvent("UpdateEvent");
        EventCenter.Instance.RegisterEvent("StartEvent");
        EventCenter.Instance.RegisterEvent("FixedUpdateEvent");
        EventCenter.Instance.RegisterEvent("OnDestroyEvent");

        EventCenter.Instance.Broadcast("AwakeEvent");
    }

    /// <summary>确保全局入口 GlobalConter 被创建（若已存在则不重复创建）。</summary>
    private void EnsureGlobalConterCreated()
    {
        if (GlobalConter.Instance != null) return;

        GameObject go = new GameObject("GlobalConter");
        DontDestroyOnLoad(go);
        go.AddComponent<GlobalConter>();
    }
    #endregion

    #region 全局事件广播接口
    void Start()
    {
        if (!GlobalManagerEnable) return;
        EventCenter.Instance.Broadcast("StartEvent");
    }

    void Update()
    {
        if (!GlobalManagerEnable || !isGlobalUpdateEnable) return;
        EventCenter.Instance.Broadcast("UpdateEvent");
    }

    void FixedUpdate()
    {
        if (!GlobalManagerEnable || !isGlobalUpdateEnable) return;
        EventCenter.Instance.Broadcast("FixedUpdateEvent");
    }

    void OnDestroy()
    {
        if (!GlobalManagerEnable) return;
        EventCenter.Instance.Broadcast("OnDestroyEvent");
    }
    #endregion

    #region 协程代理封装：对外提供协程启动停止，普通类可调用
    public Coroutine StartGlobalCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void StopGlobalCoroutine(IEnumerator routine)
    {
        StopCoroutine(routine);
    }

    public void StopGlobalCoroutine(Coroutine coroutine)
    {
        StopCoroutine(coroutine);
    }
    #endregion

    #region 全局暂停/恢复接口
    public void PauseAllUpdate()
    {
        isGlobalUpdateEnable = false;
    }

    public void ResumeAllUpdate()
    {
        isGlobalUpdateEnable = true;
    }
    #endregion
}
