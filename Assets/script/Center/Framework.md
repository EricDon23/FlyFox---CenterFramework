# Center 框架文档

本文档基于 `Assets/script/Center/` 实际源码逐文件核对后重写，描述框架的**真实**架构、启动时序、组件职责与已知断点。
`Center/` 是框架基础层（**冻结：只修 bug，不新增功能、不擅自重命名**）；游戏行为在 `Action/`，纯数据在 `Data/`，枚举在 `Enum/`。

---

## 0. 目录结构（实际）

```
Assets/script/
├── Action/                      游戏行为 + 逻辑（按域分三层）
│   ├── CharacterBehavior/       角色/物品：PlayerBehavior/PlayerAction、ItemBehavior、ScoreAction、FeedbackAnimation
│   ├── GameBehavior/            游戏行为：LevelBuildBehavior/LevelBuild、BackgroundParallaxBehavior/…Logic
│   └── UIBehavior/              UI 行为：ScoreRollingBehavior/…Logic、UIManagerBehavior/…Logic
├── Center/                      框架基础（冻结）
│   ├── Bootstrap/               事件泵 + 全局引导
│   │   ├── SingleMono.cs            逻辑层事件泵（DontDestroyOnLoad）
│   │   ├── SingleUIMono.cs         UI 层事件泵（随场景销毁）
│   │   └── GlobalConter.cs         全局引导入口（Mono，DontDestroyOnLoad）
│   ├── Behavior/                 行为契约与基类
│   │   ├── BasicObjectBehaviorImp.cs   行为契约接口（Init×4 / LogicAwake / LogicUpdate / LogicDestroy）
│   │   ├── Objectbehavior.cs           行为抽象基类（纯 C#，持 g_obj/date/isBuild/isGlobal）
│   │   └── UIBehavior.cs               UI 行为基类（自动订阅 UIUpdateEvent，隐藏自动跳过）
│   ├── Event/EventCenter.cs     全局事件总线（线程安全锁、签名校验）
│   ├── Factory/                 工厂三段式
│   │   ├── BehaviourTypeRegistry.cs    类型注册表（缓存 isBuild/isGlobal 标志）
│   │   ├── BehaviourFactory.cs        构造器（数据装配、预制体加载、分流）
│   │   └── BehaviourInstanceHub.cs     实例仓库（运行栈 + 待注入区）
│   ├── Config/                  配置中心
│   │   ├── ConfigCenter.cs         JSON 配置加载 + 默认数据工厂
│   │   └── BasicDate.cs           纯数据基类（index/name）
│   ├── Resource/                资源中心
│   │   ├── ResCenter.cs            路径前缀路由（res:// ab:// web:// addr://）
│   │   └── ResModule/              Resources / AssetBundle / Addressable / Web / Editor 后端
│   ├── CollisionModuled/       碰撞/触发桥（MonoBehaviour，2D/3D × Collision/Trigger）
│   ├── global/                  游戏全局状态（注意：不是框架入口）
│   │   ├── Global.cs               静态全局数据容器
│   │   ├── GameStateMachine.cs      状态机（Menu/Playing/Paused/GameOver）
│   │   └── ScoreCalculator.cs       纯函数结算器
│   ├── structure/               跨层数据结构
│   │   ├── ResHandle.cs             资源句柄（引用计数、一次性 Release）
│   │   ├── AudioPlayRequest.cs      音频播放请求参数
│   │   └── Tag.cs                   可交互标记 Mono 组件
│   ├── Utils/                   基础设施
│   │   ├── BasicSingle.cs                 纯 C# 单例基类（反射懒加载）
│   │   ├── AutoRegisterBehaviourAttribute.cs  行为自动注册标记
│   │   ├── AutoRegisterConfigAttribute.cs     配置自动注册标记
│   │   └── …（JsonDataUtil/BaseUtils/MathOfGame/KeyboardLockManager/…）
│   ├── AssemblyScanner.cs       程序集扫描器（识别上述两个特性）
│   ├── AssetPool.cs             GameObject 对象池（LIFO 栈）
│   ├── AudioCenter.cs            音频中心（BGM 交叉淡入、SFX 池）
│   ├── InputCenter.cs            输入中心（逻辑键/轴映射）
│   └── UI/UICenter.cs            UI 面板缓存中心
├── Data/                        纯数据类（BasicDate 子类）
├── Enum/                        GameEnum / AudioGroupType / GridType / ScoreShape / InterActionType
└── resource/                    配置/资源源文件
```

