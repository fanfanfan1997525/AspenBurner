# AspenBurner

版本: v0.4.3

一个面向 Windows 的轻量准心与状态角标工具，主要为《三角洲行动》这类没有中心准心的游戏提供屏幕中心十字准心，并在角落显示 CPU 频率与温度。

## 功能

- 屏幕中心小尺寸十字准心
- 支持自定义 `RGB`、长度、粗细、间距、透明度、偏移和四臂开关
- 支持独立 CPU 状态角标
- 优先读取 `Control Center` 的真实 `Clock / Temperature / Usage`
- 失败时回退到通用 Windows 频率估算与可用直连传感器
- 设置面板支持实时预览、滑块调节、数值输入和角标拖拽
- 仅在目标游戏窗口前台时显示，避免常驻桌面干扰

## 目录

- `CrosshairOverlay.ps1`: 主脚本
- `Configure-Crosshair.ps1`: 设置面板
- `Start-Crosshair.cmd`: 启动入口
- `Stop-Crosshair.cmd`: 停止入口
- `config/crosshair.json`: 配置文件
- `src/CrosshairOverlay.Core.psm1`: 核心逻辑
- `tests/tdd/crosshair_overlay/CrosshairOverlay.Tests.ps1`: Pester 测试

## 启动

直接双击：

- `Start-Crosshair.cmd`
- `Configure-Crosshair.cmd`
- `Stop-Crosshair.cmd`

也可以用命令行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Start-Crosshair.ps1
powershell -ExecutionPolicy Bypass -File .\Configure-Crosshair.ps1
powershell -ExecutionPolicy Bypass -File .\Stop-Crosshair.ps1
```

如果游戏以管理员权限运行，启动和停止脚本会走提权链路；桌面弹出 UAC 时需要手动点“是”。

## 配置

配置文件位于 `config/crosshair.json`。常用字段：

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

- `Color`: `Green` / `Yellow` / `Custom`
- `ColorR/G/B`: 自定义颜色通道
- `Length`: 准心单臂长度
- `Gap`: 中心间距
- `Thickness`: 线宽
- `OutlineThickness`: 黑边宽度
- `Opacity`: 透明度
- `OffsetX/OffsetY`: 准心相对中心偏移
- `ShowLeftArm/ShowRightArm/ShowTopArm/ShowBottomArm`: 四臂开关
- `StatusEnabled`: 是否显示状态角标
- `StatusPosition`: `TopLeft / TopRight / BottomLeft / BottomRight`

## 温度来源

当前版本优先使用笔记本官方 `Control Center` 的厂商链路读取 CPU 真实温度，而不是显示 `TZ` 热区近似值。

读取顺序：

1. `Control Center` vendor provider
2. 可用的直连硬件传感器
3. Windows 通用频率回退

因此，频率和温度可能会有瞬时波动，这是正常现象。

## 测试

运行测试：

```powershell
Invoke-Pester -Path .\tests\tdd\crosshair_overlay\CrosshairOverlay.Tests.ps1
```

当前基线：

- Pester `46/46` 通过

## 注意事项

- 当前目标进程默认包含 `DeltaForceClient-Win64-Shipping` 和 `delta_force_launcher`
- 准心与角标默认只在目标游戏前台显示
- 如果游戏使用独占全屏，Windows 叠加层可能不可见，优先使用无边框或窗口化全屏
- 如果修改的是脚本代码而不是纯配置，需要重启准心进程才能生效
