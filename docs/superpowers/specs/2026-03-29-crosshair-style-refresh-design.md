# AspenBurner 样式刷新修复设计

版本: v0.6.2  
日期: 2026-03-29

## 1. 问题

当前设置面板修改 `颜色 / 透明度 / 文字样式` 这类非几何参数时，预览会变，但真实 overlay 有时不跟着变。  
用户已经确认：`长度 / 间距 / 粗细` 这类几何参数基本正常，主要是样式类参数不生效。

## 2. 根因判断

问题不在配置链路本身：

- `SettingsForm -> ConfigEdited -> ApplicationContext -> OverlayRuntime.UpdateConfig(...)` 已经接通。
- `CrosshairOverlayForm.ApplyConfig(...)` 和 `StatusOverlayForm.ApplyStatus(...)` 也都能收到最新值。

更像是可视刷新语义不够强：

- 当前两只透明顶层窗体只调用 `Invalidate()`。
- 对这类 click-through layered overlay，`Invalidate()` 不保证“立即重绘”。
- 所以会出现内部状态已经更新，但用户眼前没有立刻变化的情况。

## 3. 方案比较

### 方案 A：只在 runtime 层重复推配置

- 优点：看起来改动少。
- 缺点：状态已经推到了窗体，重复推只是掩盖问题。
- 结论：不采用。

### 方案 B：在窗体层把“样式变化”升级成同步重绘

- 优点：修的就是根因；范围最小；不改配置结构。
- 缺点：需要补两类 UI 回归测试。
- 结论：采用。

### 方案 C：重构整个 overlay view-state

- 优点：长期最干净。
- 缺点：为这个 bug 过重。
- 结论：当前不值得。

## 4. 最终设计

### 4.1 CrosshairOverlayForm

- `ApplyConfig(...)` 仍更新内部 config 和颜色。
- 如果窗体当前可见，则对非几何样式变化执行更强的即时重绘，而不只 `Invalidate()`。

覆盖的样式项：

- `Color`
- `ColorR / G / B`
- `Opacity`
- `Thickness`
- `OutlineThickness`

### 4.2 StatusOverlayForm

- `ApplyStatus(...)` 仍比较文本/字号/颜色差异。
- 只要样式有变化且窗体可见，就立即重绘，不等下一轮消息泵。

覆盖的样式项：

- `StatusTextColor`
- `StatusOpacity`
- `StatusFontSize`
- 文本本身

## 5. 测试策略

必须补两条失败测试：

1. 可见的 `CrosshairOverlayForm` 在只改颜色/透明度时，`ApplyConfig(...)` 会立即触发重绘。
2. 可见的 `StatusOverlayForm` 在文本不变、只改颜色/透明度时，`ApplyStatus(...)` 会立即触发重绘。

## 6. 验收标准

- 真实准心修改颜色/透明度后立即变化。
- CPU 角标修改颜色/透明度/字号后立即变化。
- 不影响现有几何参数更新逻辑。
- 全量测试继续通过。
