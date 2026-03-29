# 当前上下文
版本: v0.5.4

- 20260328: AspenBurner 已完成桌面化，包含托盘、设置面板、兼容脚本、CLI 与真实 CPU 遥测链路。
- 20260328: Overlay 侧已完成稳定性修复：异常日志、tick 防崩、设置即时应用、推荐预设、Reset。
- 20260328: 固件排查已做过 BIOS/EC 回刷；当前 BIOS=1.07.23、EC=7.08，但系统层 Event37 问题已在 bench 中不再复现。
- 20260329: AspenBurner.Bench 已成为 CPU/热行为验证工具，覆盖帧循环、多核吞吐、Control Center 遥测与 Event37。
- 20260329: Bench 新增 `AverageTemperatureC` 输出，当前全量测试通过。
- 20260329: 已沉淀本机调优 skill：`C:\Users\Aspen\.codex\skills\clevo-gaming-thermal-tuning\`。
- 20260329: skill 已实机验证可切 CC40 档位、切 Windows 电源方案、运行 AspenBurner.Bench。
- 20260329: 当前推荐游戏默认档：`HighPerformance + Entertainment + Custom`。
- 20260329: 当前推荐极限降温档：`Balanced + Entertainment + Maximum`。
- 20260329: 当前推荐锁频排查档：`Aspen Safe Gaming(3600) + Entertainment + Maximum`。