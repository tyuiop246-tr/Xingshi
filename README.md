# 醒狮：高桩采青

基于 Unity 2022.3.62f3c1 开发的 3D 平台跳跃白盒 Demo。题材取自广东非遗醒狮的「梅花桩」与「采青」，玩家操控醒狮从平地出发，连续跨越梅花桩与移动鼓阵，登顶高台摘下「青」。

> 本项目面向「关卡策划 / 游戏策划」岗位设计：在可玩的 Demo 之外，提供起承转合的关卡动线、难度曲线、配置表、俯视图与一键生成工具，完整资料见下方「相关资料」。

## 策划概览

| 项目 | 内容 |
|---|---|
| 题材 | 广东非遗醒狮：梅花桩 + 采青 |
| 类型 | 3D 平台跳跃（Platformer） |
| 核心目标 | 精准落点、跳跃距离把控、动态障碍时机判断 |
| 关卡结构 | 起（教学）→ 承（静态桩）→ 转（移动鼓）→ 合（采青高台） |
| 当前内容 | 3 个可选关卡；关卡 4 在关卡选择页显示「开发中」 |
| 实现状态 | 白盒可玩，含封面、关卡选择、HUD、复活点、暂停/设置界面 |

**设计原则**

- 题材与玩法咬合：梅花桩天然提供高低错落与落点约束，不是单纯贴皮。
- 难度沿 +Z 轴平滑上升，通过高度、桩径、横向偏移、机关速度四个维度加压。
- 白盒优先：用基础几何体 + 程序化材质验证空间节奏，不依赖外部美术资源。
- 工具化落地：关卡由 Editor 脚本一键生成，数值改动后可直接重建整关。

## 关卡设计

### 单关动线

| 区段 | 距离 | 内容与目的 |
|---|---|---|
| 起 | 0–16 m | 宽阔广场 + 两级低台阶，熟悉移动与跳跃；远处锚点柱建立目标感 |
| 承 | 17–53 m | 14 根加密梅花桩，间距约 2.7 m，顶面 1.0→3.6 m，直径 2.4→1.9；练习连续跳跃 |
| 转 | 60–80 m | 3 个横向往返的移动鼓 + 3 个固定喘息台，速度 2.0→3.0，相位错开；节奏高潮 |
| 合 | 90–92 m | 高台 + 发光旋转的「青」+ 聚光灯，强正反馈释放；保留高风险隐藏捷径 |

### 三个可选关卡

关卡 1 为完整教学版；关卡 2 / 3 通过 `LevelRuntimeTuner.cs` 在同一场景上做运行时难度微调，切换关卡时会先还原初始参数，避免难度叠加。

| 关卡 | 名称 | 桩面缩放 | 横向偏移 | 机关倍率 | 体验目标 |
|---|---|---|---|---|---|
| 1 | 入门 | 1.00 | 1.00 | 1.00 | 熟悉手感，容错最高 |
| 2 | 进阶 | 0.86 | 1.25 | 1.20 | 桩更窄、跨度更大、鼓更快 |
| 3 | 挑战 | 0.72 | 1.55 | 1.45 | 考验精准落点与机关时机 |

### 复活点系统

- 全程 3 个复活点，位于教学区末端、桩段末端与鼓阵末端。
- 走近后触发浅蓝光圈与半透明光柱，激活后材质变亮并提示。
- 死亡后从最近已激活复活点重生；未激活任何复活点则回到起点。
- 切换关卡或按 `R` 重开时，复活点与终点采集状态统一重置。

## 操作说明

| 按键 / 操作 | 功能 |
|---|---|
| 封面按 `Space` / `Enter` | 进入关卡选择 |
| 关卡选择点击按钮，或按 `1 / 2 / 3` | 开始对应关卡 |
| `WASD` / 方向键 | 移动 |
| `Space` | 跳跃 |
| `R` | 重新开始当前关卡 |
| `ESC` | 打开 / 关闭设置界面 |
| 设置界面「退出关卡」 | 返回关卡选择界面 |

关卡选择页参考《蛋仔派对》的蓝底星级按钮样式；关卡 4 按钮置灰并显示「开发中」。

## 系统实现