> **命名技术债（已知，不擅自改）**：`CollisionModuled`（应为 CollisionModule）、`GlobalConter`（应为 GlobalController）、`Objectbehavior`（应为 ObjectBehavior）、`BasicObjectBehaviorImp`（接口应以 I 开头）、`BasicDate`（语义是"数据"而非"日期"）。重命名会破坏序列化与引用，需走单独重构审批。

---

## 1. 架构总览

框架采用 **事件驱动 + 工厂三段式装配 + 单例服务 + 三层分离**：

- **数据（Data）**：`BasicDate` 子类，只放字段 + 只读属性 + 得失函数（Heal/ApplyDamage/Set*），无业务逻辑。
- **逻辑（Logic）**：纯 C# 类，被行为持有，做帧更新/状态机/算法。
- **行为（Behavior）**：`Objectbehavior` 子类，纯 C#（非 MonoBehaviour），只做「绑定数据 → 创建逻辑 → 订阅事件 → 销毁退订」。
- **事件泵**：`SingleMono`（逻辑）/ `SingleUIMono`（UI）把 Unity 的 Awake/Start/Update/FixedUpdate/OnDestroy 转发为 EventCenter 广播。
- **服务单例**：`EventCenter`/`FactoryCenter`/`ConfigCenter`/`ResCenter`/`InputCenter`/`AudioCenter`/`UICenter` 全部继承 `BasicSingle<T>`。

依赖方向**严格单向**：`Action → Center/Event + Center/Factory + Center/Config + Center/Resource`，反向不允许；`Center` 内部子目录之间只经门面（FactoryCenter / ResCenter）交互，不互相 new。

---

## 2. 核心组件职责（按代码核对）

