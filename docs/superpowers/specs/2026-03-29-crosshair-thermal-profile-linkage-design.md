# AspenBurner 准心联动热管理设计

版本: v0.6.1  
日期: 2026-03-29

## 1. 目标

把本机已经验证过的 A/C 热管理档联动进 AspenBurner：

- A 档：`高性能 + Performance + Maximum + Discrete GPU only`
- C 档：`平衡 + Entertainment + Maximum + MSHybrid`

行为要求：

1. 用户手动开启准心 runtime，且 CPU 角标开启后，按 5 分钟节奏进入并重申 A 档。
2. 用户手动暂停/关闭准心，或关闭 CPU 角标时，立即回 C 档。
3. 仅在确认是当前这台 Clevo/Colorful 机器且存在可控链路时启用。
4. 不因为 alt-tab、目标窗口短暂丢失、等待游戏启动而切回 C 档。

## 2. 非目标

- 不把档位选择暴露成新的复杂设置页。
- 不尝试支持所有品牌笔记本。
- 不在 `OverlayRuntime` 的 200ms 状态 tick 内直接做 CC40 / powercfg 控制。
- 不改动现有准心显示规则、前台窗口检测规则和设置面板布局。

## 3. 需求统一

用户后续补充已经把“准心开始显示”统一成“用户手动开启准心 runtime”。  
原因很简单：用户通常会在真正进入三角洲前 5 分钟就启动准心工具，如果必须等待目标窗口命中才切 A 档，那么 A 档会在进游戏后才生效，和真实使用方式冲突。

所以本轮设计采用以下统一语义：

- `开启准心` = `AspenBurner 处于 Running，且 StatusEnabled=true`
- `关闭准心` = `AspenBurner 被 Pause / Stop，或 StatusEnabled=false`
- `等待目标窗口` 不视为关闭准心
- `桌面预览` 不参与 A 档晋升

## 4. 方案比较

### 方案 A：把切档逻辑直接塞进 `OverlayRuntime`

- 优点：表面上改动少。
- 缺点：渲染/前台检测/热管理耦合到一个 200ms tick，后面一定变脆。
- 结论：垃圾方案，不用。

### 方案 B：独立热管理状态机 + 应用层定时器

- 优点：职责清晰；渲染逻辑不被污染；易测；门禁与机型特例可单独收敛。
- 缺点：会多几个小类和一组状态机测试。
- 结论：采用。

### 方案 C：完全依赖外部脚本或 skill

- 优点：复用现有调优脚本最快。
- 缺点：运行时依赖用户目录 skill，不是软件自身能力；分发时容易断。
- 结论：参考实现思路，但不作为产品内集成方案。

## 5. 最终设计

### 5.1 结构

新增三层：

1. `ThermalProfileController`
   - 纯状态机。
   - 只关心当前配置、当前运行态、是否需要启停 5 分钟定时器、是否需要请求 A/C 档切换。

2. `IThermalProfileDriver`
   - 抽象真实切档动作。
   - 负责把 `A/C` 请求映射到电源方案和 CC40 UI 自动化。

3. `ClevoThermalProfileDriver`
   - 本机专用驱动。
   - 负责机型门禁、Control Center 路径解析、UI Automation 选项切换、`powercfg /s` 调用。

`AspenBurnerApplicationContext` 只负责组装和转发：

- 订阅 `OverlayRuntime.HealthChanged`
- 接收设置变更
- 驱动 5 分钟定时器
- 把状态机产出的动作交给驱动执行

### 5.2 状态机规则

#### A 档晋升条件

满足以下全部条件时，启动 5 分钟晋升定时器：

- `Lifecycle = Running`
- `StatusEnabled = true`
- `Target != DesktopPreview`
- 驱动确认 `IsSupported = true`

定时器每 5 分钟触发一次：

- 如果条件仍满足，则请求切换 A 档
- 如果条件不满足，则停止定时器，不切 C 档

#### C 档回退条件

以下任一条件成立时，立即请求 C 档，并停止定时器：

- `Lifecycle = Paused`
- `Lifecycle = Stopped`
- `StatusEnabled = false`

#### 明确不触发 C 的情况

- alt-tab
- 游戏窗口短暂失焦
- `Target = WaitingForTarget`
- 仅仅还没进入三角洲

### 5.3 机型和能力门禁

驱动必须同时满足以下条件才会启用：

1. WMI 能识别本机为 `NP5x_6x_7x_SNx` 系列。
2. 能定位 `CLEVOCO.FnhotkeysandOSD_*` 包或已缓存的 `CC40.exe`。
3. 能解析出 A/C 档所需的控件 AutomationId。

如果任一条件不满足：

- 状态机照常运行
- 驱动返回 no-op
- 不影响准心本体

### 5.4 A/C 档定义

#### A 档

- Windows 电源方案：`High Performance`
- CC40 Power Mode：`Performance`
- CC40 Fan Mode：`Maximum`
- GPU Switch：`Discrete GPU only`

#### C 档

- Windows 电源方案：`Balanced`
- CC40 Power Mode：`Entertainment`
- CC40 Fan Mode：`Maximum`
- GPU Switch：`MSHybrid`

### 5.5 错误处理

- 切档失败只写日志，不打断主程序。
- 同一个状态不会重复高频切换。
- 应用退出前尽量请求一次 C 档，但失败不阻止退出。

## 6. 数据流

1. `OverlayRuntime` 发布 `HealthSnapshot`
2. `AspenBurnerApplicationContext` 接收 snapshot 和当前 config
3. `ThermalProfileController` 计算动作：
   - `StartCadence`
   - `StopCadence`
   - `ApplyA`
   - `ApplyC`
   - `NoOp`
4. `AspenBurnerApplicationContext` 驱动 WinForms 5 分钟定时器
5. `ClevoThermalProfileDriver` 执行真实切档
6. 成功/失败写入日志

## 7. 测试策略

必须覆盖：

1. `Running + StatusEnabled=true` 时启动 5 分钟晋升定时器。
2. 到点时切 A 档。
3. `Paused` / `Stopped` / `StatusEnabled=false` 时立即切 C 档。
4. `WaitingForTarget` 不切 C 档。
5. `DesktopPreview` 不进 A 档。
6. 不支持机型时，驱动 no-op。
7. 应用退出时请求 C 档。

## 8. 验收标准

- 准心软件在本机上可自动联动 A/C 热管理档。
- 手动关闭准心后，不需要人工干预即可回到 C 档。
- alt-tab 和等待游戏窗口期间不会错误降回 C。
- 现有 overlay 测试和主程序测试不回归。
- 新增状态机测试覆盖上述核心分支。
