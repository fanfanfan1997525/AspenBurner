# 当前上下文
版本: v0.5.5

- 20260329: AspenBurner 已完成桌面化、设置面板、托盘、CLI、兼容脚本、CPU 遥测与 Bench 工具。
- 20260329: 已固化本机调优 skill `C:\Users\Aspen\.codex\skills\clevo-gaming-thermal-tuning\`，可切 CC40、电源方案并跑 bench。
- 20260329: 最新 bench 结论已统一：
  - A 档 = `高性能 + Performance + Maximum + dGPU only`，当前性能最强。
  - C 档 = `平衡 + Entertainment + Maximum + MSHybrid`，当前长期更凉。
  - B 档不再推荐；D 档仅用于诊断。
- 20260329: 用户当前新需求是把 A/C 档联动集成进 AspenBurner：
  - 手动开启准心 runtime 且 `StatusEnabled=true` 后，按 5 分钟节奏切/重申 A 档。
  - 手动暂停/关闭准心，或关闭 CPU 角标时，立即切回 C 档。
  - 不因 alt-tab、目标窗口短暂丢失而回 C。
  - 仅在确认是这台 Clevo/Colorful 机器且具备可控能力时启用。
- 20260329: 实现策略已统一为独立热管理状态机，不把切档逻辑塞进 `OverlayRuntime` 的 200ms tick。
