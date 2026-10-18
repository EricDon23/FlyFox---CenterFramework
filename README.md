# CenterFramework

> A Unity Custom Game Framework | 自研Unity游戏框架
> 统一架构：**Center（框架基础，冻结） + Action（行为/逻辑） + Data（纯数据） + Enum**

---

## 📐 框架架构

### 核心设计理念

```
┌─────────────────────────────────────────┐
│  Center（框架核心，冻结只修 bug）       │
│  ├─ Bootstrap    启动引导             │
│  ├─ Event         事件中心             │
│  ├─ Factory       工厂注册与实例化      │
│  ├─ Config        配置中心             │
│  ├─ Resource      资源中心             │
│  ├─ Collision     碰撞桥接             │
│  ├─ Global        全局状态/状态机       │
│  └─ Utils         工具类               │
├─────────────────────────────────────────┤
│  Action（业务行为，可自由拓展）         │
│  ├─ CharacterBehavior  角色行为        │
│  ├─ GameBehavior       游戏行为        │
│  └─ UIBehavior         UI 行为          │
├─────────────────────────────────────────┤
│  Data（纯数据容器）                     │
│  └─ BasicDate 子类，无逻辑              │
└─────────────────────────────────────────┘
```

### 三层分离

| 分层 | 职责 | 特点 |
|---|---|---|
| **Data 层** | 纯数据存储，字段 + 属性 + 修改方法 | 无业务逻辑，可序列化 |
| **Logic 层** | 纯 C# 逻辑类，由 Behavior 持有 | 可测试，不依赖 Unity |
| **Behavior 层** | 绑定数据 + 订阅事件 + 驱动逻辑 | 继承 Objectbehavior / UIBehavior |

---

## ⚡ 核心特性

### 1. 自动注册机制
- `[AutoRegisterBehaviour("Key")]` 标记行为类
- `[AutoRegisterConfig("Key")]` 标记配置类
- 启动时程序集扫描，自动注册到工厂
- 零硬编码，新增行为只需打标签

### 2. 事件驱动
- `EventCenter` 全局事件中心
- 支持泛型参数广播
- 异常隔离，单个订阅者报错不影响整体
- 订阅/退订成对管理

### 3. 工厂模式
- `FactoryCenter` 统一创建行为实例
- `BehaviourRegistry` 维护 GameObject → Behavior 映射
- 销毁收敛到 Hub，统一调度

### 4. 资源管理
- `ResCenter` 统一资源加载入口
- 句柄制管理，引用计数自动释放
- 支持多源：Resources / AssetBundle / Addressable

### 5. 配置系统
- `ConfigCenter` 配置中心
- JSON 配置自动反序列化
- 配置缺失自动回退默认值
- `config-first` 数据源原则

---

## 🚀 启动时序

```
访问 SingleMono.Instance
  ↓
创建 GameObject + AddComponent<SingleMono>
  ↓
GlobalConter.Initialize()
  ├─ 事件泵就绪
  ├─ 程序集扫描（行为 + 配置）
  ├─ FactoryCenter 启动自动实例化行为
  ├─ 加载主 UI
  └─ GameStateMachine 初始化
  ↓
广播 AwakeEvent
  ↓
每帧 Update/FixedUpdate → EventCenter 广播 → 行为回调
```

---

## 📁 框架目录

```
Assets/script/Center/
├── Bootstrap/          启动引导
│   ├── SingleMono.cs          单例 Mono 基类
│   ├── GlobalConter.cs        全局引导入口
│   └── AssemblyScanner.cs     程序集扫描器
├── Behavior/           行为基类
│   ├── Objectbehavior.cs      实体行为基类
│   └── UIBehavior.cs          UI 行为基类
├── Event/              事件系统
│   └── EventCenter.cs         全局事件中心
├── Factory/            工厂系统
│   ├── BehaviourFactory.cs     行为工厂
│   ├── BehaviourTypeRegistry.cs 类型注册表
│   └── BehaviourInstanceHub.cs 实例 Hub
├── Config/             配置系统
│   ├── ConfigCenter.cs         配置中心
│   └── BasicDate.cs           数据基类
├── Resource/           资源系统
│   ├── ResCenter.cs           资源中心
│   └── ResModule/             资源模块实现
├── CollisionModule/    碰撞桥接
│   ├── TriggerBridge2D.cs      2D 触发桥
│   └── CollisionBridge2D.cs    2D 碰撞桥
├── Global/             全局状态
│   ├── GameStateMachine.cs     游戏状态机
│   └── Global.cs               全局静态数据
├── Tool/               工具
│   └── ConfigTableGenerator.cs 配置表生成器
└── Utils/               工具类
    ├── BasicSingle.cs          纯 C# 单例基类
    ├── AutoRegisterAttribute.cs 自动注册特性
    └── JsonDataUtil.cs         JSON 工具
```

---

## 📝 扩展指南：新增一个行为

### 1. 编写数据类
```csharp
[AutoRegisterConfig("Player")]
[Serializable]
public class Player : BasicDate
{
    public int maxHealth = 3;
    public int currentHealth = 3;
    public void ApplyDamage(int dmg) => currentHealth -= dmg;
}
```

### 2. 编写行为类
```csharp
[AutoRegisterBehaviour("Player")]
public class PlayerBehavior : Objectbehavior
{
    public PlayerBehavior() { isBuild = true; }  // 启动时自动实例化
    
    public override void Init(GameObject obj, BasicDate data) { /* 绑定数据 */ }
    public override void LogicAwake() { /* 订阅事件 */ }
    public override void LogicDestroy() { /* 退订事件 */ }
}
```

### 3. 配置文件（可选）
`Assets/Resources/Config/player.json`，字段名与序列化字段一致。

### 4. 预制体（实体行为必需）
`Assets/Resources/Entity/Player.prefab`

---

## 🎮 示例游戏：FlyFox

本框架内置示例游戏 **FlyFox**（横版飞行射击），打包在 `BUILD/` 文件夹内。

**游戏特点：**
- 玩家移动 + Dash 冲刺 + 受伤动画
- 4 种敌人差异化 AI（甲虫/曲线鹰/秃鹫/斜鹰）
- 物品收集（金币/宝石/樱桃回血）
- 完整 UI 流程（主菜单/HUD/暂停/结算/成绩记录）
- 难度随时间递增（速度加快 + 密度提升）

> 游戏仅为框架演示，主体是框架本身。

---

## 📚 文档

- [开发教程.md](./开发教程.md) — 快速上手
- [Center/Framework.md](./Center/Framework.md) — 框架详细文档
