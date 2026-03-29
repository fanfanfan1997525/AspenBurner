# AspenBurner Windows 安装器与发布计划

版本: v0.6.0  
日期: 2026-03-29

## Problem Understanding

- What: 为 AspenBurner 增加 Windows 安装器、版本化发布脚本、tag 与远端 Release 资产。
- Why: 当前只有 publish 目录，没有标准安装路径和可复用发布链。
- Acceptance Criteria:
  - 能生成 installer exe 和 portable zip
  - 安装后兼容脚本可用
  - 代码推送到远端
  - 已打版本 tag
  - 已创建 GitHub Release 并附带资产

## Scope

- `src/AspenBurner.App/AspenBurner.App.csproj`
- `AspenBurner.Common.ps1`
- `README.md`
- `build/release/*`
- `tests/pester/*`
- `memory-bank/*`

## 实施步骤

1. 固化版本号与发布方案文档。
2. 先写发布辅助脚本测试，覆盖版本解析与产物命名。
3. 实现发布脚本与 Inno Setup 安装器脚本。
4. 修正兼容脚本对安装目录的支持。
5. 本机安装打包依赖并生成 release 资产。
6. 执行安装/启动/停止/卸载 smoke。
7. 更新 memory-bank。
8. 提交、推送、打 tag、创建 GitHub Release。

