# Rouge Demo — 代码架构图 & 运行流程图

**生成日期：2026-05-10**

---

## 一、代码架构图（类关系 & 依赖）

```
┌────────────────────────────────────────────────────────────────────────────┐
│                            GameBootstrap                                   │
│                         (入口 MonoBehaviour)                                │
│  Awake() 按序构建全部 GameObject:                                           │
│    Camera → Player → BulletManager → EnemySpawner → GameManager            │
│    → Canvas(UI) → WaveManager                                              │
└───────┬───────────────────────────────────────────────────────────────────┘
        │ 创建 & 注入依赖
        ▼
┌───────────────────────────────────────────────────────────────────────────┐
│                                                                           │
│  ┌──────────────┐    ┌──────────────────┐    ┌──────────────────┐         │
│  │ PlayerHealth │    │  BulletManager   │    │  EnemySpawner    │         │
│  │              │    │  (单例)           │    │                  │         │
│  │ maxHP=100    │◄───│  activeTypes<>    │    │ maxEnemies       │         │
│  │ Heal(%)      │    │  cooldowns[]      │    │ spawnInterval    │         │
│  │ TakeDamage() │    │  damageMult       │    │ enemyHP/speed    │         │
│  │ HPPercent    │    │  cooldownMult     │    │ ApplyWaveScaling │         │
│  │ IsDead       │    │  countBonus       │    │ BuildEnemy()     │         │
│  └──────┬───────┘    │  speedMult        │    └────────┬─────────┘         │
│         │            │                   │             │                   │
│         │            │  FireStraight()   │             │ 创建 & 配置        │
│         │            │  FireOrbital()    │             ▼                   │
│         │            │  FireRicochet()   │    ┌──────────────────┐         │
│         │            │  FireShotgun()    │    │   BaseEnemy      │         │
│         │            │  FireChain()      │    │   moveSpeed      │         │
│         │            │  ApplyUpgrades()  │    │   contactDamage  │         │
│         │            │  AddXxxBonus() x5 │    │   OnTriggerStay  │         │
│         │            └────────┬──────────┘    └────────┬─────────┘         │
│         │                     │                        │                   │
│         │                     │ 创建子弹                 │ 持有              │
│         │                     ▼                        ▼                   │
│         │            ┌──────────────────┐    ┌──────────────────┐         │
│         │            │   BaseBullet     │    │  EnemyHealth     │         │
│         │            │   (基类)          │    │  maxHealth       │         │
│         │            │   damage         │    │  TakeDamage()    │         │
│         │            │   speed          │    │  UpdateBar()     │         │
│         │            │   lifetime       │    │  → AddKill()     │         │
│         │            │   IsPaused检查    │    └──────────────────┘         │
│         │            └──┬───┬───┬───┬──┘                                  │
│         │               │   │   │   │                                      │
│         │      ┌────────┘   │   │   └──────────┐                           │
│         │      │      ┌─────┘   └─────┐        │                           │
│         │      ▼      ▼              ▼        ▼                            │
│         │  Straight  Orbital    Ricochet   Chain                           │
│         │  (直线)    (环绕)      (弹射)     (链式)                           │
│         │                                                           │
│         └───────────────── GameManager.IsPaused ◄────────────────────│
│                                                                     │
│  ┌──────────────────┐    ┌──────────────────┐                       │
│  │   GameManager    │    │   WaveManager    │                       │
│  │   (单例)          │◄───│                  │                       │
│  │                  │    │ CurrentWave      │                       │
│  │ IsPaused(static) │    │ KillsThisWave    │                       │
│  │ AddKill()        │    │ KillsNeeded      │                       │
│  │ TriggerGameOver()│    │ DamageMultiplier │                       │
│  │ HandleInput(1-5) │    │ CooldownMultiplier│                      │
│  │ UpdateUI()       │    │ BulletCountBonus │                       │
│  │ bulletTypeText   │    │ BulletSpeedMult  │                       │
│  │ statsText        │    │                   │                       │
│  │ cooldownBar      │    │ OnKill()          │                       │
│  │ playerHPText     │    │ TriggerWaveUp()   │                       │
│  │ gameOverPanel    │    │ ShowUpgradeChoices│                       │
│  └──────────────────┘    │ OnUpgradeChosen() │                       │
│                          │ AdvanceWave()     │                       │
│                          │ CreateUpgradePanel│                       │
│                          └──────────────────┘                       │
└───────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                      ScriptableObject 配置层                      │
│                                                                  │
│  ┌─────────────────────┐    ┌─────────────────────┐              │
│  │     GameConfig      │    │     WaveConfig      │              │
│  │                     │    │                     │              │
│  │ straight/ orbital/  │    │ killsPerWave=100    │              │
│  │ ricochet/ shotgun/  │    │ hpScale/speedScale  │              │
│  │ chain 的冷却/伤害   │    │ spawnRateScale      │              │
│  │ /速度/数量/颜色     │    │ maxEnemiesPerWave   │              │
│  │                     │    │ damagePerWave       │              │
│  │ playerMaxHP=100     │    │                     │              │
│  │ maxEnemies=40       │    │ upgrades[10]        │              │
│  │ orbitalRadius       │    │  ├ 5 global         │              │
│  │ ricochetMaxBounces  │    │  └ 5 per-type       │              │
│  │ shotgunSpreadAngle  │    │                     │              │
│  │ chainMaxHops/range  │    │ UpgradeDef:         │              │
│  │ cameraOrthoSize     │    │  type/name/desc     │              │
│  └─────────────────────┘    │  value/color        │              │
│                              │  targetBulletType?  │              │
│                              └─────────────────────┘              │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                        Utils 工具层                               │
│                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  MeshGenerator   │  │ ExtensionMethods │  │  AutoDestroy  │  │
│  │  (静态方法)       │  │                  │  │               │  │
│  │                  │  │ RandomPointOn    │  │ 定时自毁组件   │  │
│  │ CreatePlayer()   │  │ ScreenEdge()     │  │               │  │
│  │ CreateEnemy()    │  │                  │  └───────────────┘  │
│  │ CreateSwordBullet│  └──────────────────┘                     │
│  │ CreateHealthBar()│                                           │
│  │ SpawnHitParticles│                                           │
│  │ SpawnDeathParticles                                         │
│  └──────────────────┘                                           │
└──────────────────────────────────────────────────────────────────┘
```