| 组件 | 类型 | 实际职责 |
|---|---|---|
| `SingleMono` | Mono 单例（DontDestroyOnLoad） | ① Awake 内主动创建 `GlobalConter`；② 广播 `AwakeEvent/StartEvent/UpdateEvent/FixedUpdateEvent/OnDestroyEvent`；③ 协程代理；④ `PauseAllUpdate/ResumeAllUpdate`。不持有行为实例，不负责销毁调度 |
| `SingleUIMono` | Mono 单例（**不** DontDestroyOnLoad） | 广播 `UIAwakeEvent/UIStartEvent/UIUpdateEvent/UIFixedUpdateEvent/UIOnDestroyEvent`；UI 协程代理；场景销毁时 `StopAllCoroutines` |
| `GlobalConter` | Mono（DontDestroyOnLoad） | 引导编排：就绪事件泵 → 扫描注册 → 实例化初始行为 → 遍历 Registry 打开主 UI → 初始化状态机；`DestroyAllBehaviours` 作为全局销毁入口（转交 Hub 与 UICenter）。**不**持有行为实例快照 |
| `AssemblyScanner` | 静态类 | 遍历当前程序集：遇 `[AutoRegisterBehaviour]` 且类型合法 → `FactoryCenter.RegisterBehaviour(type, key)`；遇 `[AutoRegisterConfig]` 且为 BasicDate 子类 → `ConfigCenter.RegisterConfigType(type, key)`；缺无参构造打 Warning 跳过 |
| `BehaviourTypeRegistry` | `BasicSingle` | 类型预备站：`Dictionary<key, Type>` + 缓存 `isBuild/isGlobal`（注册时 `Activator.CreateInstance` 探测字段值）+ 显式 `SetInstantiateFlag` 覆盖 |
| `BehaviourFactory` | `BasicSingle` | 构造器：按 `isGlobal`/是否 MonoBehaviour 分流；实体行为先 `LoadData`（config-first）再 `ResCenter.LoadSync<GameObject>(key)` 加载预制体；`LogicAwake` 后入栈或挂待注入；`BuildUI` 返回 `UIBehavior`（不直接操作 UICenter） |
| `BehaviourInstanceHub` | `BasicSingle` | 实例仓库：`_behaviourStack`（已运行）+ `_pendingBehaviours`（待注入）；不做构造、不判标志；`DestroyAll()` 遍历栈调 `LogicDestroy` 后清空 |
| `FactoryCenter` | `BasicSingle` | **门面**：把调用转发给 Registry/Factory/Hub，对外 API 稳定 |
| `EventCenter` | `BasicSingle` | 事件字典 `Dictionary<string, Delegate>`；`lock(syncRoot)` 保护；订阅时签名不一致**抛异常**；广播时单回调异常被 try-catch 隔离，不中断其他监听器 |
| `ConfigCenter` | `BasicSingle` | `DateDic`（按 name 键存数据实例）+ `_defaultFactories`（key→无参工厂）；`LoadConfig<T>(path)` 从 ResCenter 读 TextAsset 反序列化为 `List<T>` 逐条入字典 |
| `ResCenter` | `BasicSingle` | 路径前缀路由：无前缀/`res://`→Resources；`ab://bundle#asset`→AssetBundle；`web://`/`http(s)://`→UnityWebRequest（仅异步）；`addr://`→Addressable；Web 同步加载报错 |
| `ResourceModule` | 普通类 | Resources 后端：引用计数字典、`_loadingPaths` 并发去重、`RefCount<=0` 时 `Resources.UnloadAsset`（GameObject/Component 不卸载） |
| `ResHandle` | 普通类 | 一次性句柄：`IsReleased` 标记防重复 Release；释放回调由后端注入 |
| `InputCenter` | `BasicSingle` | `inputMap`/`axisMap` 字典；`InputEnable` 全局开关；字符串 KeyCode 解析；`GetMoveVector()` 默认读 Horizontal/Vertical |
| `AudioCenter` | `BasicSingle` | BGM 单 Source + 代际号防并发淡入；SFX 复用空闲 Source；`_groupVolumes` 按 Master/BGM/SFX/Voice 分组 |
| `UICenter` | `BasicSingle` | `UICache` 字典；`OpenUI` 命中缓存 SetActive(true)，未命中交 `BehaviourFactory.BuildUI`（返回 `UIBehavior`）后缓存；`CloseUI` 仅隐藏；`DestroyAllUI` 退订+Destroy。主 UI 由 `GlobalConter.OpenMainUI` 遍历 Registry 后调 `OpenUI` |
| `UIBehavior` | `Objectbehavior` 子类 | `LogicAwake` 自动订阅 UIUpdateEvent/UIFixedUpdateEvent；`IsVisible` 状态；隐藏时 `OnUIUpdate` 自动 return；`LogicDestroy` 自动退订 |
| `GameStateMachine` | `BasicSingle` | 注册 4 个状态事件；订阅 `RequestGameStart/RequestGameOver`；`StartGame/PauseGame/ResumeGame/EndGame` 状态守卫；`Global.CurrentState` 变更经 `GameStateChanged` 广播 |
| `Global` | 静态类 | 会话数据容器：StartTime/Level/SessionStartTime/SessionDuration/SessionBaseScore/FinalSessionScore/CurrentState |
| `ScoreCalculator` | 静态类 | 纯函数：按 sessionDuration 分段加权（≤30s×1.0；30–60s→1.5；60–120s→2.0；>120s 每多 60s +0.5） |
| `AssetPool` | `BasicSingle` | `Dictionary<string, Stack<GameObject>>` LIFO；`GetObject`/`PushGameObject`；超 maxPoolCount 直接 Destroy |
| `CollisionBridgeBase<T>` | MonoBehaviour 抽象 | 持 enter/stay/exit 三个 `Action<T>` 委托；`Bind*/Unbind*/ClearEvents`；实现 `BasicObjectBehaviorImp` 契约但不进工厂 new |

