using UnityEngine;

/// <summary>
/// 全局引导入口（MonoBehaviour，DontDestroyOnLoad）。
/// 仅负责：启动编排（扫描→逻辑行为实例化→启动主UI→状态机）、全局销毁入口。
/// 不持有行为实例列表——实例仓库与销毁调度统一归 BehaviourInstanceHub。
/// </summary>
public class GlobalConter : MonoBehaviour
{
    private static GlobalConter instance;
    public static GlobalConter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalConter>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GlobalConter");
                    instance = go.AddComponent<GlobalConter>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private bool _initialized;

    /// <summary>Unity 启动自举：场景加载前自动访问 Instance，触发整个启动链。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (!_initialized)
        {
            _initialized = true;
            StartCoroutine(InitializeCoroutine());
        }
    }

    /// <summary>
    /// 协程分阶段启动：先做不依赖场景的初始化（扫描/配置/输入），
    /// 等一帧让场景加载完成后再实例化行为与打开 UI，避免 BeforeSceneLoad 阶段 FindObjectOfType 返回 null。
    /// </summary>
    private System.Collections.IEnumerator InitializeCoroutine()
    {
        // 阶段1：不依赖场景的初始化
        _ = SingleMono.Instance;
        _ = SingleUIMono.Instance;
        AssemblyScanner.ScanBehaviours();
        AssemblyScanner.ScanConfigs();
        LoadAllConfigs();
        SetupDefaultInput();
        Debug.Log("[GlobalConter] 阶段1完成：扫描/配置/输入就绪");

        // 阶段2：等场景加载完成
        yield return null;
        yield return null;

        // 确保 EventSystem 存在（UI 鼠标点击必需）
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // 优先使用新 Input System 的模块，回退到旧 StandaloneInputModule
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                es.AddComponent(inputModuleType);
            else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GlobalConter] 自动创建 EventSystem");
        }

        // 阶段3：实例化行为与打开 UI（此时场景对象已存在）
        FactoryCenter.Instance.BootstrapInitialBehaviours();
        OpenMainUI();
        GameStateMachine.Instance.Initialize();

        Debug.Log("[GlobalConter] 引导完成");
    }

    /// <summary>加载 Resources/Config 下跑酷相关 JSON 配置到 ConfigCenter。</summary>
    private void LoadAllConfigs()
    {
        var cc = ConfigCenter.Instance;
        cc.LoadConfig<Player>("Config/player");
        cc.LoadConfig<BackgroundParallaxData>("Config/background");
        cc.LoadConfig<ScoreRollingData>("Config/scorerolling");
        cc.LoadConfig<SpawnPositionData>("Config/levelbuild");
        // 飞行射击配置（playership/enemyship/bullet）由对应模块自行加载，此处不硬编码。
        // UIManager 无私有配置数据，按钮路径硬编码在行为类内。
    }

    /// <summary>配置 InputCenter 默认轴与按键映射。</summary>
    private void SetupDefaultInput()
    {
        InputCenter.Instance.SetAxisMap("Horizontal", "Horizontal");
        InputCenter.Instance.SetAxisMap("Vertical", "Vertical");
        InputCenter.Instance.SetKeyMap("Dash", "LeftShift");
        InputCenter.Instance.InputEnable = true;
    }

    /// <summary>找 Registry 中第一个 UIBehavior 子类且 isBuild=true 的 key，打开为主 UI。</summary>
    private void OpenMainUI()
    {
        BehaviourTypeRegistry registry = BehaviourTypeRegistry.Instance;
        foreach (string key in registry.GetKeys())
        {
            if (!registry.TryGetType(key, out System.Type type)) continue;
            if (!typeof(UIBehavior).IsAssignableFrom(type)) continue;
            if (!registry.IsInitialLoad(key)) continue;

            UICenter.Instance.OpenUI(key);
            return;
        }
        Debug.LogWarning("[GlobalConter] 没有找到 isBuild=true 的主UI行为");
    }

    /// <summary>统一销毁入口：逻辑行为交 Hub 销毁，UI 交 UICenter 销毁。</summary>
    public void DestroyAllBehaviours()
    {
        BehaviourInstanceHub.Instance.DestroyAll();
        UICenter.Instance.DestroyAllUI();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
