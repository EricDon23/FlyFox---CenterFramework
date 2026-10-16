# Assetest 脚本框架说明

统一架构：**Center（框架基础，冻结） + Action（行为/逻辑） + Data（纯数据） + Enum + resource**。
框架内部的全局状态（Global / GameStateMachine / ScoreCalculator）位于 `Center/global/`，引导入口 `GlobalConter` 位于 `Center/Bootstrap/`。

---

## 目录结构（实际）

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

## 核心链路（真实时序）

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

## 三层分离

| 层 | 位置 | 职责 |
|---|---|---|
| 数据 Data | `Data/` | `BasicDate` 子类，字段 + 只读属性 + 得失函数（`Heal`/`ApplyDamage`/`Set*`），无业务逻辑 |
| 逻辑 Logic | `Action/…`（与行为同置） | 纯 C# 类（`PlayerAction`、`LevelBuild`、`ScoreRollingLogic`），被行为持有 |
| 行为 Behavior | `Action/…` | `Objectbehavior` 子类，只做「绑定数据 → 创建逻辑 → 订阅事件 → 销毁退订」 |

---

## ⚠️ 当前工程状态（必读）

框架管线（特性扫描 → 注册 → 工厂实例化）代码已就位，但**业务侧尚未迁移到新管线**：

1. **现有 6 个行为类均未打 `[AutoRegisterBehaviour]`**，8 个数据类均未打 `[AutoRegisterConfig]`。
   → 扫描器扫到空表，启动时 `BootstrapInitialBehaviours` 空转，`isBuild=true` 构造器标记当前不生效。
2. **`UIManagerBehavior`/`ScoreRollingBehavior` 继承的是 `Objectbehavior` 而非 `UIBehavior`**，不走 UI 泵自动订阅，也不被 `OpenMainUI` 识别。
3. **`InputCenter` 无 `SetKeyMap`/`SetAxisMap` 调用**，`PlayerBehavior.GetMoveVector()` 运行时返回零向量。
4. **`ConfigCenter.LoadConfig` 无调用方**，`Resources/Config/` 下已有 4 个 json 但当前不被加载。

> 详细断点与修复优先级见 `Center/Framework.md` 第 7 节。**新增内容**请严格按下方「配置指南」写正确代码；**存量旧内容**待 owner 授权后统一迁移。

---

## 资源现状（Assets/Resources/）

| 类型 | 已有 | 缺失 |
|---|---|---|
| 预制体 | `Player.prefab`、`Background.prefab`、`LevelBuild.prefab` | `Item.prefab`、`ScoreRolling.prefab`、UI 主面板预制体 |
| 配置 JSON | `Config/player.json`、`Config/background.json`、`Config/levelbuild.json`、`Config/scorerolling.json` | 暂无（接线后即可生效） |

---

## 配置指南（新增一个行为的正确姿势）

### 1. 写数据类（Data/）

继承 `BasicDate`，打 `[AutoRegisterConfig("Key")]`：

```csharp
[AutoRegisterConfig("Player")]
[Serializable]
public class Player : BasicDate
{
    [SerializeField] private int currentHealth = 1;
    public int Health => currentHealth;
    public void ApplyDamage(int amount) => currentHealth = Mathf.Max(currentHealth - amount, 0);
}
```

### 2. 写行为类（Action/…）

继承 `Objectbehavior`（UI 则继承 `UIBehavior`），打 `[AutoRegisterBehaviour("Key")]`，构造器里按需要设标志：

```csharp
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

`Assets/Resources/Config/player.json`，键名与序列化字段名一致。配置缺失时自动回退到数据类字段默认值（config-first）。

### 4. 预制体（实体行为必需）

`Assets/Resources/<Key>.prefab`，文件名与 `[AutoRegisterBehaviour("Key")]` 的 key 一致。全局行为（`isGlobal=true`）不需要预制体。

### 5. 碰撞/触发桥

把 `TriggerBridge2D/3D` 或 `CollisionBridge2D/3D` 直接挂到场景/预制体物体上，用 `BindEnterEvent` 等注册回调。桥是普通 MonoBehaviour，**不走工厂、不打特性**。

---

## 快速上手

新增行为的完整步骤、模版代码、`isBuild/isGlobal` 语义、UI 行为写法、单元测试说明见 [开发教程.md](./开发教程.md)。
框架架构、组件职责、启动时序、合规性审查见 [Center/Framework.md](./Center/Framework.md)。

---

#总结

本版 README.md 基于实际源码核对重写：① 修正目录结构（GlobalConter 在 Center/Bootstrap/，Global 状态在 Center/global/，不存在根目录 global/）；② 核心链路改为 SingleMono.Awake 嵌套触发 GlobalConter.Initialize 的真实时序；③ 新增「当前工程状态」一节，明确列出 4 个未接线断点（业务类未打特性、UI 行为未继承 UIBehavior、Input 未映射、Config 未加载）；④ 更新资源现状（已有 3 个预制体 + 4 个 JSON，缺 Item/ScoreRolling/UI 预制体）；⑤ 配置指南改为以特性驱动为准的正确写法，旧文档中「手动在 GlobalConter 三段登记」的说法已废弃。

> 声明：本稿结论均来自对 Center/、Action/、Data/ 源文件的直接阅读；未修改任何代码，仅更新文档。存量业务类的特性补打与接线需 owner 授权后执行。
