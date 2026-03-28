# 当前上下文
版本: v0.5.2

- 20260328: 热修 CPU 真温度链路。根因不是 git 回退，而是 .NET 8 进程直接枚举 `WindowsApps`/加载包内原生 DLL 失败，桌面版因此退回 `Fallback`，只剩频率没有温度。
- 20260328: 已新增 `ControlCenterRuntimeLocator`，优先从运行中的 `FnKey/CC40` 进程路径反推出包根目录，绕开 .NET 对 `WindowsApps` 的目录枚举限制。
- 20260328: 已新增 `ControlCenterRuntimeCache`，启动时把 `CC40/DCHU` 镜像到 `%LOCALAPPDATA%\\AspenBurner\\vendor-cache`，再从缓存加载厂商运行库。
- 20260328: 本机集成验证已确认：消息泵存在时，新链路能读到真实值，样本曾返回 `FrequencyMHz=2100/3777`、`TemperatureC=98`；说明真正剩余风险是旧管理员实例仍在截流单实例启动。