---

## 二、运行时流程图

### 2.1 启动初始化流程

```
GameBootstrap.Awake()
    │
    ├─[1] SetupCamera()
    │     └─ 正交相机, orthoSize=8, 背景色=深蓝灰, z=-10
    │
    ├─[2] CreatePlayer()
    │     └─ MeshGenerator.CreatePlayer() → 球体
    │     └─ AddComponent<PlayerHealth>(maxHP=100)
    │
    ├─[3] CreateBulletManager(player)
    │     └─ new BulletManager { playerTransform, config }
    │     └─ BulletManager.Start()
    │         ├─ ApplyConfig() → 读 GameConfig, 填充 cooldowns/speed/count
    │         ├─ activeTypes ← 全部5种 (Straight~Chain)
    │         └─ CreateOrbital() → 3把环绕飞剑
    │
    ├─[4] CreateEnemySpawner()
    │     └─ new EnemySpawner { config }
    │     └─ ApplyConfig() → 读 GameConfig, 填充 spawnInterval/enemyHP 等
    │
    ├─[5] CreateGameManager(bm, es)
    │     └─ new GameManager { bulletManager, enemySpawner, config }
    │
    ├─[6] CreateUI(gm)
    │     └─ Canvas (ScreenSpaceOverlay)
    │     ├─ bulletTypeText    → "1:Straight[ON]  ...  5:Chain[ON]"
    │     ├─ statsText         → "Wave X | [N/M] | Enemies: N  Kills: N"
    │     ├─ playerHPText      → "HP: 100%"
    │     ├─ cooldownBar       → Image(Filled, Horizontal)
    │     ├─ fpsText           → "FPS: 0"
    │     ├─ HelpText          → "Press 1-5 to toggle bullet types"
    │     └─ GameOverPanel     → 黑色半透明 + "Game Over" 文本 (默认隐藏)
    │
    └─[7] CreateWaveManager(bm, es, gm, canvas)
          └─ new WaveManager { config=waveConfig }
          └─ Init() → ApplyConfig() → KillsNeeded=100
          └─ CreateUpgradePanel() → 3按钮面板 (默认隐藏)
```

