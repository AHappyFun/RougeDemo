# Rouge Demo — Claude 项目规则

## 项目概述

吸血鬼幸存者风格的修仙/rogue-like 塔防游戏。玩家固定在屏幕中心，敌人从屏幕边缘向中心移动，多种自动发射子弹类型。

## 技术栈

- Unity 2022.3 LTS + URP
- 3D 渲染 + 2D 正交相机 (orthographicSize=8)
- 纯代码构建（GameBootstrap 模式） + Prefab 资源（位于 Resources/）
- 命名空间：`namespace Rouge { ... }`

## 架构规则

### 代码构建
- 所有 GameObject 在 `GameBootstrap.Awake()` 中按序构建：
  1. Camera → Player → BulletManager → EnemySpawner → GameManager → Canvas(UI) → WaveManager
- 无 MonoBehaviour 拖拽引用，全部通过代码 `public field = value` 注入
- 使用 `GameObject.FindGameObjectWithTag("Player"/"Enemy")` 查找运行时对象

### Prefab 系统（Resources/）
- 通过 Unity MCP 工具在 Editor 中直接创建/编辑 prefab 和 material
- 加载：`MeshGenerator` 内部通过 `Resources.Load<T>()` 加载，**调用方代码无需感知**
- 结构：
  ```
  Resources/
  ├── Prefabs/
  │   ├── Player.prefab
  │   ├── Enemies/BasicEnemy.prefab
  │   ├── Bullets/SwordBullet.prefab
  │   ├── Bullets/StraightBullet.prefab  (每类型独立颜色)
  │   ├── Bullets/OrbitalBullet.prefab
  │   ├── Bullets/RicochetBullet.prefab
  │   ├── Bullets/ShotgunBullet.prefab
  │   ├── Bullets/ChainBullet.prefab
  │   └── VFX/{HitParticles,DeathParticles}.prefab
  └── Materials/
      ├── Player.mat / EnemyDefault.mat
      ├── Bullets/{Straight,Orbital,Ricochet,Shotgun,Chain,SwordBullet}.mat
      ├── {HealthBarBG,HealthBarFill}.mat
      └── VFX/ParticleDefault.mat
  ```
  ```
- Prefab 不含运行时脚本（BaseBullet / BaseEnemy / EnemyHealth 等），脚本在 spawn 时由代码添加
- VFX prefab 使用 URP Particles/Unlit 材质，Entities 使用 URP Lit
- 颜色在实例化后由代码设置（prefab 本身用默认颜色）

### 暂停系统
- 全局暂停：`GameManager.IsPaused` (static bool)
- 所有 `Update()` 入口必须在最开头检查 `if (GameManager.IsPaused) return;`
- 注意：子类 `override Update()` 中，`base.Update()` 的 early return 不能阻止子类后续代码执行，必须子类自己也加检查

### 配置系统（4 个独立 Config）
- `GameConfig` — 游戏整体控制（玩家血量、敌人生成、相机参数）
- `BulletConfig` — 子弹/技能属性（5 种子弹配置、专属参数）
- `WaveConfig` — 波次控制（触发条件、敌人缩放曲线）
- `UpgradeConfig` — 增益列表（所有 UpgradeDef + UpgradeType 定义）
- 配置存放在 `Res/` 目录下
- 配置从 ScriptableObject 读取后缓存为字段，运行时直接访问字段（不每帧读配置）

### 子弹系统
- 5 种子弹类型：Straight / Orbital / Ricochet / Shotgun / Chain
- 分类：`BulletCategory` 枚举区分 **Attack（常驻普攻）** 和 **Skill（技能）**
  - Straight → Attack（受攻速影响）
  - Orbital / Ricochet / Shotgun / Chain → Skill（受冷却缩减影响）
- 所有类型同时开火，用 `HashSet<BulletType> activeTypes` 管理
- 按键 1-5 独立 toggle（ON/OFF）
- `BulletManager` 管理全部子弹发射、冷却、增益
  - `cooldownMult` → 影响 Attack 类型的攻速间隔
  - `skillCooldownMult` → 影响 Skill 类型的冷却时间
- 子弹继承结构：`BaseBullet` → `StraightBullet / OrbitalBullet / RicochetBullet / ChainBullet`
- 子类 `override Update()` 必须手动检查 `IsPaused`
- Orbital 环绕特殊处理：由 BulletManager.UpdateOrbital() 驱动，Create/DestroyOrbital() 管理生命周期
- 子弹外观：从 `Prefabs/Bullets/SwordBullet.prefab` 实例化，运行时设置颜色
- 命中 VFX：`MeshGenerator.SpawnHitParticles()`，使用 `Prefabs/VFX/HitParticles.prefab`
- cooldownBar UI 使用 `Image.FillMethod.Horizontal`

### 敌人系统
- 屏幕边缘随机生成（`Camera.RandomPointOnScreenEdge()`）
- 追踪玩家（`transform.position += dir * speed * Time.deltaTime`）
- 碰撞伤害：`OnTriggerStay` + 1s CD
- HP 归零时：`MeshGenerator.SpawnDeathParticles()` → `GameManager.AddKill()` → `Destroy()`
- 血条以子物体形式包含在 `BasicEnemy.prefab` 中

### 波次 & 增益系统
- 每击杀 KillsNeeded 只敌人 → 触发"突破"升级
- 触发时：`isUpgrading = true` + `GameManager.IsPaused = true` → 弹出 3 选 1 面板
- 选择后 → `ApplyWaveScaling()` (敌人缩放) + `BulletManager.ApplyUpgrades()` (全局增益) → 恢复暂停
- 全局增益：累积 multiplier → `AdvanceWave()` 时通过 `ApplyUpgrades()` 一次性推给 BulletManager
- 单类型增益：即时调用 `BulletManager.AddXxxBonus()` 方法
- `UpgradeDef.targetBulletType` = null 表示全局，非 null 表示单类型

### UI
- 所有 UI 在 `GameBootstrap.CreateUI()` 中用代码创建
- 使用 `Text` + `Image` (uGUI)，`Font.CreateDynamicFontFromOSFont("Arial", size)`
- GameOver 面板不销毁，默认隐藏（SetActive(false)）
- 增益面板不销毁，每次刷新按钮文本和回调

## 代码规范

### 命名
- 类名：PascalCase
- 字段/方法：camelCase（private/protected），PascalCase（public/static/属性）
- 枚举：PascalCase
- 文件：与类名一致

### 文件组织
```
Assets/Rouge/
├── Scripts/
│   ├── Core/      — 入口、管理器、配置（10 个 .cs）
│   ├── Bullet/    — 子弹类（5 个 .cs）
│   ├── Enemy/     — 敌人相关（2 个 .cs）
│   ├── Editor/    — 编辑器工具
│   └── Utils/     — 工具、扩展（3 个 .cs）
├── Res/
│   ├── Resources/ — 运行时加载的 Prefab 和 Material
│   ├── GameConfig.asset      — 整体控制
│   ├── BulletConfig.asset    — 子弹属性
│   ├── WaveConfig.asset      — 波次控制
│   └── UpgradeConfig.asset   — 增益列表
├── Scene/         — Unity 场景文件
└── Resources/     — Unity Resources 目录（如有）
```

### C# 注意事项
- 用 `System.Text.StringBuilder` 拼接字符串，避免 `+` 在循环中
- `Time.deltaTime` 用于暂停敏感的移动逻辑；`Time.unscaledDeltaTime` 用于 FPS 等不受暂停影响的逻辑
- `Mathf.RoundToInt()` 用于浮点数取整
- 子弹冷却用 `Time.time - lastFireTime >= cooldown` 模式
- 伤害用 `int`，百分比用 `float`
- 防御性调用：`bulletManager?.AddXxx()`, `GameManager.Instance?.TriggerGameOver()`
- 不要修改.gitignore 除非确认需求

### 避免的常见错误
- **`base.Update()` 的陷阱**：子类中调用 `base.Update()` 后必须自行检查 `IsPaused`，基类的 `return` 不会阻止子类代码
- **Orbital 数组泄漏**：`ApplyUpgrades()` 中先 `DestroyOrbital()` 再 `ApplyConfig()` 最后 `CreateOrbital()`，否则 orbitalBullets 数组会孤儿化
- **Scene 文件**：改场景会导致 .unity 元数据变更，仅当用户明确要求时修改
- **Prefab Scale 叠加**：prefab 自身 scale 和 `MeshGenerator.CreateXxx` 的 scale 参数不能重复设置——prefab 应为单位缩放 (1,1,1)
- **🔴 禁止硬编码地图/场景相关数值**：`bounds`、`MaxDistance`、子弹销毁距离等与地图尺寸耦合的值，必须从场景数据、GameConfig 或运行时计算获取。写死常量会导致地图放大/修改时功能静默失效，排查困难。

## 配置默认值参考

### GameConfig 默认值
| 子弹 | 冷却 | 伤害 | 速度 | 数量 | 颜色 |
|---|---|---|---|---|---|
| Straight | 0.3s | 10 | 10 | 1 | Yellow |
| Orbital | 0s | 10 | 360°/s | 3 | Orange |
| Ricochet | 0.5s | 8 | 8 | 1 | Green |
| Shotgun | 0.8s | 6×5 | 10 | 5 | Magenta |
| Chain | 0.6s | 12 | 20 | 1 | Cyan |

### 增益池（10 种）
- 全局：伤害+30%、冷却-20%、子弹+1、回血30%、弹速+25%
- 单类型：Ricochet反弹+2、Orbital转速+40%、Chain跳数+1、Chain范围+30%、Shotgun角度-20%

### 波次参数
- 默认每 100 击杀触发一次，每波额外 +20 需求
- 敌人 HP 每波 +50%、速度 +15%、生成间隔 *(1-0.15)^(w-1)、最大数 +5、伤害 +3
