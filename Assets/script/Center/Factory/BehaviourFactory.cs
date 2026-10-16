using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行为构造器：按 isGlobal 分流构造、数据装配、后加载注入编排、启动实例化。
/// 注册判定走 Registry，实例存取走 Hub；UI 行为交 UICenter 缓存，不进逻辑 Hub。
/// </summary>
public class BehaviourFactory : BasicSingle<BehaviourFactory>
{
    private BehaviourFactory() { }

    private BehaviourTypeRegistry Registry => BehaviourTypeRegistry.Instance;
    private BehaviourInstanceHub Hub => BehaviourInstanceHub.Instance;

    // ---------------- 数据装配（仅实体行为） ----------------
    private BasicDate LoadData(string registerKey)
    {
        BasicDate data = ConfigCenter.Instance.GetConfig(registerKey);
        return data != null ? data : ConfigCenter.Instance.CreateDefault(registerKey);
    }

    // ---------------- 对象创建 ----------------
    public GameObject SetGameObject(string registerKey)
    {
        Registry.TryGetType(registerKey, out Type behaviourType);
        return CreateObject(registerKey, behaviourType);
    }

    public GameObject SetGameObject<T>(string registerKey) where T : Objectbehavior, new()
        => CreateObject(registerKey, typeof(T));

    private GameObject CreateObject(string registerKey, Type behaviourType)
    {
        if (behaviourType == null) return null;

        bool isGlobal = Registry.IsGlobal(registerKey);

        // —— 全局逻辑行为：不抓预制体、不绑数据，g_obj = null ——
        if (isGlobal)
        {
            BasicObjectBehaviorImp globalBehaviour = InstantiateBehaviour(behaviourType, null, null);
            if (globalBehaviour == null) return null;

            if (Registry.ShouldInstantiate(registerKey))
            {
                globalBehaviour.LogicAwake();
                Hub.AddToStack(globalBehaviour);
            }
            else
            {
                Hub.AddPending(registerKey, globalBehaviour);
            }
            return null;
        }

        // —— Mono 类型（碰撞桥）不能经 Activator 创建，需走场景组件绑定 ——
        if (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(behaviourType))
        {
            Debug.LogWarning($"[Factory] {behaviourType.Name} 为 MonoBehaviour，不能经本路径 new，需走场景组件绑定：{registerKey}");
            return null;
        }

        BasicDate data = LoadData(registerKey);

        ResHandle handle = ResCenter.Instance.LoadSync<GameObject>(registerKey);
        if (handle == null || !(handle.AssetObj is GameObject prefab))
        {
            Debug.LogWarning($"[Factory] 加载预制体失败（缺 Resources 预制体）：{registerKey}");
            return null;
        }

        GameObject obj = UnityEngine.Object.Instantiate(prefab);
        handle.Release();
        // 【修复】Instantiate 后 Unity 自动命名为 "预制体名(Clone)"，改为 registerKey 原名，避免按名查找子物体时因后缀失败
        obj.name = registerKey;

        BasicObjectBehaviorImp behaviour = InstantiateBehaviour(behaviourType, obj, data);
        if (behaviour == null) return obj;

        if (Registry.ShouldInstantiate(registerKey))
        {
            behaviour.LogicAwake();
            Hub.AddToStack(behaviour);
        }
        else
        {
            Hub.AddPending(registerKey, behaviour);
        }
        return obj;
    }

    private BasicObjectBehaviorImp InstantiateBehaviour(Type behaviourType, GameObject obj, BasicDate data)
    {
        object instance;
        try { instance = Activator.CreateInstance(behaviourType); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Factory] 实例化行为失败 {behaviourType.Name}：{e.Message}");
            return null;
        }
        if (instance is Objectbehavior ob) ob.Init(obj, data);
        return instance as BasicObjectBehaviorImp;
    }

    public T CreateBehaviour<T>(GameObject obj, BasicDate date) where T : Objectbehavior, new()
    {
        T behaviour = new T();
        behaviour.Init(obj, date);
        behaviour.LogicAwake();
        Hub.AddToStack(behaviour);
        return behaviour;
    }

    public T CreateBehaviour<T>(GameObject obj, string registerKey) where T : Objectbehavior, new()
        => CreateBehaviour<T>(obj, LoadData(registerKey));

    // ---------------- 后加载注入编排 ----------------
    public void InjectBehaviour(string registerKey)
    {
        if (Hub.TryTakePending(registerKey, out BasicObjectBehaviorImp behaviour))
        {
            behaviour.LogicAwake();
            Hub.AddToStack(behaviour);
            Hub.RemovePending(registerKey);
            return;
        }

        if (Registry.Contains(registerKey))
        {
            bool hadFlag = Registry.TryGetInstantiateFlag(registerKey, out bool oldFlag);
            Registry.SetInstantiateFlag(registerKey, true);
            SetGameObject(registerKey);
            if (hadFlag) Registry.SetInstantiateFlag(registerKey, oldFlag);
            else Registry.SetInstantiateFlag(registerKey, false);
        }
    }

    public void InjectAllBehaviours()
    {
        foreach (BasicObjectBehaviorImp behaviour in Hub.PendingValues)
        {
            behaviour.LogicAwake();
            Hub.AddToStack(behaviour);
        }
        Hub.ClearPending();
    }

    // ---------------- UI 构造（交 UICenter 缓存，不进逻辑 Hub） ----------------
    /// <summary>
    /// 构造UI：加载预制体、实例化GameObject、new UIBehavior、执行Init+LogicAwake，
    /// 返回 UIBehavior（其 RootObj 即预制体实例）。注册缓存由调用方（UICenter）完成。
    /// </summary>
    public UIBehavior BuildUI(string registerKey)
    {
        if (!Registry.TryGetType(registerKey, out Type behaviourType))
        {
            Debug.LogWarning($"[BehaviourFactory] UI注册信息不存在：{registerKey}");
            return null;
        }

        ResHandle handle = ResCenter.Instance.LoadSync<GameObject>(registerKey);
        if (handle == null || !(handle.AssetObj is GameObject prefab))
        {
            Debug.LogWarning($"[BehaviourFactory] UI预制体加载失败：{registerKey}");
            return null;
        }
        GameObject uiRoot = UnityEngine.Object.Instantiate(prefab);
        handle.Release();
        uiRoot.name = registerKey;

        BasicDate data = LoadData(registerKey);

        UIBehavior uiBehavior;
        try
        {
            uiBehavior = (UIBehavior)Activator.CreateInstance(behaviourType);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BehaviourFactory] 创建UI行为失败 {behaviourType.Name}：{e.Message}");
            UnityEngine.Object.Destroy(uiRoot);
            return null;
        }

        uiBehavior.Init(uiRoot, data);
        uiBehavior.LogicAwake();
        return uiBehavior;
    }

    // ---------------- 启动实例化（过滤UI） ----------------
    public void BootstrapInitialBehaviours()
    {
        foreach (string name in Registry.GetKeys())
        {
            if (!Registry.TryGetType(name, out Type type)) continue;

            // UIBehavior 跳过，不进逻辑 Hub，交由 UICenter 管理
            if (typeof(UIBehavior).IsAssignableFrom(type)) continue;

            if (!Registry.IsInitialLoad(name)) continue;
            SetGameObject(name);
        }
    }
}
