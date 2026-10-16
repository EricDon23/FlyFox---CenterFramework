using System.Collections.Generic;

/// <summary>
/// 行为实例仓库：只持有「已启动实例栈」与「待注入区」，不做构造、不判定标志。
/// </summary>
public class BehaviourInstanceHub : BasicSingle<BehaviourInstanceHub>
{
    private BehaviourInstanceHub() { }

    private readonly List<BasicObjectBehaviorImp> _behaviourStack = new List<BasicObjectBehaviorImp>();
    private readonly Dictionary<string, BasicObjectBehaviorImp> _pendingBehaviours = new Dictionary<string, BasicObjectBehaviorImp>();

    public IReadOnlyList<BasicObjectBehaviorImp> Behaviours => _behaviourStack;

    public void AddToStack(BasicObjectBehaviorImp behaviour) => _behaviourStack.Add(behaviour);

    public void AddPending(string registerKey, BasicObjectBehaviorImp behaviour)
        => _pendingBehaviours[registerKey] = behaviour;

    public bool TryTakePending(string registerKey, out BasicObjectBehaviorImp behaviour)
        => _pendingBehaviours.TryGetValue(registerKey, out behaviour);

    public void RemovePending(string registerKey) => _pendingBehaviours.Remove(registerKey);

    public Dictionary<string, BasicObjectBehaviorImp>.ValueCollection PendingValues
        => _pendingBehaviours.Values;

    public void ClearAll()
    {
        _behaviourStack.Clear();
        _pendingBehaviours.Clear();
    }

    // 加入 BehaviourInstanceHub
    public void ClearPending() => _pendingBehaviours.Clear();

    /// <summary>
    /// 统一销毁：遍历运行栈执行 LogicDestroy（退订由行为自身完成），再清空栈与待注入区。
    /// 由 GlobalConter 在场景切换/全局退出时调用。
    /// </summary>
    public void DestroyAll()
    {
        foreach (BasicObjectBehaviorImp behaviour in _behaviourStack)
        {
            behaviour?.LogicDestroy();
        }
        ClearAll();
    }
}
