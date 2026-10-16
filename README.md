# CenterFramework

> 
> A Unity Custom Game Framework | 自研Unity游戏框架
> 统一架构：**Center（框架基础，冻结） + Action（行为/逻辑） + Data（纯数据） + Enum + resource**

框架内部的全局状态（Global / GameStateMachine / ScoreCalculator）位于 `Center/global/`，引导入口 `GlobalConter` 位于 `Center/Bootstrap/`。

---

## 📁 目录结构

```
Assets/script/
├── Action/                          游戏行为 + 逻辑（按域分三层）
│   ├── CharacterBehavior/           角色/物品：
│   │                                  PlayerBehavior / PlayerAction
│   │                                  ItemBehavior / ScoreAction
│   │                                  FeedbackAnimation（普通 Mono，非行为）
│   ├── GameBehavior/               游戏行为：
│   │                                  LevelBuildBehavior / LevelBuild
│   │                                  BackgroundParallaxBehavior / BackgroundParallaxLogic
│   └── UIBehavior/                  UI 行为：
│                                      ScoreRollingBehavior / ScoreRollingLogic
│                                      UIManagerBehavior / UIManagerLogic
├── Center/                          框架基础（冻结：只修 bug，不新增）
│   ├── Bootstrap/                   SingleMono / SingleUIMono / GlobalConter
│   ├── Behavior/                    BasicObjectBehaviorImp / Objectbehavior / UIBehavior
│   ├── Event/                       EventCenter
│   ├── Factory/                     BehaviourTypeRegistry / BehaviourFactory / BehaviourInstanceHub / FactoryCenter
│   ├── Config/                      ConfigCenter / BasicDate
│   ├── Resource/                    ResCenter + ResModule/（Resources/AB/Addressable/Web/Editor）
│   ├── CollisionModuled/            CollisionBridge2D/3D + TriggerBridge2D/3D
│   ├── global/                      Global（静态数据）/ GameStateMachine / ScoreCalculator
│   ├── structure/                   ResHandle / AudioPlayRequest / Tag
│   ├── Utils/                       BasicSingle / AutoRegister*Attribute / JsonDataUtil / …
│   ├── AssemblyScanner.cs           程序集扫描器
│   ├── AssetPool.cs / AudioCenter.cs / InputCenter.cs / UI/UICenter.cs
│   └── Framework.md                 框架详细文档
├── Data/                            纯数据类（BasicDate 子类）：
│                                      Player / Score / Fruit / GridCell
│                                      BackgroundParallaxData / ScoreRollingData
│                                      SpawnPositionData / UIManagerData
├── Enum/                            GameEnum / AudioGroupType / GridType / ScoreShape / InterActionType
└── resource/                        配置/资源源文件
```

---

## ⚙️ 核心启动时序

```
flowchart LR
    A[首次访问 SingleMono.Instance] --> B[创建 GameObject:SingleMono + 挂载SingleMono组件]
    B --> C[SingleMono.Awake]
    C --> D[EnsureGlobalConterCreated 创建GlobalConter对象]
    D --> E[GlobalConter.Awake → Initialize()]
    E --> E1[SingleMono / SingleUIMono 事件泵就绪]
    E --> E2[AssemblyScanner 扫描行为、配置特性]
    E --> E3[FactoryCenter.BootstrapInitialBehaviours<br/>启动自动实例化行为 isBuild=true]
    E --> E4[OpenMainUI 自动加载主UI面板]
    E --> E5[GameStateMachine 初始化，默认进入Menu状态]
    E --> F[广播 AwakeEvent 事件]
    F --> G[后续每帧 SingleMono.Update/FixedUpdate<br/>EventCenter广播事件，驱动所有行为回调]
```

时序文本版：

```
首次访问 SingleMono.Instance
  → new GameObject("SingleMono") + AddComponent<SingleMono>()
    → SingleMono.Awake
      → EnsureGlobalConterCreated() → new GameObject("GlobalConter") + AddComponent<GlobalConter>()
        → GlobalConter.Awake → Initialize()
            1. SingleMono / SingleUIMono 事件泵就绪
            2. AssemblyScanner.ScanBehaviours() / ScanConfigs()   // 特性扫描
            3. FactoryCenter.BootstrapInitialBehaviours()        // isBuild=true 的非 UI 行为
            4. OpenMainUI()                                     // 遍历 Registry 找第一个 UIBehavior
            5. GameStateMachine.Initialize()                     // 状态事件 + 置 Menu
      → RegisterEvent + Broadcast("AwakeEvent")
  → 之后每帧 SingleMono.Update/FixedUpdate → EventCenter 广播 → 行为回调
```

