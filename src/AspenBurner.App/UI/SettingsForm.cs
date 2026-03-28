using AspenBurner.App.Configuration;
using AspenBurner.App.Core;
using AspenBurner.App.Diagnostics;
using AspenBurner.App.Runtime;

namespace AspenBurner.App.UI;

/// <summary>
/// Interactive settings window with live preview and runtime health visibility.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly PreviewCanvas previewCanvas;
    private readonly Label lifecycleLabel;
    private readonly Label targetLabel;
    private readonly Label telemetrySourceLabel;
    private readonly Label telemetryFreshnessLabel;
    private readonly Label liveStatusLabel;
    private readonly Label feedbackLabel;
    private readonly Label presetDescriptionLabel;
    private readonly ComboBox colorComboBox;
    private readonly ComboBox presetComboBox;
    private readonly ComboBox statusPositionComboBox;
    private readonly ComboBox statusColorComboBox;
    private readonly SliderRow colorRRow;
    private readonly SliderRow colorGRow;
    private readonly SliderRow colorBRow;
    private readonly SliderRow lengthRow;
    private readonly SliderRow gapRow;
    private readonly SliderRow thicknessRow;
    private readonly SliderRow outlineRow;
    private readonly SliderRow opacityRow;
    private readonly SliderRow offsetXRow;
    private readonly SliderRow offsetYRow;
    private readonly SliderRow statusOpacityRow;
    private readonly SliderRow statusFontSizeRow;
    private readonly NumericRow statusOffsetXRow;
    private readonly NumericRow statusOffsetYRow;
    private readonly NumericRow statusRefreshRow;
    private readonly CheckBox showLeftCheckBox;
    private readonly CheckBox showRightCheckBox;
    private readonly CheckBox showTopCheckBox;
    private readonly CheckBox showBottomCheckBox;
    private readonly CheckBox statusEnabledCheckBox;
    private readonly CheckBox statusTemperatureCheckBox;
    private bool suppressUpdates;
    private CrosshairConfig currentConfig = new();

    /// <summary>
    /// Initializes a new settings form.
    /// </summary>
    public SettingsForm()
    {
        this.Text = "AspenBurner 设置";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1120, 760);
        this.Size = new Size(1180, 820);

        SplitContainer splitContainer = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 560,
        };

        Panel previewHost = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
        };

        Label previewTitle = new()
        {
            Text = "实时预览",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
        };

        Label previewHint = new()
        {
            Text = "拖动右上角 CPU 文本可调整锚点和边距。左侧预览与真实准心共享同一套参数。",
            Dock = DockStyle.Top,
            Height = 40,
        };

        this.previewCanvas = new PreviewCanvas
        {
            Dock = DockStyle.Fill,
            Config = this.currentConfig,
        };
        this.previewCanvas.StatusPlacementChanged += this.OnStatusPlacementChanged;
        previewHost.Controls.Add(this.previewCanvas);
        previewHost.Controls.Add(previewHint);
        previewHost.Controls.Add(previewTitle);

        FlowLayoutPanel rightFlow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10),
        };

        GroupBox runtimeGroup = new()
        {
            Text = "运行状态",
            Width = 520,
            Height = 170,
        };
        TableLayoutPanel runtimeTable = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10),
        };
        runtimeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        runtimeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        this.lifecycleLabel = AddStatusRow(runtimeTable, 0, "生命周期");
        this.targetLabel = AddStatusRow(runtimeTable, 1, "目标窗口");
        this.telemetrySourceLabel = AddStatusRow(runtimeTable, 2, "遥测来源");
        this.telemetryFreshnessLabel = AddStatusRow(runtimeTable, 3, "数据鲜度");
        this.liveStatusLabel = AddStatusRow(runtimeTable, 4, "当前读数");
        runtimeGroup.Controls.Add(runtimeTable);
        rightFlow.Controls.Add(runtimeGroup);

        GroupBox crosshairGroup = new()
        {
            Text = "准心",
            Width = 520,
            Height = 500,
        };
        FlowLayoutPanel crosshairFlow = CreateGroupFlow();

        this.colorComboBox = CreateComboBox(["Green", "Yellow", "Custom"], value => this.UpdateConfig(this.currentConfig with { Color = value }));
        crosshairFlow.Controls.Add(CreateLabeledRow("颜色模式", this.colorComboBox));
        this.colorRRow = CreateSliderRow("R", 0, 255, value => this.UpdateConfig((this.currentConfig with { Color = "Custom", ColorR = value })));
        this.colorGRow = CreateSliderRow("G", 0, 255, value => this.UpdateConfig((this.currentConfig with { Color = "Custom", ColorG = value })));
        this.colorBRow = CreateSliderRow("B", 0, 255, value => this.UpdateConfig((this.currentConfig with { Color = "Custom", ColorB = value })));
        this.lengthRow = CreateSliderRow("长度", 2, 20, value => this.UpdateConfig(this.currentConfig with { Length = value }));
        this.gapRow = CreateSliderRow("间距", 1, 20, value => this.UpdateConfig(this.currentConfig with { Gap = value }));
        this.thicknessRow = CreateSliderRow("粗细", 1, 6, value => this.UpdateConfig(this.currentConfig with { Thickness = value }));
        this.outlineRow = CreateSliderRow("描边", 0, 4, value => this.UpdateConfig(this.currentConfig with { OutlineThickness = value }));
        this.opacityRow = CreateSliderRow("透明度", 64, 255, value => this.UpdateConfig(this.currentConfig with { Opacity = value }));
        this.offsetXRow = CreateSliderRow("中心偏移 X", -200, 200, value => this.UpdateConfig(this.currentConfig with { OffsetX = value }));
        this.offsetYRow = CreateSliderRow("中心偏移 Y", -200, 200, value => this.UpdateConfig(this.currentConfig with { OffsetY = value }));
        AddSliderRows(crosshairFlow, this.colorRRow, this.colorGRow, this.colorBRow, this.lengthRow, this.gapRow, this.thicknessRow, this.outlineRow, this.opacityRow, this.offsetXRow, this.offsetYRow);

        CheckBox showLeftCheckBox = null!;
        CheckBox showRightCheckBox = null!;
        CheckBox showTopCheckBox = null!;
        CheckBox showBottomCheckBox = null!;
        showLeftCheckBox = CreateCheckBox("显示左臂", value => this.UpdateArmConfig(nameof(CrosshairConfig.ShowLeftArm), value, showLeftCheckBox));
        showRightCheckBox = CreateCheckBox("显示右臂", value => this.UpdateArmConfig(nameof(CrosshairConfig.ShowRightArm), value, showRightCheckBox));
        showTopCheckBox = CreateCheckBox("显示上臂", value => this.UpdateArmConfig(nameof(CrosshairConfig.ShowTopArm), value, showTopCheckBox));
        showBottomCheckBox = CreateCheckBox("显示下臂", value => this.UpdateArmConfig(nameof(CrosshairConfig.ShowBottomArm), value, showBottomCheckBox));
        this.showLeftCheckBox = showLeftCheckBox;
        this.showRightCheckBox = showRightCheckBox;
        this.showTopCheckBox = showTopCheckBox;
        this.showBottomCheckBox = showBottomCheckBox;
        crosshairFlow.Controls.Add(this.showLeftCheckBox);
        crosshairFlow.Controls.Add(this.showRightCheckBox);
        crosshairFlow.Controls.Add(this.showTopCheckBox);
        crosshairFlow.Controls.Add(this.showBottomCheckBox);

        crosshairGroup.Controls.Add(crosshairFlow);
        rightFlow.Controls.Add(crosshairGroup);

        GroupBox statusGroup = new()
        {
            Text = "CPU 角标",
            Width = 520,
            Height = 360,
        };
        FlowLayoutPanel statusFlow = CreateGroupFlow();
        this.statusEnabledCheckBox = CreateCheckBox("启用 CPU 角标", value => this.UpdateConfig(this.currentConfig with { StatusEnabled = value }));
        this.statusTemperatureCheckBox = CreateCheckBox("显示温度", value => this.UpdateConfig(this.currentConfig with { StatusShowTemperature = value }));
        statusFlow.Controls.Add(this.statusEnabledCheckBox);
        statusFlow.Controls.Add(this.statusTemperatureCheckBox);
        this.statusPositionComboBox = CreateComboBox(["TopLeft", "TopRight", "BottomLeft", "BottomRight"], value => this.UpdateConfig(this.currentConfig with { StatusPosition = value }));
        this.statusColorComboBox = CreateComboBox(["Yellow", "Green", "White"], value => this.UpdateConfig(this.currentConfig with { StatusTextColor = value }));
        statusFlow.Controls.Add(CreateLabeledRow("锚点", this.statusPositionComboBox));
        this.statusOffsetXRow = CreateNumericRow("边距 X", 0, 500, value => this.UpdateConfig(this.currentConfig with { StatusOffsetX = value }));
        this.statusOffsetYRow = CreateNumericRow("边距 Y", 0, 500, value => this.UpdateConfig(this.currentConfig with { StatusOffsetY = value }));
        this.statusRefreshRow = CreateNumericRow("刷新间隔", 500, 5000, value => this.UpdateConfig(this.currentConfig with { StatusRefreshMs = value }));
        statusFlow.Controls.Add(this.statusOffsetXRow.Panel);
        statusFlow.Controls.Add(this.statusOffsetYRow.Panel);
        statusFlow.Controls.Add(this.statusRefreshRow.Panel);
        statusFlow.Controls.Add(CreateLabeledRow("文字颜色", this.statusColorComboBox));
        this.statusOpacityRow = CreateSliderRow("文字透明度", 64, 255, value => this.UpdateConfig(this.currentConfig with { StatusOpacity = value }));
        this.statusFontSizeRow = CreateSliderRow("字号", 9, 24, value => this.UpdateConfig(this.currentConfig with { StatusFontSize = value }));
        AddSliderRows(statusFlow, this.statusOpacityRow, this.statusFontSizeRow);
        statusGroup.Controls.Add(statusFlow);
        rightFlow.Controls.Add(statusGroup);

        GroupBox actionsGroup = new()
        {
            Text = "动作",
            Width = 520,
            Height = 182,
        };
        FlowLayoutPanel buttonFlow = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
        };
        this.presetComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            DisplayMember = nameof(CrosshairPresetDefinition.DisplayName),
        };
        this.presetComboBox.SelectedIndexChanged += (_, _) => this.UpdatePresetDescription();
        this.presetDescriptionLabel = new Label
        {
            AutoSize = false,
            Width = 360,
            Height = 38,
            Location = new Point(96, 38),
            ForeColor = Color.DimGray,
        };
        foreach (CrosshairPresetDefinition preset in CrosshairPresetCatalog.GetRecommendations())
        {
            this.presetComboBox.Items.Add(preset);
        }

        Panel presetRow = new()
        {
            Width = 470,
            Height = 76,
            Margin = new Padding(0, 0, 0, 8),
        };
        Label presetLabel = new()
        {
            Text = "推荐参数",
            Location = new Point(0, 8),
            Width = 90,
        };
        this.presetComboBox.Location = new Point(96, 4);
        Button applyPresetButton = CreateButton("应用预设", (_, _) => this.ApplySelectedPreset());
        applyPresetButton.Location = new Point(328, 0);
        presetRow.Controls.Add(presetLabel);
        presetRow.Controls.Add(this.presetComboBox);
        presetRow.Controls.Add(applyPresetButton);
        presetRow.Controls.Add(this.presetDescriptionLabel);
        if (this.presetComboBox.Items.Count > 0)
        {
            this.presetComboBox.SelectedIndex = 0;
        }
        Button saveButton = CreateButton("保存配置", (_, _) => this.SaveRequested?.Invoke(this, EventArgs.Empty));
        Button resetButton = CreateButton("恢复默认", (_, _) => this.ResetToDefaults());
        Button previewButton = CreateButton("桌面预览 8 秒", (_, _) => this.PreviewRequested?.Invoke(this, EventArgs.Empty));
        Button exitButton = CreateButton("退出程序", (_, _) => this.ExitRequested?.Invoke(this, EventArgs.Empty));
        buttonFlow.Controls.Add(presetRow);
        buttonFlow.Controls.Add(saveButton);
        buttonFlow.Controls.Add(resetButton);
        buttonFlow.Controls.Add(previewButton);
        buttonFlow.Controls.Add(exitButton);
        actionsGroup.Controls.Add(buttonFlow);
        rightFlow.Controls.Add(actionsGroup);

        this.feedbackLabel = new Label
        {
            AutoSize = false,
            Width = 520,
            Height = 28,
            Text = "尚未应用更改。",
            ForeColor = Color.DimGray,
            Margin = new Padding(6),
        };
        rightFlow.Controls.Add(this.feedbackLabel);

        splitContainer.Panel1.Controls.Add(previewHost);
        splitContainer.Panel2.Controls.Add(rightFlow);
        this.Controls.Add(splitContainer);

        this.SetConfig(this.currentConfig);
        this.UpdatePresetDescription();
    }

    /// <summary>
    /// Raised when the edited configuration changes.
    /// </summary>
    public event EventHandler<CrosshairConfig>? ConfigEdited;

    /// <summary>
    /// Raised when the user explicitly requests an immediate save.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Raised when desktop preview should start.
    /// </summary>
    public event EventHandler? PreviewRequested;

    /// <summary>
    /// Raised when the user requests full application exit.
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Gets or sets whether the form may really close instead of minimizing to tray semantics.
    /// </summary>
    public bool AllowExit { get; set; }

    /// <summary>
    /// Replaces the currently edited configuration.
    /// </summary>
    public void SetConfig(CrosshairConfig config)
    {
        this.suppressUpdates = true;
        try
        {
            this.currentConfig = config;
            this.previewCanvas.Config = config;

            this.colorComboBox.SelectedItem = config.Color;
            this.colorRRow.SetValue(config.ColorR);
            this.colorGRow.SetValue(config.ColorG);
            this.colorBRow.SetValue(config.ColorB);
            this.lengthRow.SetValue(config.Length);
            this.gapRow.SetValue(config.Gap);
            this.thicknessRow.SetValue(config.Thickness);
            this.outlineRow.SetValue(config.OutlineThickness);
            this.opacityRow.SetValue(config.Opacity);
            this.offsetXRow.SetValue(config.OffsetX);
            this.offsetYRow.SetValue(config.OffsetY);
            this.showLeftCheckBox.Checked = config.ShowLeftArm;
            this.showRightCheckBox.Checked = config.ShowRightArm;
            this.showTopCheckBox.Checked = config.ShowTopArm;
            this.showBottomCheckBox.Checked = config.ShowBottomArm;
            this.statusEnabledCheckBox.Checked = config.StatusEnabled;
            this.statusTemperatureCheckBox.Checked = config.StatusShowTemperature;
            this.statusPositionComboBox.SelectedItem = config.StatusPosition;
            this.statusColorComboBox.SelectedItem = config.StatusTextColor;
            this.statusOffsetXRow.SetValue(config.StatusOffsetX);
            this.statusOffsetYRow.SetValue(config.StatusOffsetY);
            this.statusRefreshRow.SetValue(config.StatusRefreshMs);
            this.statusOpacityRow.SetValue(config.StatusOpacity);
            this.statusFontSizeRow.SetValue(config.StatusFontSize);
        }
        finally
        {
            this.suppressUpdates = false;
        }

        this.UpdateControlStates();
        this.previewCanvas.Invalidate();
    }

    /// <summary>
    /// Updates the runtime health labels.
    /// </summary>
    public void UpdateHealth(HealthSnapshot snapshot)
    {
        this.lifecycleLabel.Text = snapshot.Presence.Lifecycle switch
        {
            AppLifecycleState.Paused => "已暂停",
            AppLifecycleState.Stopped => "已停止",
            _ => "运行中",
        };
        this.targetLabel.Text = snapshot.Presence.Target switch
        {
            TargetWindowState.TargetMatched => "目标窗口已命中",
            TargetWindowState.DesktopPreview => $"桌面预览中（剩余 {snapshot.Presence.PreviewSecondsRemaining}s）",
            _ => "等待目标窗口",
        };
        this.telemetrySourceLabel.Text = snapshot.TelemetrySource;
        this.telemetryFreshnessLabel.Text = snapshot.Presence.Telemetry switch
        {
            TelemetryFreshnessState.Fresh => "Fresh",
            TelemetryFreshnessState.Stale => "Stale",
            _ => "Unavailable",
        };
        this.liveStatusLabel.Text = string.IsNullOrWhiteSpace(snapshot.LastStatusText) ? "--" : snapshot.LastStatusText;
    }

    /// <summary>
    /// Updates the footer feedback text.
    /// </summary>
    public void SetFeedback(string message, bool isError = false)
    {
        this.feedbackLabel.Text = message;
        this.feedbackLabel.ForeColor = isError ? Color.Firebrick : Color.SeaGreen;
    }

    /// <inheritdoc />
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!this.AllowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    private static Label AddStatusRow(TableLayoutPanel table, int rowIndex, string title)
    {
        Label titleLabel = new()
        {
            Text = title,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        };

        Label valueLabel = new()
        {
            Text = "--",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        };

        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        table.Controls.Add(titleLabel, 0, rowIndex);
        table.Controls.Add(valueLabel, 1, rowIndex);
        return valueLabel;
    }

    private static FlowLayoutPanel CreateGroupFlow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
    }

    private static void AddSliderRows(FlowLayoutPanel host, params SliderRow[] rows)
    {
        foreach (SliderRow row in rows)
        {
            host.Controls.Add(row.Panel);
        }
    }

    private static Button CreateButton(string text, EventHandler onClick)
    {
        Button button = new()
        {
            Text = text,
            Width = 110,
            Height = 34,
        };
        button.Click += onClick;
        return button;
    }

    private ComboBox CreateComboBox(string[] items, Action<string> onChanged)
    {
        ComboBox comboBox = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180,
        };
        comboBox.Items.AddRange(items);
        comboBox.SelectedIndexChanged += (_, _) =>
        {
            if (!this.suppressUpdates && comboBox.SelectedItem is string value)
            {
                onChanged(value);
            }
        };
        return comboBox;
    }

    private Panel CreateLabeledRow(string labelText, Control control)
    {
        Panel panel = new()
        {
            Width = 470,
            Height = 42,
            Margin = new Padding(6),
        };
        Label label = new()
        {
            Text = labelText,
            Location = new Point(0, 10),
            Width = 120,
        };
        control.Location = new Point(128, 6);
        panel.Controls.Add(label);
        panel.Controls.Add(control);
        return panel;
    }

    private SliderRow CreateSliderRow(string labelText, int minimum, int maximum, Action<int> onChanged)
    {
        Panel panel = new()
        {
            Width = 470,
            Height = 58,
            Margin = new Padding(6),
        };

        Label label = new()
        {
            Text = labelText,
            Location = new Point(0, 6),
            Width = 120,
        };

        TrackBar trackBar = new()
        {
            Minimum = minimum,
            Maximum = maximum,
            TickStyle = TickStyle.None,
            Width = 210,
            Location = new Point(128, 20),
        };

        NumericUpDown numeric = new()
        {
            Minimum = minimum,
            Maximum = maximum,
            Width = 78,
            Location = new Point(350, 20),
            TextAlign = HorizontalAlignment.Center,
        };

        trackBar.ValueChanged += (_, _) =>
        {
            if (this.suppressUpdates)
            {
                return;
            }

            if ((int)numeric.Value != trackBar.Value)
            {
                numeric.Value = trackBar.Value;
            }

            onChanged(trackBar.Value);
        };

        numeric.ValueChanged += (_, _) =>
        {
            if (this.suppressUpdates)
            {
                return;
            }

            int value = (int)numeric.Value;
            if (trackBar.Value != value)
            {
                trackBar.Value = value;
                return;
            }

            onChanged(value);
        };

        panel.Controls.Add(label);
        panel.Controls.Add(trackBar);
        panel.Controls.Add(numeric);
        return new SliderRow(panel, label, trackBar, numeric);
    }

    private NumericRow CreateNumericRow(string labelText, int minimum, int maximum, Action<int> onChanged)
    {
        Panel panel = new()
        {
            Width = 470,
            Height = 42,
            Margin = new Padding(6),
        };

        Label label = new()
        {
            Text = labelText,
            Location = new Point(0, 10),
            Width = 120,
        };

        NumericUpDown numeric = new()
        {
            Minimum = minimum,
            Maximum = maximum,
            Width = 90,
            Location = new Point(128, 6),
            TextAlign = HorizontalAlignment.Center,
        };
        numeric.ValueChanged += (_, _) =>
        {
            if (!this.suppressUpdates)
            {
                onChanged((int)numeric.Value);
            }
        };

        panel.Controls.Add(label);
        panel.Controls.Add(numeric);
        return new NumericRow(panel, label, numeric);
    }

    private CheckBox CreateCheckBox(string text, Action<bool> onChanged)
    {
        CheckBox checkBox = new()
        {
            Text = text,
            Width = 300,
            Height = 28,
            Margin = new Padding(6),
        };
        checkBox.CheckedChanged += (_, _) =>
        {
            if (!this.suppressUpdates)
            {
                onChanged(checkBox.Checked);
            }
        };
        return checkBox;
    }

    private void UpdateArmConfig(string propertyName, bool enabled, CheckBox sourceCheckBox)
    {
        CrosshairConfig nextConfig = propertyName switch
        {
            nameof(CrosshairConfig.ShowLeftArm) => this.currentConfig with { ShowLeftArm = enabled },
            nameof(CrosshairConfig.ShowRightArm) => this.currentConfig with { ShowRightArm = enabled },
            nameof(CrosshairConfig.ShowTopArm) => this.currentConfig with { ShowTopArm = enabled },
            nameof(CrosshairConfig.ShowBottomArm) => this.currentConfig with { ShowBottomArm = enabled },
            _ => this.currentConfig,
        };

        if (!nextConfig.ShowLeftArm && !nextConfig.ShowRightArm && !nextConfig.ShowTopArm && !nextConfig.ShowBottomArm)
        {
            this.suppressUpdates = true;
            sourceCheckBox.Checked = true;
            this.suppressUpdates = false;
            this.SetFeedback("至少需要保留一个准心臂。", isError: true);
            return;
        }

        this.UpdateConfig(nextConfig);
    }

    private void UpdateConfig(CrosshairConfig config)
    {
        if (this.suppressUpdates)
        {
            return;
        }

        this.SetConfig(config);
        this.SetFeedback("已应用到实际准心，等待自动保存。");
        this.ConfigEdited?.Invoke(this, config);
    }

    private void UpdateControlStates()
    {
        bool customColor = string.Equals(this.currentConfig.Color, "Custom", StringComparison.Ordinal);
        this.colorRRow.SetEnabled(customColor);
        this.colorGRow.SetEnabled(customColor);
        this.colorBRow.SetEnabled(customColor);

        bool statusEnabled = this.currentConfig.StatusEnabled;
        this.statusPositionComboBox.Enabled = statusEnabled;
        this.statusColorComboBox.Enabled = statusEnabled;
        this.statusOffsetXRow.SetEnabled(statusEnabled);
        this.statusOffsetYRow.SetEnabled(statusEnabled);
        this.statusRefreshRow.SetEnabled(statusEnabled);
        this.statusOpacityRow.SetEnabled(statusEnabled);
        this.statusFontSizeRow.SetEnabled(statusEnabled);
        this.statusTemperatureCheckBox.Enabled = statusEnabled;
    }

    private void OnStatusPlacementChanged(object? sender, StatusPlacement placement)
    {
        this.SetConfig(this.currentConfig with
        {
            StatusPosition = placement.Position,
            StatusOffsetX = placement.OffsetX,
            StatusOffsetY = placement.OffsetY,
        });
        this.ConfigEdited?.Invoke(this, this.currentConfig);
        this.SetFeedback("角标位置已更新。");
    }

    private void ApplySelectedPreset()
    {
        if (this.presetComboBox.SelectedItem is not CrosshairPresetDefinition preset)
        {
            return;
        }

        this.UpdateConfig(CrosshairPresetCatalog.Apply(preset.Id, this.currentConfig));
        this.SetFeedback($"已应用推荐参数：{preset.DisplayName}。");
    }

    private void ResetToDefaults()
    {
        this.UpdateConfig(CrosshairPresetCatalog.Reset());
        this.SetFeedback("已恢复默认参数。");
    }

    private void UpdatePresetDescription()
    {
        this.presetDescriptionLabel.Text = this.presetComboBox.SelectedItem is CrosshairPresetDefinition preset
            ? preset.Description
            : "选择推荐参数后可一键套用。";
    }

    private readonly record struct SliderRow(Panel Panel, Label Label, TrackBar TrackBar, NumericUpDown Numeric)
    {
        public void SetValue(int value)
        {
            this.TrackBar.Value = Math.Clamp(value, this.TrackBar.Minimum, this.TrackBar.Maximum);
            this.Numeric.Value = this.TrackBar.Value;
        }

        public void SetEnabled(bool enabled)
        {
            this.Label.Enabled = enabled;
            this.TrackBar.Enabled = enabled;
            this.Numeric.Enabled = enabled;
        }
    }

    private readonly record struct NumericRow(Panel Panel, Label Label, NumericUpDown Numeric)
    {
        public void SetValue(int value)
        {
            this.Numeric.Value = Math.Clamp(value, (int)this.Numeric.Minimum, (int)this.Numeric.Maximum);
        }

        public void SetEnabled(bool enabled)
        {
            this.Label.Enabled = enabled;
            this.Numeric.Enabled = enabled;
        }
    }
}
