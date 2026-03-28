# 项目进度
版本: v0.5.3

已完成
- 20260326: 建立独立准心工具、配置文件、启动/停止脚本和 memory-bank。
- 20260327: 修复整屏透明窗导致的黑屏与闪烁，改为小尺寸 overlay，仅在目标游戏前台显示。
- 20260328: 完成交互式设置面板、CPU 角标、真实温度优先链路、仓库初始化与远端发布。
- 20260328: 完成 AspenBurner 桌面化重构，交付 WinForms 主程序、托盘、设置窗、兼容脚本和 CLI。
- 20260328: 完成兼容入口热修，覆盖参数兼容、单实例配置重载和命令链回归测试。
- 20260328: 完成 Control Center 真温度热修，增加运行库定位和本地缓存装载。
- 20260328: 完成稳定性与设置体验热修，新增全局异常日志、tick 防崩、推荐预设、Reset、设置即时应用。
- 20260328: 验证通过 `dotnet test` 54/54、`dotnet build -c Release` 成功、`start -> preview -> stop` smoke 成功。

待观察
- 20260328: 若用户继续报告游戏内不显示或保存无效，优先排查是否存在旧管理员实例截流单实例命令。

- 20260328: BIOS/EC investigation complete: Colorful HX public page has no BIOS, Clevo mirror B10723 and EC10708 downloaded, B10724 entry present but unavailable.