### 2.2 主循环 (每帧)

```
Unity Update()
    │
    ├── GameManager.Update()
    │     ├─ isGameOver? → R键重开 (LoadScene 0)
    │     ├─ HandleInput()
    │     │    └─ 按键 1/2/3/4/5 → ToggleBulletType(type)
    │     │         └─ BulletManager.ToggleBulletType()
    │     │              ├─ activeTypes.Add/Remove
    │     │              ├─ Orbital 特殊处理: Create/DestroyOrbital()
    │     │              └─ UpdateBulletTypeUI() → [ON]/[OFF]
    │     ├─ UpdateUI()
    │     │    ├─ statsText    ← Wave X | [K/W] | Enemies: N  Kills: N
    │     │    ├─ cooldownBar  ← GetCooldownProgress() (最大冷却进度)
    │     │    └─ playerHPText ← PlayerHealth.HPPercent
    │     └─ UpdateFPS() → 30帧滑动平均
    │
    ├── BulletManager.Update()
    │     ├─ IsPaused? → return
    │     ├─ UpdateOrbital() → 环绕角度+速度, 更新飞剑位置/朝向
    │     └─ for each active type (except Orbital):
    │           ├─ 冷却完成? → FireType(type)
    │           │    ├─ Straight: 找最近敌人 → StraightBullet.Init(dir)
    │           │    ├─ Ricochet: 找最近敌人 → RicochetBullet.Init(dir, maxBounces)
    │           │    ├─ Shotgun:  找最近敌人 → N×StraightBullet(扇形扩散)
    │           │    └─ Chain:   找最近敌人 → ChainBullet.Init(target, hops, range)
    │           └─ lastFireTimes[i] = Time.time
    │
    ├── EnemySpawner.Update()
    │     ├─ IsPaused? → return
    │     └─ 时间到 & 数量未达上限 → BuildEnemy(屏幕边缘随机位置)
    │           ├─ MeshGenerator.CreateEnemy() → 红色球体
    │           ├─ AddComponent<BaseEnemy> + AddComponent<EnemyHealth>
    │           └─ CreateHealthBar() → 绿/黄/红血条
    │
    ├── Bullet.Update() (每个活跃子弹)
    │     ├─ IsPaused? → return
    │     ├─ Straight:   position += dir * speed * dt
    │     ├─ Ricochet:   position += dir * speed * dt; 屏幕边缘反弹检测
    │     ├─ Chain:      状态机 Traveling→Cooldown→FindNextTarget
    │     └─ BaseBullet: 超时销毁
    │
    └── Enemy.Update() (每个活跃敌人)
          ├─ IsPaused? → return
          └─ position += (player.pos - self.pos).normalized * speed * dt
```

### 2.3 伤害流程

```
┌─ 子弹命中敌人 ─────────────────────────────────────────────┐
│                                                            │
│  Bullet.OnTriggerEnter(Collider other)                     │
│    ├─ other.CompareTag("Enemy")?                           │
│    ├─ EnemyHealth.TakeDamage(damage)                       │
│    │    ├─ currentHP -= damage                             │
│    │    ├─ UpdateHealthBar() → 绿→黄→红 血条缩放           │
│    │    └─ HP ≤ 0?                                         │
│    │         ├─ SpawnDeathParticles()                      │
│    │         ├─ GameManager.AddKill()                      │
│    │         │    └─ WaveManager.OnKill()                  │
│    │         │         ├─ isUpgrading? → return            │
│    │         │         ├─ KillsThisWave++                   │
│    │         │         └─ KillsThisWave ≥ KillsNeeded?     │
│    │         │              └─ TriggerWaveUp() → 见下方     │
│    │         └─ Destroy(gameObject)                        │
│    └─ SpawnHitParticles() → URP 粒子效果                   │
│                                                            │
│  * 特殊处理:                                               │
│    Orbital: OnTriggerStay → 每敌人每秒最多受伤1次           │
│    Chain:   手动调用 health.TakeDamage() (禁用基类碰撞)     │
│    Ricochet: 命中后随机转向 + bounceCount++, 到达上限销毁   │
└────────────────────────────────────────────────────────────┘

┌─ 敌人接触玩家 ─────────────────────────────────────────────┐
│                                                            │
│  Enemy.OnTriggerStay(Collider other)                       │
│    ├─ IsPaused? → return                                   │
│    ├─ other.CompareTag("Player")?                           │
│    ├─ 距离上次伤害 ≥ 1s?                                    │
│    ├─ PlayerHealth.TakeDamage(contactDamage)                │
│    │    ├─ IsDead? → return                                │
│    │    ├─ 无敌帧检查 (0.5s CD)                             │
│    │    ├─ currentHP -= damage                             │
│    │    └─ currentHP ≤ 0?                                   │
│    │         └─ GameManager.TriggerGameOver()              │
│    │              ├─ gameOverPanel.SetActive(true)          │
│    │              ├─ gameOverText = "Game Over\nKills: N"  │
│    │              ├─ enemySpawner.enabled = false           │
│    │              └─ bulletManager.enabled = false         │
│    └─ lastDamageTime = Time.time                           │
└────────────────────────────────────────────────────────────┘
```

