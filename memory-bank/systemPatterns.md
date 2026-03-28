# 系统模式
版本: v0.5.0

- 应用层
  - `Program`: 参数解析、单实例互斥、二次实例命令转发
  - `AspenBurnerApplicationContext`: 托盘、设置窗、保存节流、兼容命令路由

- 运行时层
  - `OverlayRuntime`: 前台窗口检测、准心显示/隐藏、防抖、桌面预览、健康状态发布
  - `ForegroundWindowSource`: Win32 前台窗口读取

- UI 层
  - `CrosshairOverlayForm`: 真实准心透明窗
  - `StatusOverlayForm`: CPU 角标透明窗
  - `SettingsForm`: 参数编辑、实时反馈
  - `PreviewCanvas`: 左侧预览与角标拖拽

- 领域/配置层
  - `CrosshairConfig*`: 配置模型、迁移、校验、序列化
  - `CrosshairGeometry`: 准心几何
  - `StatusOverlayPlacement`: 角标锚点与拖拽换算
  - `StatusTextFormatter`: CPU 文本格式化
  - `ColorResolver`: 颜色解析

- 遥测层
  - `ControlCenterCpuStatusProvider`: 优先读取厂商数据
  - `FallbackCpuStatusProvider`: Windows 通用频率回退
  - `CpuStatusService`: provider 合并与鲜度判断

- 兼容层
  - `AspenBurner.Common.ps1`
  - `Start/Configure/Stop-Crosshair.ps1`
  - `.cmd` 入口继续保留，只负责提权和命令转发
