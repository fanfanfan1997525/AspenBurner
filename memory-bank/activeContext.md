# 当前上下文 版本: v0.4.3

- 20260328: 用户要求 CPU 角标显示真实温度，明确拒绝 `TZ` 近似值。
- 20260328: 已确认 Windows 标准传感器链路拿不到可信 CPU Package 温度；官方控制中心 `CC40` 界面可显示真实 `Clock/Temperature`。
- 20260328: 已验证两条厂商链路：
  - 屏幕 OCR 可读到控制中心中的真实读数。
  - 更优方案为直接加载 `CC40.exe` 的 `CC40.PageSystemElement.Interface_CPU`，在设置 `DCHU` 原生 DLL 搜索路径后可直接读 `Clock/Temperature/Usage`。
- 20260328: `CrosshairOverlay.ps1` 已改为优先使用 Control Center vendor provider，失败时回退到 `% Processor Performance + 可用直连温度传感器`。
- 20260328: 新增 `Resolve-PreferredCpuStatusSnapshot`，解决 vendor 时钟预热阶段暂时卡在基频时的回退合并逻辑。
- 20260328: 当前实测 vendor 读数约为 `4218/74C`，与控制中心界面读数一致量级。
- 20260328: 已触发一次 `Start-Crosshair.cmd`，是否真正接管管理员实例仍取决于桌面 UAC 确认。