### 2.4 波次 & 升级流程

```
击杀数达到 KillsNeeded (默认100)
    │
    ▼
WaveManager.TriggerWaveUp()
    ├─ isUpgrading = true          ← 防止重复触发
    ├─ GameManager.IsPaused = true ← 冻结所有 Update()
    │
    └─ ShowUpgradeChoices()
         ├─ upgradePanel.SetActive(true)
         ├─ 从 WaveConfig.upgrades 随机抽取 3 个
         │    (10个池: 5全局 + 5单类型)
         ├─ 填充 3 个按钮的文本和回调
         └─ 等待玩家点击...

玩家点击某个增益按钮
    │
    ▼
OnUpgradeChosen(UpgradeDef def)
    ├─ switch (def.type):
    │    ├─ DamageUp:      DamageMultiplier *= 1+30%
    │    ├─ CooldownDown:  CooldownMultiplier *= 1-20%
    │    ├─ BulletCount:   BulletCountBonus += 1
    │    ├─ Heal:          PlayerHealth.Heal(30%)
    │    ├─ BulletSpeed:   BulletSpeedMultiplier *= 1+25%
    │    ├─ RicochetBounce:→ BulletManager.AddRicochetBounce(+2)
    │    ├─ OrbitalSpeed:  → BulletManager.AddOrbitalSpeed(+40%)
    │    ├─ ChainHops:     → BulletManager.AddChainHops(+1)
    │    ├─ ChainRange:    → BulletManager.AddChainRange(+30%)
    │    └─ ShotgunSpread: → BulletManager.AddShotgunSpreadReduction(-20%)
    │
    ├─ upgradePanel.SetActive(false)
    │
    ├─ AdvanceWave()
    │    ├─ CurrentWave++
    │    ├─ KillsThisWave = 0
    │    ├─ KillsNeeded = base + growth * (wave-1)
    │    ├─ 计算敌人缩放: HP/Speed/SpawnRate/MaxCount/Damage
    │    ├─ EnemySpawner.ApplyWaveScaling(...)
    │    └─ BulletManager.ApplyUpgrades(dmg, cd, cnt, spd)
    │         ├─ DestroyOrbital() → ApplyConfig() → CreateOrbital()
    │         └─ 所有子弹属性更新 (damageMult, cooldownMult, countBonus, speedMult)
    │
    ├─ isUpgrading = false
    └─ GameManager.IsPaused = false  ← 恢复游戏
```

### 2.5 暂停系统

```
GameManager.IsPaused (static bool)

设为 true 时，所有 Update() 入口立即 return:
    ├── BulletManager.Update()          → 子弹停止发射 + 环绕停止旋转
    ├── EnemySpawner.Update()           → 敌人停止生成
    ├── BaseBullet.Update()             → 现有子弹停止移动 + 超时计时器冻结
    ├── BaseEnemy.Update()              → 敌人停止移动
    ├── BaseEnemy.OnTriggerStay()       → 敌人停止造成接触伤害
    ├── StraightBullet.Update()         → 直线子弹停止移动
    ├── RicochetBullet.Update()         → 弹射子弹停止移动
    └── ChainBullet.Update()            → 链式子停止移动/跳转

不受暂停影响:
    ├── GameManager.Update()            → 仍然处理 R 重开(非GameOver时也响应输入?)
    │                                     注: GameOver时仍然处理R键
    ├── GameManager.UpdateFPS()         → FPS仍更新 (使用 unscaledDeltaTime)
    └── Unity UI事件系统                 → 增益按钮仍可点击
```

