# FlyFox - CenterFramework

> A Unity Custom Game Framework | 自研Unity游戏框架
> 基于 Center 框架开发的横版飞行射击游戏

---

## 🎮 游戏介绍

**FlyFox** 是一款横版飞行射击游戏，基于自研 Center 框架开发。

### 游戏玩法
- 玩家控制小飞机在屏幕内移动，躲避敌人碰撞
- 收集金币、宝石、樱桃（回血）得分
- 敌人有多种类型，从不同方向来袭
- 被敌人碰到扣血，3 滴血用完游戏结束
- 按 ESC 暂停游戏

### 游戏状态
```
主菜单 → Play → 游戏中 → GameOver
              ↘ ESC → 暂停菜单 → 继续/退出
```

---

## 📁 目录结构

```
Assets/script/
├── Action/                          游戏行为 + 逻辑
│   ├── CharacterBehavior/           角色行为：
│   │                                  PlayerBehavior（玩家移动+碰撞+动画）
│   │                                  EnemyShipBehavior（4种敌人差异化移动）
│   │                                  ItemBehavior（物品拾取）
│   │                                  BehaviourRegistry（静态行为注册表）
│   ├── GameBehavior/               游戏行为：
│   │                                  LevelBuildBehavior（物品关卡构建）
│   │                                  EnemyBuildBehavior（敌人生成器）
│   │                                  InputController（输入控制）
│   │                                  BackgroundParallaxBehavior（背景视差）
│   ├── UIBehavior/                  UI 行为：
│   │                                  UIManagerBehavior（主菜单）
│   │                                  GamingUI（游戏内HUD：血量/Dash/分数）
│   │                                  GameStopUi（暂停菜单）
│   │                                  GameOverUi（结算界面）
│   │                                  RecordBehavior（成绩记录）
│   │                                  ScoreRollingBehavior（分数滚动）
│   └── Archive/                    归档死代码
├── Center/                          框架基础（冻结：只修 bug，不新增）
│   ├── Bootstrap/                   SingleMono / GlobalConter
│   ├── Behavior/                    Objectbehavior / UIBehavior
│   ├── Event/                       EventCenter（事件中心）
│   ├── Factory/                     BehaviourFactory / FactoryCenter
│   ├── Config/                      ConfigCenter / BasicDate
│   ├── Resource/                    ResCenter
│   ├── CollisionModule/             TriggerBridge2D / CollisionBridge2D
│   ├── Global/                      GameStateMachine / Global
│   └── Tool/                        ConfigTableGenerator
├── Data/                            纯数据类：
│                                      Player / Score / ScoreRecord
│                                      EnemyShipData / SpawnPositionData
└── Enum/                            GameEnum / GridType
```

---

## 🎯 游戏功能

### 玩家系统
- **移动**：WASD / 方向键移动，相机边界限制
- **Dash 冲刺**：Shift 键冲刺，3 次上限，自动恢复
- **血量**：3 滴血，被敌人碰撞扣 1 血，1 秒无敌
- **受伤动画**：Animator hurt 参数触发受伤动画

### 敌人系统
| 敌人 | 移动方式 | 生成位置 |
|---|---|---|
| 甲虫 bettle1 | 水平左移 | 右侧 |
| 曲线鹰 eagle1 | Sin 曲线摆动 | 右侧 |
| 秃鹫 vulture1 | 垂直上下飞 | 上方/下方 |
| 斜鹰 eagle2 | 45° 斜向左下 | 右上方 |

### 难度递增
- 移动速度随时间加快（5f → 10f 上限）
- 生成间隔随时间缩短（3s → 1s 下限）
- 屏幕内敌人上限 6 个

### UI 系统
- **主菜单**：Play / Record 按钮
- **游戏 HUD**：血量图标、Dash 图标、实时分数
- **暂停菜单**：ESC 呼出，继续/退出
- **结算界面**：显示本局分数、存活时间，保存到 scores.json
- **成绩记录**：按分数排序显示历史成绩

---

## 📦 打包信息

### BUILD 文件夹
```
BUILD/
└── Windows/
    └── FlyFox.exe  可执行文件
```

### 运行要求
- Windows 系统
- 无需安装，解压即可运行

---

## 🚀 快速上手

### 开发环境
- Unity 2022.3 LTS
- C#

### 运行游戏
1. 打开 Unity Hub，加载项目
2. 等待资源导入完成
3. 点击 Play 运行

### 操作说明
| 按键 | 功能 |
|---|---|
| WASD / 方向键 | 移动 |
| Shift | Dash 冲刺 |
| ESC | 暂停 / 返回 |
| 鼠标点击 | UI 按钮 |

---

## 📝 框架文档

- [开发教程.md](./开发教程.md)
- [Center/Framework.md](./Center/Framework.md)
