# Rouge Demo — 需求文档汇总

**最后更新：2026-06-10**  
**状态：全部已实现**

---

## Round 1 — 核心游戏框架

### 项目概述
吸血鬼幸存者风格的修仙肉鸽塔防游戏。玩家固定在屏幕中心，敌人从屏幕边缘向中心移动，多种自动发射子弹类型。

**技术栈：Unity 2022.3 LTS + URP**

### 玩家
- 固定在屏幕中心，不移动，视觉可转向

### 子弹类型（5种，全部自动发射，独立CD）
| 类型 | 冷却 | 伤害 | 速度 | 特性 |
|---|---|---|---|---|
| Straight 直射 | 0.3s | 10 | 10 | 自动向最近敌人直线发射 |
| Orbital 环绕 | 无CD | 10 | 360°/s | 3颗环绕旋转(半径4)，每敌人每秒受伤1次 |
| Ricochet 弹射 | 0.5s | 8 | 8 | 屏幕边缘反弹，最大5次 |
| Shotgun 散射 | 0.8s | 6×5 | 10 | 扇形60°扩散5颗 |
| Chain 链式 | 0.6s | 12 | 20 | 连锁弹射3次，范围5单位 |

### 敌人系统
- 屏幕四周随机生成 → 向中心移动(速度2)
- HP 30 / SphereCollider r=0.5 / 红色
- 最大15只 / 生成间隔1s
- 子弹命中减HP，归零销毁

### 输入控制
- 武器开关通过 PlayerStats Inspector 控制，运行时也可修改

### UI
- 敌人数+击杀数 / 冷却进度条 / FPS / 敌人3D血条 / 玩家血条

---

## Round 2 — 3D渲染 + 可配置化

1. 视角保持2D正交但用 **3D 渲染**，为修仙风格画面打基础
2. 子弹增加可配置项：攻击速度、攻击数量等 → `GameConfig` ScriptableObject
3. 敌人增加可配置项：生成数量、生成速度等

---

## Round 3 — 玩家受伤 + 飞剑外观 + 粒子特效

1. 主角受伤害，HP=0 时 **GameOver 弹窗**（显示击杀数，按 R 重开）
2. 修复链式子**弹迅速切换目标**的 Bug → 改为状态机 (Traveling→Cooldown)
3. 受击/击中改为 **URP 粒子特效**（Particles/Unlit）
4. 子弹改为**修仙飞剑风格**：剑身 Cube + 剑格 Cube + TrailRenderer

---

## Round 4 — 波次/升级系统

每击败100个敌人触发"突破"：
- 弹出 **3 个增益选项**强化玩家
- 选择后进入下一波，**敌人属性递增**
- 配置项分拆到 `WaveConfig` ScriptableObject

---

## Round 5 — 多子弹同时发射

所有子弹类型可**同时拥有并独立开火**，通过 PlayerStats 开关控制。

---

## Round 6 — Bug 修复 + 增益完善

1. **选增益时游戏暂停**：全局 `GameManager.IsPaused`，冻结所有 Update()
2. **增益选项不变化**：`isUpgrading` 防护，选增益期间不会重复触发
3. **单类型增益**：新增 5 种只增益特定子弹的选项（弹射次数、旋转速度、链式跳数/范围、散射角度）
4. **UI 标注开关状态**：`1:Straight[ON] 2:Orbital[ON] ...` 实时显示

---

## Round 7 — Prefab 系统 + 自定义 Shader

### Prefab 资源化
- `ResGenerator.cs` Editor 工具，菜单 "Rouge → Generate Assets" / `[InitializeOnLoad]` 自动运行
- 一键生成全部 Prefab 和 Material 到 `Resources/` 目录
- 每种子弹有独立 Prefab 文件（5 段飞剑模型：Blade/Tip/Guard/Handle/Pommel）
- `Res/` → `Resources/` 重命名

### 自定义 URP HLSL Shader
- **Rouge/Character** — Lit，主光源 + SH 环境光 + Blinn-Phong，用于角色/敌人/飞剑
- **Rouge/VFX** — 透明 Unlit，`[HDR]` 颜色，用于粒子/Trail/血条

