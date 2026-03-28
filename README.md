# AspenBurner

版本: v0.5.0  
日期: 2026-03-28

AspenBurner 现在是一个专门的 Windows 桌面软件，不再以 PowerShell 透明窗脚本作为主运行时。当前主程序为 `dist\AspenBurner\AspenBurner.exe`，保留 `Start/Configure/Stop` 脚本与 `.cmd` 入口作为兼容壳层，同时提供 `AspenBurner.Cli.ps1/.cmd` 作为自动化与自验入口。

## 当前能力

- 小型十字准心，支持 `RGB`、长度、粗细、间距、描边、透明度、中心偏移
- 四臂开关，可组合为十字、T 形等样式
- CPU 角标，支持位置锚点、边距、文字颜色、字号、刷新间隔
- 左侧实时预览，支持拖动 CPU 角标调整位置
- 托盘常驻，支持显示设置、桌面预览 8 秒、暂停显示、退出程序
- 单实例运行，二次启动会转发命令到主实例
- 兼容旧入口：
  - `Start-Crosshair.cmd/.ps1`
  - `Configure-Crosshair.cmd/.ps1`
  - `Stop-Crosshair.cmd/.ps1`
- 自动化入口：
  - `AspenBurner.Cli.cmd`
  - `AspenBurner.Cli.ps1`
- 日志落盘到 `logs\`

## 目录

- `dist\AspenBurner\AspenBurner.exe`: 已发布桌面程序
- `src\AspenBurner.App\`: C# WinForms 主程序源码
- `tests\AspenBurner.App.Tests\`: MSTest 自动化测试
- `config\crosshair.json`: 兼容旧版的配置文件
- `Start-Crosshair.cmd/.ps1`: 启动/恢复显示
- `Configure-Crosshair.cmd/.ps1`: 打开设置窗口
- `Stop-Crosshair.cmd/.ps1`: 请求主程序退出
- `AspenBurner.Cli.cmd/.ps1`: 自动化命令入口
- `logs\`: 运行日志

## 使用

推荐直接双击：

- `Start-Crosshair.cmd`
- `Configure-Crosshair.cmd`
- `Stop-Crosshair.cmd`

也可以直接运行发布物：

```powershell
.\dist\AspenBurner\AspenBurner.exe --config-path .\config\crosshair.json --resume
.\dist\AspenBurner\AspenBurner.exe --config-path .\config\crosshair.json --show-settings
.\dist\AspenBurner\AspenBurner.exe --stop
```

自动化命令入口：

```powershell
.\AspenBurner.Cli.ps1 start
.\AspenBurner.Cli.ps1 configure
.\AspenBurner.Cli.ps1 preview -PreviewSeconds 3
.\AspenBurner.Cli.ps1 stop
```

如果游戏以管理员权限运行，兼容脚本会请求 UAC 提权。需要你在桌面确认一次。

## 配置

配置文件位于 `config\crosshair.json`。常用字段如下：

```json
{
  "Color": "Custom",
  "ColorR": 130,
  "ColorG": 80,
  "ColorB": 126,
  "Length": 20,
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

说明：

- `Color`: `Green / Yellow / Custom`
- `ColorR/G/B`: 自定义颜色通道
- `Length`: 单臂长度
- `Gap`: 中心间距
- `Thickness`: 线宽
- `OutlineThickness`: 描边宽度
- `Opacity`: 准心透明度
- `OffsetX/OffsetY`: 准心中心偏移
- `ShowLeftArm/ShowRightArm/ShowTopArm/ShowBottomArm`: 四臂开关
- `StatusEnabled`: 是否显示 CPU 角标
- `StatusPosition`: `TopLeft / TopRight / BottomLeft / BottomRight`
- `StatusOffsetX/StatusOffsetY`: 角标边距
- `StatusRefreshMs`: 刷新间隔

## 遥测

CPU 遥测当前策略：

1. 优先尝试 `Control Center`
2. 失败时回退到 Windows 通用频率估算
3. 没有可信温度时显示 `--C`，不再伪装成真实温度

界面会展示：

- 数据来源
- 鲜度状态 `Fresh / Stale / Unavailable`
- 当前状态文本

## 测试与发布

运行测试：

```powershell
dotnet test .\AspenBurner.sln
```

发布：

```powershell
dotnet publish .\src\AspenBurner.App\AspenBurner.App.csproj -c Release -o .\dist\AspenBurner
```

当前自动化基线：

- MSTest `36/36` 通过
- 发布物 smoke 通过
- `opencli` 路径自审通过：`resume -> show-settings -> preview -> stop`

## 已知限制

- 目标游戏识别当前默认只包含 `DeltaForceClient-Win64-Shipping` 与 `delta_force_launcher`
- 独占全屏下，Windows 叠加层仍可能不可见，优先使用无边框或窗口化全屏
- 兼容脚本会提权，但 UAC 确认仍必须由桌面用户完成
