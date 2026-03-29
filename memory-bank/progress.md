# 项目进度
版本: v0.5.5

已完成
- 20260326: 建立独立准心工具、配置文件、启动/停止脚本和 memory-bank。
- 20260327: 修复整屏透明窗导致的黑屏与闪烁，改为小尺寸 overlay，仅在目标游戏前台显示。
- 20260328: 完成交互式设置面板、CPU 角标、真实温度优先链路、仓库初始化与远端发布。
- 20260328: 完成 AspenBurner 桌面化重构，交付 WinForms 主程序、托盘、设置窗、兼容脚本和 CLI。
- 20260328: 完成兼容入口热修、Control Center 温度热修、稳定性与设置应用热修。
- 20260329: 完成 AspenBurner.Bench CPU 验证工具，并用于多组功耗/风扇/显示模式对比。
- 20260329: Bench 新增 `AverageTemperatureC` 输出，并通过全量 `dotnet test`。
- 20260329: 创建并验证本地 skill `clevo-gaming-thermal-tuning`，可直连 CC40、电源方案和 bench。
- 20260329: 完成 A/B/C/D 四组本机热管理档对比，当前统一结论为：A 性能最强，C 长期更稳。

待观察
- Delta Force 真实 FPS 直采仍缺稳定 PresentMon 链路，当前主要依赖 CPU/GPU/温度/Event37 侧证据。
- 若联动功能落地后仍有体验问题，优先继续沿状态机和机型门禁收敛，而不是往 `OverlayRuntime` 再打补丁。
- 20260329: 已完成准心联动 A/C 热管理档：
  - 新增 `ThermalProfileController / Coordinator / Driver`
  - 新增本机门禁、脚本定位、5 分钟 cadence、后台串行 apply
  - 新增 20+ 条热管理相关测试
  - Release 实机验证通过，`--stop` 可把 A 拉回 C
- 20260329: 正在修复新的样式刷新回归：真实准心对 `颜色 / 透明度` 更新不稳定，计划仅修两只 overlay form 的即时重绘。
- 20260329: 已完成样式刷新修复：真实准心与 CPU 角标在颜色/透明度等非几何参数变化时立即重绘；全量测试与 Release 构建通过。