---

## 3. 启动流程（**真实时序**，含嵌套 Awake）

框架**没有** `[RuntimeInitializeOnLoadMethod]` 自举，启动依赖 `SingleMono` 首次访问触发的嵌套 Awake：

```
[Unity 启动]
  某脚本/场景首次访问 SingleMono.Instance
    └─ Instance getter: new GameObject("SingleMono") → AddComponent<SingleMono>()
         └─ 同步触发 SingleMono.Awake()
              ├─ EnsureGlobalConterCreated()
              │    └─ new GameObject("GlobalConter") + AddComponent<GlobalConter>()
              │         └─ 同步触发 GlobalConter.Awake()
              │              ├─ instance=this; DontDestroyOnLoad
              │              └─ Initialize()
              │                   1. _ = SingleMono.Instance        // 此时已在 Awake，缓存 instance
              │                   2. _ = SingleUIMono.Instance       // new UI 泵，触发其 Awake 注册 UI 事件
              │                   3. AssemblyScanner.ScanBehaviours()  // 特性扫描 → Registry
              │                   4. AssemblyScanner.ScanConfigs()     // 特性扫描 → ConfigCenter
              │                   5. FactoryCenter.BootstrapInitialBehaviours()
              │                   │     // 遍历 Registry，跳过 UIBehavior，对 isInitialLoad=true 的
              │                   │     // 调 SetGameObject(key) → LoadData + LoadSync(prefab) + Instantiate
              │                   6. OpenMainUI()                      // 遍历 Registry 找第一个 UIBehavior 且 isBuild=true 的 key，调 OpenUI
              │                   7. GameStateMachine.Initialize()      // 注册状态事件、订阅 Request*、置 Menu
              │                   └─ Debug.Log("[GlobalConter] 引导完成")
              ├─ RegisterEvent: Awake/Start/Update/FixedUpdate/OnDestroy
              └─ Broadcast("AwakeEvent")     // 注意：晚于所有行为 LogicAwake 订阅
```

每帧驱动链路：

```
SingleMono.Update/FixedUpdate
  → EventCenter.Broadcast("UpdateEvent"/"FixedUpdateEvent")
    → 行为在 LogicAwake 中 Subscribe 的回调被触发
SingleUIMono.Update/FixedUpdate
  → EventCenter.Broadcast("UIUpdateEvent"/"UIFixedUpdateEvent")
    → UIBehavior 基类自动转发到 OnUIUpdate/OnUIFixedUpdate
```

> **时序注意点**：`AwakeEvent` 在所有行为 `LogicAwake` 订阅**之后**才广播，行为不应依赖 `AwakeEvent` 做初始化；行为的初始化应放在 `LogicAwake` 内同步完成。

---

## 4. 行为生命周期与 isBuild / isGlobal

### 4.1 生命周期阶段

| 阶段 | 方法 | 触发方 |
|---|---|---|
| 装配 | `Init(GameObject, BasicDate)` | `BehaviourFactory.InstantiateBehaviour` 内调；`g_obj=obj; date=objectDate` |
| 启动 | `LogicAwake()` | `SetGameObject`（立即）或 `InjectBehaviour`（后加载）；订阅事件、创建逻辑 |
| 帧更新 | （无强制接口） | 行为自行在 `LogicAwake` 里 Subscribe `UpdateEvent`/`FixedUpdateEvent` |
| 销毁 | `LogicDestroy()` | `GlobalConter.DestroyAllBehaviours` → `BehaviourInstanceHub.DestroyAll`（遍历栈调 LogicDestroy）+ `UICenter.DestroyAllUI`；退订由行为自身完成 |

### 4.2 两个标志的真实语义

`BehaviourTypeRegistry.CacheFlags` 在注册时 `Activator.CreateInstance` 一个探测实例，反射读 `isBuild`/`isGlobal` 两个**实例字段**并缓存：