---

## 🧩 三层分离架构

| 分层 | 存放路径 | 核心职责 |
| --- | --- | --- |
| 数据 Data | `Data/` | `BasicDate` 子类，字段 + 只读属性 + 修改方法（`Heal`/`ApplyDamage`/`Set*`），**无业务逻辑** |
| 逻辑 Logic | `Action/` 对应目录 | 纯C#普通类（PlayerAction、LevelBuild等），由Behavior持有，承载业务逻辑 |
| 行为 Behavior | `Action/` 对应目录 | `Objectbehavior` 子类，只负责：绑定数据 → 创建逻辑实例 → 订阅事件 → 销毁退订 |

---

## ⚠️ 当前工程状态（必读）

框架管线（特性扫描 → 注册 → 工厂实例化）代码已就位，但**业务侧尚未迁移到新管线**：

1. 现有 6 个行为类均未打 `[AutoRegisterBehaviour]`，8 个数据类均未打 `[AutoRegisterConfig]`。
→ 扫描器扫到空表，启动时 `BootstrapInitialBehaviours` 空转，`isBuild=true` 构造器标记当前不生效。
2. `UIManagerBehavior`/`ScoreRollingBehavior` 继承的是 `Objectbehavior` 而非 `UIBehavior`，不走 UI 泵自动订阅，也不被 `OpenMainUI` 识别。
3. `InputCenter` 无 `SetKeyMap`/`SetAxisMap` 调用，`PlayerBehavior.GetMoveVector()` 运行时返回零向量。
4. `ConfigCenter.LoadConfig` 无调用方，`Resources/Config/` 下已有 4 个 json 但当前不被加载。

> 
> 详细断点与修复优先级见 `Center/Framework.md` 第 7 节。
> **新增内容**请严格按下方「配置指南」写正确代码；**存量旧内容**待 owner 授权后统一迁移。

---

## 📦 资源现状 Assets/Resources/

| 类型 | 已有资源 | 缺失资源 |
| --- | --- | --- |
| 预制体 | `Player.prefab`、`Background.prefab`、`LevelBuild.prefab` | `Item.prefab`、`ScoreRolling.prefab`、UI 主面板预制体 |
| 配置 JSON | `player.json`、`background.json`、`levelbuild.json`、`scorerolling.json` | 无（接线后即可生效） |

---

## 📝 配置指南：新增一个行为的标准流程

### 1. 编写数据类（Data/）

继承 `BasicDate`，添加 `[AutoRegisterConfig("Key")]`

```
[AutoRegisterConfig("Player")]
[Serializable]
public class Player : BasicDate
{
    [SerializeField] private int currentHealth = 1;
    public int Health => currentHealth;
    public void ApplyDamage(int amount) => currentHealth = Mathf.Max(currentHealth - amount, 0);
}
```

### 2. 编写行为类（Action/对应目录）

实体行为继承 `Objectbehavior`；UI行为继承 `UIBehavior`，打上 `[AutoRegisterBehaviour("Key")]`

```
[AutoRegisterBehaviour("Player")]
public class PlayerBehavior : Objectbehavior
{
    public PlayerBehavior() { isBuild = true; }   // 启动时自动实例化
    // isGlobal = true 时：不加载预制体、不绑数据，直读 Global 静态类
    public override void Init(GameObject obj, BasicDate objectDate) { /* … */ }
    public override void LogicAwake()  { /* Subscribe */ }
    public override void LogicDestroy(){ /* Unsubscribe */ }
}
```

### 3. 配置文件（可选）

`Assets/Resources/Config/player.json`，字段名与序列化字段保持一致。
配置缺失时自动回退到数据类内定义的默认值（config-first）。

### 4. 预制体（实体行为必需）

`Assets/Resources/<Key>.prefab`，文件名与 `[AutoRegisterBehaviour("Key")]` 的 key 保持一致。

> 
> 全局行为（`isGlobal=true`）不需要预制体。

### 5. 碰撞/触发桥模块

`TriggerBridge2D/3D` / `CollisionBridge2D/3D` 直接挂载在场景/预制体物体上，调用 `BindEnterEvent` 注册回调。

> 
> 桥组件是普通MonoBehaviour，**不走工厂、不需要特性标记**。

---

## 🚀 快速上手

- 新增行为完整步骤、代码模板、`isBuild/isGlobal` 语义、UI行为写法、单元测试：[开发教程.md](./%E5%BC%80%E5%8F%91%E6%95%99%E7%A8%8B.md)
- 框架架构、模块职责、启动时序、合规审查：[Center/Framework.md](./Center/Framework.md)

---

