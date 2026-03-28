# 当前上下文 版本: v0.4.4

- 20260328: 用户要求 CPU 角标显示真实温度，明确拒绝 `TZ` 近似值。
- 20260328: 已确认 Windows 标准传感器链路拿不到可信 CPU Package 温度；官方 `Control Center` 可显示真实 `Clock/Temperature`。
- 20260328: 已验证两条厂商链路：
  - OCR 可读到控制中心界面的真实读数。
  - 更优方案是直接加载 `CC40.exe` 的 `CC40.PageSystemElement.Interface_CPU`，在设置 `DCHU` 原生 DLL 搜索路径后直接读取 `Clock/Temperature/Usage`。
- 20260328: `CrosshairOverlay.ps1` 已改为优先使用 `Control Center` vendor provider，失败时回退到 `% Processor Performance + 可用直连温度传感器`。
- 20260328: 新增 `Resolve-PreferredCpuStatusSnapshot`，解决 vendor 时钟预热阶段短暂卡在基频时的回退合并逻辑。
- 20260328: 当前实测 vendor 读数约为 `4218/74C`，与控制中心界面读数一致量级。
- 20260328: 已在 `F:\software\crosshair-overlay` 初始化本地 git 仓库，并完成远端仓库 `https://github.com/fanfanfan1997525/AspenBurner.git` 的推送。
- 20260328: 远端 `main` 当前已落到提交 `12f2472`，本地与远端一致。