### 架构
- `MeshGenerator` 拆分 `FromPrefab` / `Procedural` 独立方法，职责分离
- `GameBootstrap` 自动 `Resources.Load` 配置，Inspector 丢失不影响
- `ApplyUpgrades()` 重置 `lastFireTimes`，防止升级后瞬间齐射
- Orbital 半径移入 `BulletTypeConfig.radius`，默认 1.5→4.0

---

## Round 8 — 3D 场景 + WASD 移动 + 俯视角相机

### 白盒场景
- `SceneBuilder.cs` 程序化生成：50×50 地板 + 4 面边界墙 + 12 个障碍物
- **ResSceneGenerator.cs**（菜单"Rouge → Generate Scene"）一键生成 .unity 场景文件
- Y轴向上，实体高度 y=0.5

### 玩家移动
- WASD 控制，XZ 平面移动（W=+Z / S=-Z / A=-X / D=+X）
- `PlayerMovement.cs`，暂停时 `IsPaused` 检查

### 摄像机
- 透视俯角 70°，固定朝向世界 +Z，仅位置跟随
- 高度 18，Z 偏移补偿画面中心偏移
- `CameraFollow.cs`，平滑 Lerp

### 子弹系统 3D 适配
- 所有子弹旋转 XY→XZ：`Atan2(x,z)`，`Euler(0,angle,0)`
- 飞剑 5 段模型（Blade/Tip/Guard/Handle/Pommel），碰撞体 `(0.3,0.3,1.0)`
- Shotgun `Euler(0,angle,0)`，Orbital `(Cos,0,Sin)`
- Ricochet 视口 Y→Z，随机方向 XZ
- 敌人生成 `ViewportPointToRay` + 地面射线
- 闪电链 0.1s 闪现，URP Shader，抬高至 y=0.2

---

## 当前最终状态速览

### 游戏模式
- 3D 场景 + 透视俯角相机（非正交）
- WASD 自由移动，摄像机平滑跟随
- 场景可预生成 .unity 文件，支持 Editor 可视化编辑

### 玩家 & 敌人
- 均使用 MeshRenderer + 球体（待美术替换模型）
- 敌人使用 NavMeshAgent 寻路，绕开障碍物（回退直线移动）
- 敌人无 Rigidbody，碰撞检测用 SphereCollider isTrigger

### 子弹
- 5 种同时开火，1-5 toggle
- 5 段飞剑模型（Blade/Tip/Guard/Handle/Pommel）
- 全部适配 Y-up 3D，XZ 平面飞行

### 增益池（10 种）
| 类型 | 名称 | 效果 |
|---|---|---|
| 全局 | 剑气强化 | 伤害 +30% |
| 全局 | 御剑如风 | 冷却 -20% |
| 全局 | 万剑归宗 | 子弹数 +1 |
| 全局 | 灵气复苏 | 回复 30% HP |
| 全局 | 流星赶月 | 弹速 +25% |
| 单类型 | 弹射强化 | Ricochet 反弹 +2 |
| 单类型 | 旋转加速 | Orbital 转速 +40% |
| 单类型 | 连锁增强 | Chain 跳数 +1 |
| 单类型 | 链式延伸 | Chain 范围 +30% |
| 单类型 | 散射聚焦 | Shotgun 角度 -20% |

### 暂停系统
`GameManager.IsPaused`，所有 Update() 入口检查。子类 override 必须自行检查。

### 配置
- `GameConfig` — 整体控制（玩家血量、敌人生成、相机参数）
- `BulletConfig` — 子弹/技能属性（含 Category/cooldown/damage/speed/专属参数）
- `WaveConfig` — 波次触发、敌人缩放曲线
- `UpgradeConfig` — 增益列表 + UpgradeType 定义

### 资源目录
```
Resources/
├── Prefabs/
│   ├── Player.prefab
│   ├── Enemies/BasicEnemy.prefab
│   ├── Bullets/{Straight,Orbital,Ricochet,Shotgun,Chain,Sword}Bullet.prefab
│   └── VFX/{HitParticles,DeathParticles}.prefab
├── Materials/ (11个材质，按类型分目录)
├── GameConfig.asset       — 整体控制
├── BulletConfig.asset     — 子弹属性
├── WaveConfig.asset       — 波次控制
└── UpgradeConfig.asset    — 增益列表
```