---

## 三、数据流总览

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  GameConfig │     │ WaveConfig  │     │  PlayerHP   │
│  (.asset)   │     │  (.asset)   │     │  (100 max)  │
└──┬──┬──┬───┘     └──────┬──────┘     └──────┬──────┘
   │  │  │                │                   │
   ▼  ▼  ▼                ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│BulletManager │   │ WaveManager  │   │ PlayerHealth │
│              │   │              │   │              │
│cooldowns[] ◄─┼───│ 累积全局倍率  │   │ TakeDamage() │
│damageMult ◄──┼───│ dmg/cd/cnt/  │   │ Heal()       │
│cooldownMult◄─┼───│ spd multiplier│  │ TriggerGame  │
│countBonus ◄──┼───│              │   │ Over()       │
│speedMult ◄───┼───│ 单类型增益 ──┼──►│              │
│              │   │ AddXxx()     │   └──────┬───────┘
│AddRicochet()◄┼───│              │          │
│AddOrbital()◄─┼───│ 敌人缩放 ────┼──►┌──────┴───────┐
│AddChain()  ◄─┼───│ hp/spd/rate  │   │EnemySpawner  │
│AddShotgun()◄─┼───│ /max/dmg     │   │              │
│              │   └──────┬───────┘   │ApplyWave     │
│FireType()    │          │           │Scaling()     │
│  创建子弹 ───┼──► Bullet.Update()   └──────────────┘
└──────────────┘
        ▲
        │ toggle
        │
   ┌────┴────┐
   │GameManager│
   │按键 1-5   │
   └──────────┘
```

---

## 四、文件清单 (18 .cs + 2 .asset)

| 层 | 文件 | 核心职责 |
|---|---|---|
| **入口** | GameBootstrap.cs | Awake() 构建全部对象，注入依赖 |
| **管理** | GameManager.cs | 单例，输入(1-5/R)，UI刷新，击杀计数，GameOver，IsPaused |
| **管理** | BulletManager.cs | 单例，5种子弹同时开火，冷却/伤害/全局&单类型增益 |
| **管理** | EnemySpawner.cs | 屏幕边缘生成敌人，波次难度缩放 |
| **管理** | PlayerHealth.cs | HP，受击无敌帧，按百分比治疗 |
| **管理** | WaveManager.cs | 波次进度，增益UI面板(3选1)，全局/单类型增益应用 |
| **配置** | GameConfig.cs | ScriptableObject：子弹/敌人/玩家/屏幕参数 |
| **配置** | WaveConfig.cs | ScriptableObject：波次触发/缩放曲线/10个增益定义 |
| **子弹** | BaseBullet.cs | 基类：伤害/速度/寿命/命中VFX/IsPaused检查 |
| **子弹** | StraightBullet.cs | 直线弹道，飞剑朝向移动方向 |
| **子弹** | OrbitalBullet.cs | 环绕飞行，每敌人每秒伤害CD |
| **子弹** | RicochetBullet.cs | 屏幕边缘/敌人反弹(最多N次) |
| **子弹** | ChainBullet.cs | 状态机(Traveling→Cooldown)，链式弹射+LineRenderer连线 |
| **敌人** | BaseEnemy.cs | 追踪玩家，接触伤害(1s CD)，IsPaused检查 |
| **敌人** | EnemyHealth.cs | HP，死亡粒子，通知 GameManager.AddKill() |
| **工具** | MeshGenerator.cs | 程序化网格：飞剑/球体/血条/粒子特效 |
| **工具** | ExtensionMethods.cs | Camera 屏幕边缘随机坐标 |
| **工具** | AutoDestroy.cs | 定时自毁组件 |
| **资源** | GameConfig.asset | 5种子弹配置 + 敌人/玩家/屏幕参数 |
| **资源** | WaveConfig.asset | 波次配置 + 10个增益定义 |
