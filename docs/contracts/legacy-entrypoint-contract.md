# 旧入口兼容契约

版本: v0.5.0  
日期: 2026-03-28

## 正式兼容入口

- `Start-Crosshair.cmd`
- `Start-Crosshair.ps1`
- `Configure-Crosshair.cmd`
- `Configure-Crosshair.ps1`
- `Stop-Crosshair.cmd`
- `Stop-Crosshair.ps1`

## 兼容要求

- 这些入口继续存在，不能无提示删除
- 它们的职责改为控制 `AspenBurner.exe`
- `Configure` 打开设置窗口
- `Stop` 请求主程序优雅退出
- 关闭设置窗口不等于退出程序

## 结果语义

- 成功时必须能把命令交给主程序
- 失败时必须有可感知反馈，不能继续沉默失败
