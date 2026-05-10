# 肉鸽塔防游戏 - 子弹系统测试

## 项目概述
开发一个吸血鬼幸存者风格的肉鸽塔防游戏，目前专注于测试各种子弹类型。玩家角色固定在屏幕中心，敌人从屏幕边缘向中心移动。需要实现多种子弹系统和基础敌人。

**技术栈：Unity 2022.3 LTS + URP（通用渲染管线）**

---

## 一、核心需求

### 1. 玩家角色
- 固定在屏幕正中央（使用Transform，不移动）
- 可以使用一个简单的Sprite或3D模型（如立方体+材质）
- 不需要移动，只需要视觉上可以转动方向或保持静止

### 2. 子弹类型（重点测试）

**【重要】所有子弹都是自动发射，无需玩家点击射击，每个子弹类型有独立的冷却时间（CD）**

#### 2.1 直线发射子弹
- 自动向最近的敌人方向发射（如果没有敌人则等待）
- 直线飞行，击中敌人后消失或穿透
- **冷却时间：0.3秒**
- 基础伤害：10
- 子弹速度：10单位/秒
- 子弹预制体：小球体 + Trail特效（可选）

#### 2.2 环绕子弹
- 围绕角色旋转的子弹/护盾
- 接触敌人造成伤害
- 可以有多颗环绕物（如3颗）
- 旋转半径：1.5单位，速度：360度/秒
- **环绕子弹没有冷却时间，持续旋转造成伤害，但每个敌人每秒只能被同一环绕子弹击中1次（使用伤害CD）**
- 子弹预制体：小球体 + 发光材质

#### 2.3 弹射子弹
- 自动向随机敌人或最近敌人发射
- 在边界（屏幕边缘）之间反弹
- 每次击中敌人弹射方向改变
- 最大弹射次数：5次
- **冷却时间：0.5秒**
- 伤害：8
- 子弹速度：8单位/秒
- 碰撞检测：使用OnCollisionEnter或OnTriggerEnter

#### 2.4 散弹
- 自动发射，一次发射多颗子弹
- 扇形状扩散，自动瞄准敌人区域
- 子弹数量：5颗
- 扩散角度：60度
- **冷却时间：0.8秒**
- 单颗伤害：6
- 子弹速度：10单位/秒

#### 2.5 连锁子弹（闪电链）
- 自动向最近的敌人发射
- 击中第一个敌人后自动跳转下一个最近敌人
- 最大连锁次数：3次
- 连锁距离限制：5单位
- **冷却时间：0.6秒**
- 伤害：12
- 视觉效果：LineRenderer或闪电特效

### 3. 敌人系统
- **基础敌人类型**
  - 从屏幕四周随机生成（使用Unity的Instantiate）
  - 向屏幕中心玩家匀速移动（使用Transform.Translate或Rigidbody）
  - 移动速度：2单位/秒
  
- **属性**
  - 生命值：30
  - 碰撞体积：SphereCollider，半径0.5单位
  - 颜色：红色材质，便于识别

- **生成机制**
  - 持续自动生成（使用InvokeRepeating或Coroutine）
  - 最大同时存在数量：15只
  - 生成间隔：1秒
  - 生成位置：屏幕边界外（Camera Viewport 转 World坐标）

### 4. 战斗机制
- 子弹命中敌人减少生命值
- 敌人生命值归零时销毁（Destroy）
- 子弹击中特效：简单的粒子系统（ParticleSystem）
- **测试阶段玩家无敌**（不实现玩家受伤，专注测试子弹）

### 5. 输入控制
- **键盘数字键1-5**：切换子弹类型（使用Input.GetKeyDown）
  - 1: 直线子弹（自动射击，CD 0.3秒）
  - 2: 环绕子弹（被动旋转）
  - 3: 弹射子弹（自动射击，CD 0.5秒）
  - 4: 散弹（自动射击，CD 0.8秒）
  - 5: 连锁子弹（自动射击，CD 0.6秒）
- 切换后立即生效，自动开始按新类型射击
- 屏幕上显示当前选中的子弹类型（TextMeshPro）

### 6. UI显示
- 当前子弹类型文字和图标
- 当前敌人数/击杀数
- 当前子弹类型的冷却进度条（使用Image.fillAmount）
- FPS显示（可选）
- 敌人头上显示简单血条（World Space Canvas + Slider）

---

## 二、Unity项目结构
Assets/
├── Scripts/
│ ├── Core/
│ │ ├── GameManager.cs # 游戏主控制器，管理子弹切换和生成
│ │ ├── BulletManager.cs # 子弹系统管理器，处理自动射击
│ │ └── EnemySpawner.cs # 敌人生成器
│ ├── Bullet/
│ │ ├── BaseBullet.cs # 子弹基类
│ │ ├── StraightBullet.cs # 直线子弹
│ │ ├── OrbitalBullet.cs # 环绕子弹
│ │ ├── RicochetBullet.cs # 弹射子弹
│ │ ├── ShotgunBullet.cs # 散弹
│ │ └── ChainBullet.cs # 连锁子弹
│ ├── Enemy/
│ │ ├── BaseEnemy.cs # 敌人基类
│ │ └── EnemyHealth.cs # 敌人生命值和血条
│ └── Utils/
│ └── ExtensionMethods.cs # 工具方法
├── Prefabs/
│ ├── Bullets/
│ │ ├── StraightBullet.prefab
│ │ ├── OrbitalBullet.prefab
│ │ ├── RicochetBullet.prefab
│ │ ├── ShotgunBullet.prefab
│ │ └── ChainBullet.prefab
│ ├── Enemy.prefab
│ └── VFX/
│ └── HitEffect.prefab
├── Materials/
│ ├── EnemyMaterial.mat
│ ├── BulletMaterial.mat
│ └── ...
├── Scenes/
│ └── GameScene.unity
└── Settings/
└── URPAsset.asset

---

