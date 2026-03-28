# 当前上下文
版本: v0.5.3

- 20260328: 本轮聚焦 AspenBurner 稳定性和设置体验回归。
- 20260328: 已加运行时异常防护，`OverlayRuntime` 的 state tick 出错只记日志并降级，不再直接打死进程。
- 20260328: 已给 WinForms 进程挂全局异常日志，后续崩溃可在 `logs/aspenburner-*.log` 追栈。
- 20260328: 已修设置面板配置应用链路，改动配置会立即驱动真实 overlay 刷新，不再只改预览。
- 20260328: 已补 `Reset`、推荐预设、小绿十字/小黄十字/黄 T 字，并保留 CPU 角标设置。
- 20260328: 已补窗体初始化回归测试，修掉预设说明标签初始化顺序导致的 NRE。
- 20260328: 下一步若用户继续反馈“保存不生效”，先查是否仍在使用旧管理员实例或旧快捷方式。

- 20260328: BIOS/EC research: matched Clevo NPxxSNx(-G) baseline, downloaded B10723 + EC10708, confirmed B10724 listing exists but source file is missing.

- 20260329: Firmware rollback status: BIOS now 1.07.23 and EC 7.08, but Event 37 persists and Win32_Processor still reports 2100/2100 MHz; root cause not resolved by firmware version rollback alone.