### 文件清单
- **30+ 个 .cs**：Core(10) + Editor(2) + Bullet(5) + Enemy(2) + Utils(4) + SceneBuilder/PlayerMovement/CameraFollow(3)
- **2 个 .shader**：RougeCharacter（含受击闪白） + RougeVFX
- **4 个 .asset**：GameConfig + BulletConfig + WaveConfig + UpgradeConfig

---

## Round 9 — 2026-06-10 大重构：Config拆分 + 技能系统 + 数据流

### 配置系统重构
- 原 `GameConfig` + `WaveConfig` 拆分为 **4 个独立 Config**
- `GameConfig` — 整体控制（玩家血量、敌人生成、相机）
- `BulletConfig` — 子弹/技能属性，含 `BulletCategory` 枚举
- `WaveConfig` — 波次触发条件 + 敌人缩放
- `UpgradeConfig` — 增益池 + UpgradeDef + UpgradeType 定义
- 所有 Config 有完整中文 Tooltip

### Attack/Skill 分类
- 新增 `BulletCategory` 枚举：`Attack`（常驻普攻）/ `Skill`（技能冷却）
- Straight → Attack，其余 4 种 → Skill
- 攻速倍率 `cooldownMult` 只影响 Attack
- 冷却缩减 `skillCooldownMult` 只影响 Skill

### Orbital 技能化
- 持续时长 `duration`（秒）→ 冷却 `cooldown`（秒）→ 自动重新激活
- 大剑视觉：缩放 1.2，剑身指向径向（侧面砍）
- 修复 OrbitalBullet 碰怪自毁 bug（误调 OnHitEnemy → Destroy）

### Ricochet 重做
- 穿透敌人（不反弹），只在窗口边界反弹
- 缩放 1.0，弹射次数耗尽销毁
- 普攻 Straight 命中敌人销毁（不穿透）

### 数据流重构（Buff 系统）
```
Config → PlayerStats（基础值，永不修改）
           ↓
   + buff 倍率（WaveManager 累积）
           ↓
   BulletManager 运行时值（实际游戏逻辑）
           ↓
   PlayerStats.runtimeXxx（Inspector 只读展示）
```
- 修复升级后冷却/数量叠加 bug（3→4→6）
- 修复 Heal 多除以 100 的 bug
- 新增 `SkillDebugPanel` 运行时调试面板（下拉查看各子弹最终属性）

### 受击闪白
- RougeCharacter.shader 新增 `_HitColor` / `_HitAmount` 属性
- EnemyHealth 闪白协程，0.12s 白色闪烁后恢复

### 删除冗余
- 删除 `ResGenerator.cs` / `ResSceneGenerator.cs` / `AutoDestroy.cs`
- 删除 VFX Prefab 中的 Missing Script 引用
- VFX 改为 `stopAction=Destroy`，无需 AutoDestroy 脚本

### UI
- HUD 间距优化（HP/CD 条重叠问题）
- PlayerStats Inspector 现含所有子弹完整属性
- 增益池 10 → 12 种：新增"疾风剑意"(攻速+25%) / "玄门心法"(技能冷却-15%)

### 修复清单
| Bug | 原因 | 修复 |
|-----|------|------|
| SyncFromPlayerStats 每帧覆盖冷却 | 放在 Update() 里全量同步 | 改为仅同步开关，冷却通过 ApplyUpgrades 管理 |
| 子弹数量 buff 叠加 (3→4→6) | SyncRuntimeToPlayerStats 写回基础字段 | 改为写入 runtimeXxx 字段 |
| Orbital 刚出来就消失 | SyncFromPlayerStats 中 SetActive 触发 CreateOrbital 时 orbitalCount=0 | 先设 orbitalCount 再 SyncToggles |
| Orbital 碰怪消失 | OrbitalBullet.OnTriggerEnter 调用 OnHitEnemy→Destroy | 移除 OnHitEnemy 调用 |
| 灵气复苏回血无效 | Heal() 里多除了 100 | 移除 /100f |
| 受击 VFX Missing Script | AutoDestroy.cs 已删但 Prefab 仍有引用 | 从 Prefab 移除了组件引用 |
