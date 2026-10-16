using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 工厂门面：不持业务逻辑，仅把调用转发给 Registry / Factory / Hub，保持对外API稳定。
/// </summary>
public class FactoryCenter : BasicSingle<FactoryCenter>
{
    private FactoryCenter() { }

    private BehaviourTypeRegistry Registry => BehaviourTypeRegistry.Instance;
    private BehaviourFactory Factory => BehaviourFactory.Instance;
    private BehaviourInstanceHub Hub => BehaviourInstanceHub.Instance;

    public IReadOnlyList<BasicObjectBehaviorImp> Behaviours => Hub.Behaviours;

    // ---- 注册（转发 Registry） ----
    public void RegisterBehaviour<T>(string registerKey) where T : Objectbehavior, new()
        => Registry.RegisterBehaviour<T>(registerKey);

    public void RegisterBehaviour(Type behaviourType, string registerKey)
        => Registry.RegisterBehaviour(behaviourType, registerKey);

    public void RemoveBehaviour(string registerKey) => Registry.RemoveBehaviour(registerKey);

    public void SetInstantiateFlag(string registerKey, bool enabled)
        => Registry.SetInstantiateFlag(registerKey, enabled);

    // ---- 创建（转发 Factory） ----
    public GameObject SetGameObject(string registerKey) => Factory.SetGameObject(registerKey);

    public GameObject SetGameObject<T>(string registerKey) where T : Objectbehavior, new()
        => Factory.SetGameObject<T>(registerKey);

    public T CreateBehaviour<T>(GameObject obj, BasicDate date) where T : Objectbehavior, new()
        => Factory.CreateBehaviour<T>(obj, date);

    public T CreateBehaviour<T>(GameObject obj, string registerKey) where T : Objectbehavior, new()
        => Factory.CreateBehaviour<T>(obj, registerKey);

    // ---- 注入 / 启动（转发 Factory） ----
    public void InjectBehaviour(string registerKey) => Factory.InjectBehaviour(registerKey);

    public void InjectAllBehaviours() => Factory.InjectAllBehaviours();

    public void BootstrapInitialBehaviours() => Factory.BootstrapInitialBehaviours();

    // ---- 清栈（转发 Hub，不重复调 LogicDestroy） ----
    public void ClearBehaviours() => Hub.ClearAll();
}