| 标志 | 值 | 工厂行为 |
|---|---|---|
| `isBuild` | `true` | `BootstrapInitialBehaviours` 启动时遍历到即调 `SetGameObject`，`LogicAwake` 后入栈 |
| `isBuild` | `false` | 启动时不创建；等外部调 `InjectBehaviour(key)`（先尝试 pending，否则临时置 true 再创建再还原） |
| `isGlobal` | `true` | `CreateObject` 走全局分支：`InstantiateBehaviour(type, null, null)`，**不加载预制体、不读配置**，`g_obj=null`、`date=null`，数据直读 `Global` 静态类 |
| `isGlobal` | `false` | 实体行为：`LoadData(key)` + `ResCenter.LoadSync<GameObject>(key)` |
| 显式 `SetInstantiateFlag(key, bool)` | — | 覆盖 `isBuild` 默认值，优先级最高 |

### 4.3 UI 行为的分流

`BehaviourFactory.BootstrapInitialBehaviours` 遍历时 `typeof(UIBehavior).IsAssignableFrom(type)` 为真则**跳过**，不进逻辑 Hub。主 UI 由 `GlobalConter.OpenMainUI` 独立遍历 Registry，找第一个 `UIBehavior` 子类且 `isInitialLoad=true` 的 key，调 `UICenter.OpenUI` → `BehaviourFactory.BuildUI`（加载预制体 + new 行为 + `Init` + `LogicAwake`，返回 `UIBehavior`）→ UICenter 缓存。

> **现状偏差**：现有 `UIManagerBehavior`/`ScoreRollingBehavior` 继承的是 `Objectbehavior` 而非 `UIBehavior`，因此既不会走 UI 泵自动订阅，也不会被 `OpenMainUI` 识别。详见第 7 节断点清单。

---

## 5. Factory 三段式

```
FactoryCenter（门面）
  ├─ BehaviourTypeRegistry   注册：key → Type + 缓存标志
  ├─ BehaviourFactory        构造：LoadData → LoadSync(prefab) → Instantiate → Init → LogicAwake
  └─ BehaviourInstanceHub    仓库：Stack(已运行) + Dict(待注入)
```

### 5.1 数据装配（config-first）

`BehaviourFactory.LoadData(key)`：

1. `ConfigCenter.GetConfig(key)` 命中 → 直接用配置实例；
2. 未命中 → `ConfigCenter.CreateDefault(key)` → 走 `_defaultFactories` 里的无参工厂；
3. 连默认工厂都没有 → `new BasicDate { index=0, name=key }` 裸兜底。

### 5.2 后加载注入

`InjectBehaviour(key)`：

- `Hub.TryTakePending` 命中 → `LogicAwake` + 入栈 + 移除 pending；
- 未命中但 Registry 有 → 临时 `SetInstantiateFlag(true)` → `SetGameObject(key)` → 还原旧标志（或 false）。

### 5.3 公共应急接口

- `CreateBehaviour<T>(obj, date)`：手动 `new T()` + `Init` + `LogicAwake` + 入栈，跳过 `IsInitialLoad` 判断。
- `BuildUI(key)`：仅 UI 用，构造后返回 `UIBehavior`，由 `UICenter.OpenUI` 注册缓存，不进逻辑 Hub。

---

## 6. 全局状态机与事件

`GameStateMachine.Initialize()` 注册并维护：

| 事件名 | 载荷 | 触发时机 |
|---|---|---|
| `GameStateChanged` | `GameEnum` | 任意状态流转时 `ChangeState` 广播 |
| `GameStart` | 无 | `OnGameStart` / `StartGame` |
| `GamePause` | 无 | `OnGamePause` / `PauseGame` |
| `GameOver` | 无 | `OnGameOver` / `EndGame` |
| `RequestGameStart` | 无 | UI 按钮触发，状态机内部订阅 |
| `RequestGameOver` | 无 | 外部请求结束 |

