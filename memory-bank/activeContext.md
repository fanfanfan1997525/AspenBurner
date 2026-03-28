# 当前上下文
版本: v0.5.1

- 20260328: 热修兼容入口无 CPU 角标。根因有二：兼容脚本仍传 `-ConfigPath`，桌面运行时只认 `--config-path`；单实例二次启动只发 `resume`，旧主进程不会重载磁盘配置。
- 20260328: 已补 `AppLaunchRequestParser` 和命令链回归测试，兼容 `-ConfigPath/--config-path`，并让 `AppCommand` 可携带配置路径。
- 20260328: 主进程收到远程 `resume/show-settings/preview/health` 前会先按磁盘配置重载；兼容脚本已改为统一传 `--config-path`。
- 20260328: `dotnet test` 41/41 通过，PowerShell 脚本语法检查通过，Release 构建成功。旧管理员实例仍在运行，替换 `dist` 发布物需用户确认 UAC 关闭旧进程后再重启。
