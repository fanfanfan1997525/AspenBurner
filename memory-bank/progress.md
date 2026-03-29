# 项目进度
版本: v0.5.4

已完成
- 20260326: 建立独立准心工具、配置文件、启动/停止脚本和 memory-bank。
- 20260327: 修复整屏透明窗导致的黑屏与闪烁，改为小尺寸 overlay，仅在目标游戏前台显示。
- 20260328: 完成交互式设置面板、CPU 角标、真实温度优先链路、仓库初始化与远端发布。
- 20260328: 完成 AspenBurner 桌面化重构，交付 WinForms 主程序、托盘、设置窗、兼容脚本和 CLI。
- 20260328: 完成兼容入口热修，覆盖参数兼容、单实例配置重载和命令链回归测试。
- 20260328: 完成 Control Center 真温度热修，增加运行库定位和本地缓存装载。
- 20260328: 完成稳定性与设置体验热修，新增全局异常日志、tick 防崩、推荐预设、Reset、设置即时应用。
- 20260329: 完成 AspenBurner.Bench CPU 验证工具，并用于多组电源/风扇/模式对比。
- 20260329: 新增 `AverageTemperatureC` 到 AspenBurner.Bench 遥测与报告链路，并通过全量测试。
- 20260329: 创建并验证本地 skill `clevo-gaming-thermal-tuning`，可直接切 CC40 档位、切 Windows 电源方案并运行 bench。

待观察
- Delta Force 实战 FPS 直采仍缺 PresentMon 稳定链路，当前更依赖 CPU/GPU/温度/Event37 侧证据。
- 若后续继续做温控优化，优先沿 skill 固化的档位矩阵继续，而不是重复从 BIOS/EC 学起。