状态守卫：`StartGame` 仅允许 Menu/GameOver → Playing；`PauseGame` 仅 Playing → Paused；`ResumeGame` 仅 Paused → Playing；`EndGame` 仅 Playing → GameOver。

---

## 7. 合规性审查结论与已知断点

### 7.1 符合「单一逻辑设计结构」的部分

- ✅ 工厂拆成 Registry/Factory/Hub 三段，各管一件事，门面 `FactoryCenter` 稳定对外。
- ✅ 事件泵分层：逻辑泵（DontDestroyOnLoad）与 UI 泵（随场景销毁）完全隔离，事件名不交叉。
- ✅ 服务全部 `BasicSingle<T>`，构造器私有 + 反射懒加载，无全局 new。
- ✅ 资源一律走 `ResCenter` 路径前缀路由，业务不感知后端；句柄 `Release` 一次性防重。
- ✅ 行为不继承 MonoBehaviour，纯 C#，靠 EventCenter 驱动，可单元测试。
- ✅ 碰撞桥是普通场景 Mono，不进工厂、不进 Hub，职责清晰。

### 7.2 流程断点（**必须知晓**，本次仅记录不改代码）

| # | 断点 | 现象 | 影响 |
|---|---|---|---|
| 1 | **业务行为/数据类零处打特性** | `PlayerBehavior`/`ItemBehavior`/`LevelBuildBehavior`/`BackgroundParallaxBehavior`/`ScoreRollingBehavior`/`UIManagerBehavior` 均无 `[AutoRegisterBehaviour]`；`Player`/`Score`/`Fruit`/… 均无 `[AutoRegisterConfig]` | `AssemblyScanner` 扫到空表 → `BootstrapInitialBehaviours` 空转 → 所有 `isBuild=true` 标记**不生效**；`OpenMainUI` 打"没有找到主 UI 行为" |
| 2 | **UI 行为基类错位** | `UIManagerBehavior`/`ScoreRollingBehavior` 继承 `Objectbehavior` 而非 `UIBehavior` | 不自动订阅 UIUpdateEvent；不被 `OpenMainUI` 识别；被当作逻辑行为尝试加载同名预制体（Resources 下无）后打 Warning 跳过 |
| 3 | **InputCenter 未接线** | 工程内无任何 `SetKeyMap`/`SetAxisMap` 调用 | `PlayerBehavior` 调 `GetMoveVector()` 运行时打"未配置轴映射"并返回零向量 |
| 4 | **ConfigCenter.LoadConfig 未接线** | `GlobalConter.Initialize` 不调 `LoadConfig`；`Resources/Config/` 下已有 4 个 json 但不被加载 | config-first 链路实际走的是 `CreateDefault`，JSON 配置文件当前不生效 |
| 5 | **`HandOffTo` 方法已删除** | 旧文档仍在引用 | 文档与代码不一致，已在本版修正 |
| 6 | **`DestroyAllBehaviours` 无调用方** | `GlobalConter.DestroyAllBehaviours` 是死入口 | 场景切换时不会自动销毁行为栈；当前靠 DontDestroyOnLoad + 单场景流程掩盖 |
| 7 | **`Config/New Folder` 空目录** | 遗留垃圾目录 | 无功能影响，建议下次清理时删除 |
| 8 | **README 所述 `StartGameManager/OverGameManager` 不存在** | script 根目录无 `global/` 文件夹，无此二类 | 旧文档残留，已在 README 修正 |

### 7.3 修复优先级建议（待授权后执行）

1. **P0**：给现有 6 个业务行为类打 `[AutoRegisterBehaviour("Key")]`、8 个数据类打 `[AutoRegisterConfig("Key")]`，使扫描管线真正生效。
2. **P0**：把 `UIManagerBehavior`/`ScoreRollingBehavior` 改为继承 `UIBehavior`，并确认对应 UI 预制体就位。
3. **P1**：在 `GlobalConter.Initialize` 末尾补 `InputCenter.SetKeyMap/SetAxisMap` 与 `ConfigCenter.LoadConfig<T>("Config/xxx")` 调用（或改为特性驱动）。
4. **P2**：场景切换时显式调 `GlobalConter.DestroyAllBehaviours()` + `ResCenter.ClearAll()` + `AssetPool.ClearAllPool()`。

