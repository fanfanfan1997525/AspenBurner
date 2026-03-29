# AspenBurner

版本: v0.6.0  
日期: 2026-03-29

AspenBurner 是一个面向 Windows 游戏场景的桌面准心工具，提供小型十字准心、CPU 遥测角标、托盘常驻、交互式设置面板，以及针对当前 Clevo / 七彩虹机型的热管理档位联动。

## 当前能力

- 小型十字准心：支持 RGB、长度、粗细、间距、描边、透明度、中心偏移。
- 四臂开关：可组合为十字、T 形等样式。
- CPU 角标：支持位置、边距、字体、颜色、透明度、刷新间隔。
- 设置面板：支持实时预览、数值输入、推荐参数、角标拖拽。
- 托盘常驻：支持显示设置、桌面预览、暂停、恢复、退出。
- 单实例运行：二次启动通过命名管道转发命令到主实例。
- 兼容入口：保留 `Start/Configure/Stop` 脚本与 `.cmd` 包装。
- 自动化入口：提供 `AspenBurner.Cli.ps1/.cmd`。
- 热管理联动：在本机可识别且可控时，支持准心与 A/C 档联动。
- Bench 工具：提供 CPU 单线程/多线程验证与 `Event 37` 侧证。

## 目录

- `src/AspenBurner.App/`: WinForms 主程序。
- `src/AspenBurner.Bench/`: CPU 验证工具。
- `build/release/`: Windows 发布与安装器脚本。
- `config/crosshair.json`: 默认配置。
- `Start-Crosshair.cmd/.ps1`: 启动或恢复准心。
- `Configure-Crosshair.cmd/.ps1`: 打开设置窗口。
- `Stop-Crosshair.cmd/.ps1`: 停止主程序。
- `AspenBurner.Cli.cmd/.ps1`: 自动化入口。
- `memory-bank/`: 项目外部大脑。

## 使用

推荐直接双击：

- `Start-Crosshair.cmd`
- `Configure-Crosshair.cmd`
- `Stop-Crosshair.cmd`

也可以直接运行主程序：

```powershell
.\dist\AspenBurner\AspenBurner.exe --config-path .\config\crosshair.json --resume
.\dist\AspenBurner\AspenBurner.exe --config-path .\config\crosshair.json --show-settings
.\dist\AspenBurner\AspenBurner.exe --stop
```

CLI 示例：

```powershell
.\AspenBurner.Cli.ps1 start
.\AspenBurner.Cli.ps1 configure
.\AspenBurner.Cli.ps1 preview -PreviewSeconds 3
.\AspenBurner.Cli.ps1 stop
```

如果目标游戏以管理员权限运行，兼容脚本会自动请求 UAC 提权。

## 配置

默认配置文件位于 `config/crosshair.json`。常用字段如下：

```json
{
  "Color": "Custom",
  "ColorR": 0,
  "ColorG": 255,
  "ColorB": 0,
  "Length": 3,
  "Gap": 4,
  "Thickness": 1,
  "OutlineThickness": 0,
  "Opacity": 250,
  "OffsetX": 0,
  "OffsetY": 0,
  "ShowLeftArm": true,
  "ShowRightArm": true,
  "ShowTopArm": true,
  "ShowBottomArm": true,
  "StatusEnabled": true,
  "StatusPosition": "TopRight",
  "StatusOffsetX": 60,
  "StatusOffsetY": 24,
  "StatusRefreshMs": 1500,
  "StatusTextColor": "Green",
  "StatusOpacity": 220,
  "StatusFontSize": 11,
  "StatusShowTemperature": true
}
```

## 开发

运行测试：

```powershell
dotnet test .\AspenBurner.sln
Invoke-Pester .\tests\pester\Release.Common.Tests.ps1
Invoke-Pester .\tests\pester\AspenBurner.Common.Tests.ps1
```

本地发布：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\release\Build-Release.ps1
```

生成产物：

- `artifacts\releases\v0.6.0\AspenBurner-v0.6.0-win-x64-portable.zip`
- `artifacts\releases\v0.6.0\AspenBurner-v0.6.0-win-x64-setup.exe`

## 已知限制

- 目标窗口识别当前默认只包含 `DeltaForceClient-Win64-Shipping` 与 `delta_force_launcher`。
- 独占全屏下，Windows 叠加层仍可能不可见，优先使用无边框或窗口化全屏。
- 真温度优先走厂商链路；链路不可用时不会伪装成真实温度。