| 能力点 | 实现 | 代码 / 资源 |
|---|---|---|
| 关卡一键生成 | Editor 脚本创建场景、材质、Prefab、灯光、UI 与角色 | `Assets/Editor/WhiteboxBuilder.cs` |
| Prefab 复用 | 梅花桩与移动鼓各保存为 Prefab，实例只改 Transform / 参数 | `MeihuaPile.prefab`、`MovingDrum.prefab` |
| 角色手感 | `CharacterController` + Coyote Time + Jump Buffer；地面直接设速、空中插值，解决圆柱顶面打滑 | `PlayerController3D.cs` |
| 转身表现 | 根据角色朝向与相机方向切换正面 / 侧面 / 背面像素贴图，类似蛋仔派对的转身反馈 | `BillboardSprite.cs` |
| 移动机关 | `Mathf.PingPong(Time.time * speed + phase, 2 * amp) - amp` | `MovingObstacle.cs` |
| 复活点 | Trigger 检测、激活状态切换、最近复活点记录、死亡重生 | `Checkpoint.cs`、`PlayerController3D.cs` |
| 关卡难度 | 基于初始参数还原并重新应用倍率，避免多关卡切换时参数漂移 | `LevelRuntimeTuner.cs` |
| UI | IMGUI / OnGUI：封面、关卡选择、HUD、复活提示、通关面板、设置面板 | `TitleScreen.cs`、`HUDController.cs` |
| 视觉 | 程序化网格材质、发光「青」、醒狮三面像素图、半透明复活点光柱、醒狮边框 | `Assets/Resources/`、`Assets/Whitebox/Materials/` |

## 相关资料

- [完整关卡策划初稿](LEVEL_DESIGN.md)：核心概念、起承转合、配置表、面试讲解要点
- [难度曲线图](DifficultyCurve.svg)：X 轴为关卡距离，Y 轴为难度
- [俯视图](TopDown.svg)：主线动线、难点位置与隐藏捷径
- 初版文档保留早期数值；当前实现以 `WhiteboxBuilder.cs` 与 `LevelRuntimeTuner.cs` 为准。

## 工程结构

```text
Assets/
├── Editor/
│   └── WhiteboxBuilder.cs        # 一键生成整关
├── Resources/
│   ├── GameFrame.png             # 封面 / HUD 醒狮边框
│   ├── LionPixel.png             # 醒狮正面
│   ├── LionPixelSide.png         # 醒狮侧面
│   └── LionPixelBack.png         # 醒狮背面
├── Scenes/
│   └── SampleScene.unity         # 游戏场景
├── Scripts/
│   ├── TitleScreen.cs            # 封面 + 关卡选择
│   ├── HUDController.cs          # HUD + 设置面板
│   ├── LevelRuntimeTuner.cs      # 关卡难度微调
│   ├── PlayerController3D.cs     # 移动 / 跳跃 / 重生
│   ├── BillboardSprite.cs        # 三面朝向切换
│   ├── Checkpoint.cs             # 复活点
│   ├── MovingObstacle.cs         # 移动鼓
│   ├── GoalQing.cs               # 终点采青
│   └── FollowCamera.cs           # 跟随相机
└── Whitebox/
    ├── Materials/                # 程序化材质与复活点发光材质
    └── Prefabs/                  # 梅花桩 / 移动鼓 Prefab

LEVEL_DESIGN.md                   # 关卡设计初稿
DifficultyCurve.svg               # 难度曲线
TopDown.svg                       # 俯视图
```

## 运行与验收

1. 使用 Unity 2022.3.62f3c1 打开本项目。
2. 打开 `Assets/Scenes/SampleScene.unity`。
3. 按 Play，封面按 `Space` 进入关卡选择。
4. 选择关卡 1 / 2 / 3，用 `WASD` 移动、`Space` 跳跃，登顶触碰「青」。
5. 验证复活点：走近激活后跌落，应从最近复活点重生。
6. 验证设置：`ESC` 打开面板，可继续、重开或退出关卡返回关卡选择。
7. 如需重建关卡：菜单 `Tools > 醒狮白盒 > 生成关卡（一键重建）`。

## 后续规划

- 关卡 4 与更多主题关卡（夜战、雨战、舞狮竞速）。
- 将关卡配置外置到 ScriptableObject / CSV，方便策划独立调参。
- WebGL 构建与在线试玩页面。
- 补充音效、粒子反馈与更完整的醒狮美术资源。