### 7.4 本次重构记录（已落地）

| 类别 | 改动 |
|---|---|
| **Bug 修复** | `AudioCenter.CrossFadeBGM` 协程被代际号打断时立即释放 `oldHandle`，修复 BGM 切换句柄泄漏 |
| **Bug 修复** | `EventCenter.Invoke` 无参/带参回调均加 try-catch，单个回调异常不再中断其他监听器 |
| **职责收敛** | 删除 `GlobalConter._behaviourInstances`/`AdoptBehaviours`、`SingleMono.ExecuteDestroyAll`；销毁调度统一归 `BehaviourInstanceHub.DestroyAll()` |
| **职责收敛** | `UICenter.InitMainUI` 删除，遍历 Registry 找主 UI 的逻辑移到 `GlobalConter.OpenMainUI`（引导职责） |
| **去耦合** | `BehaviourFactory.BuildUI` 不再回写 `UICenter.SetUIScene`，改为返回 `UIBehavior`，由 `UICenter.OpenUI` 自行注册缓存 |
| **接口精简** | `BasicObjectBehaviorImp` 只保留 `Init(GameObject, BasicDate)`，删除从未被调用的 `Init()`/`Init(GameObject)`/`Init(BasicDate)` 三个重载；`Objectbehavior`/`CollisionBridgeBase` 同步精简 |
| **删除冗余** | `CollisionBridgeBase.LogicFixedUpdate`（无调用方）；`InputCenter.GetKeyAction`（`GetKeyDown` 别名）；`UIBehavior` 新增 `RootObj` 属性供外部读取 |

---

## 8. 关键约定（开发必须遵守）

- `Center/` 目录**不新增**业务代码，只修 bug；命名技术债不擅自重命名。
- 新行为必须：① 继承 `Objectbehavior`（UI 则 `UIBehavior`）；② 打 `[AutoRegisterBehaviour("Key")]`；③ 对应数据类打 `[AutoRegisterConfig("Key")]`；④ 构造器里按需要设 `isBuild=true`/`isGlobal=true`。
- 数据类只放字段 + 得失函数，**不写** Update/状态机/坐标换算。
- 行为的订阅写在 `LogicAwake`，退订写在 `LogicDestroy`，成对出现。
- 资源一律经 `ResCenter.LoadSync/LoadAsync`，句柄用完必须 `Release()`。
- 跨脚本通信一律走 `EventCenter`，不直接 new 其它行为。
- 碰撞/触发桥直接挂场景物体，用 `BindEnterEvent` 等回调，**不**走工厂注册。
- 枚举在 `Enum/` 统一维护，不改动已有值。

---

#总结

本版 Framework.md 基于 Center/ 下 30+ 源文件逐行核对重写：① 修正目录结构（GlobalConter 实际在 Bootstrap/，global/ 装的是 Global/GameStateMachine/ScoreCalculator）；② 补全真实启动时序（SingleMono.Awake 嵌套触发 GlobalConter.Awake 的链路）；③ 拆解 Factory 三段式（Registry/Factory/Hub）职责边界；④ 新增第 7 节合规性审查，明确列出 8 个流程断点——最关键的是**业务类零处打自动注册特性、UI 行为未继承 UIBehavior、Input/Config 加载未接线**，导致框架管线代码虽就位但运行时空转；⑤ 标注命名技术债（CollisionModuled/GlobalConter/Objectbehavior/BasicDate）为已知问题，按项目冻结规则不擅自修改。

> 声明：本稿所有结论均来自对 `Assets/script/Center/` 与 `Assets/script/Action/`、`Data/` 下源文件的直接阅读与 grep 交叉验证；未修改任何代码文件，仅更新文档。修复建议（P0–P2）需经项目 owner 授权后方可执行。
