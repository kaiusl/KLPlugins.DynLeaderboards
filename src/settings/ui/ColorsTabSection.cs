using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Helpers;

using SimHub.Plugins.Styles;

using Xceed.Wpf.Toolkit;

namespace KLPlugins.DynLeaderboards.Settings.UI;

internal class ColorsTabSection<K> {
    public Menu Menu { get; }
    public Grid ColorsGrid { get; }
    public string Label { get; }
    public TextBoxColors<K> Colors { get; }

    private readonly Dictionary<K, ColorRow> _rows;

    private readonly SettingsControl _settingsControl;
    private readonly DynLeaderboardsPlugin _plugin;
    private readonly Action _updateInfos;

    internal ColorsTabSection(
        SettingsControl settingsControl,
        DynLeaderboardsPlugin plugin,
        string label,
        TextBoxColors<K> colors,
        Menu menu,
        Grid colorsGrid,
        Action updateInfos
    ) {
        this._settingsControl = settingsControl;
        this._plugin = plugin;
        this.Label = label;
        this.Colors = colors;
        this._rows = [];
        this.Menu = menu;
        this.ColorsGrid = colorsGrid;
        this._updateInfos = updateInfos;
    }

    internal void Build(Func<K, bool> isDef) {
        this.BuildMenu(isDef);
        this.BuildItems(isDef);
    }

    private void BuildMenu(Func<K, bool> isDef) {
        var resetMenu = new ButtonMenuItem { Header = "Reset", ShowDropDown = true };
        this.Menu.Items.Add(resetMenu);

        var resetMenuResetAll = new MenuItem { Header = "Reset all" };

        resetMenu.Items.Add(resetMenuResetAll);
        resetMenuResetAll.Click += (sender, e) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Reset();
                    }

                    this._updateInfos();
                }
            );
        };

        var resetMenuResetNames = new MenuItem { Header = "Reset all foreground" };
        resetMenu.Items.Add(resetMenuResetNames);
        resetMenuResetNames.Click += (sender, e) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.ResetForeground();
                    }

                    this._updateInfos();
                }
            );
        };

        var resetMenuResetClasses = new MenuItem { Header = "Reset all backgrounds" };
        resetMenu.Items.Add(resetMenuResetClasses);
        resetMenuResetClasses.Click += (sender, e) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.ResetBackground();
                    }

                    this._updateInfos();
                }
            );
        };

        var disableMenu = new ButtonMenuItem { Header = "Disable all" };
        this.Menu.Items.Add(disableMenu);

        disableMenu.Click += (sender, e) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Disable();
                    }

                    this._updateInfos();
                }
            );
        };

        var enableMenu = new ButtonMenuItem { Header = "Enable all" };
        this.Menu.Items.Add(enableMenu);
        enableMenu.Click += (sender, e) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Enable();
                    }

                    this._updateInfos();
                }
            );
        };

        void RefreshColors() {
            this.ColorsGrid.Children.Clear();
            this.ColorsGrid.RowDefinitions.Clear();

            this.BuildLabelRow();

            foreach (var (cls, i) in this.Colors.WithIndex() ?? []) {
                this.ColorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                ColorRow row;
                if (!this._rows.ContainsKey(cls.Key)) {
                    row = this.CreateNewRow(cls.Key, cls.Value, isDef);
                    this._rows[cls.Key] = row;
                } else {
                    row = this._rows[cls.Key];
                }

                row.AddToGrid(this.ColorsGrid, i + 1);
            }
        }

        var refreshBtn = new ButtonMenuItem { Header = "Refresh" };
        refreshBtn.ToolTip =
            "Refresh colors. This will check if new classes or categories have been added and will add them here for customization.";
        refreshBtn.Click += (sender, e) => RefreshColors();
        this.Menu.Items.Add(refreshBtn);
    }

    private void BuildItems(Func<K, bool> isDef) {
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto), MinWidth = 75 }
        );
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.ColorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        this.BuildLabelRow();

        foreach (var (cls, i) in this.Colors.WithIndex() ?? []) {
            this.ColorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var row = this.CreateNewRow(cls.Key, cls.Value, isDef);
            this._rows[cls.Key] = row;
            row.AddToGrid(this.ColorsGrid, i + 1);
        }
    }

    private ColorRow CreateNewRow(K cls, OverridableTextBoxColor color, Func<K, bool> isDef) {
        var row = new ColorRow(cls, cls!.ToString(), color, this._settingsControl.FindResource, this._updateInfos);
        if (color.HasBase() || isDef(cls)) {
            row.RemoveButton.IsEnabled = false;
            row.RemoveButton.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
            if (isDef(cls)) {
                row.RemoveButton.ToolTip = "This category is the default and cannot be removed.";
            } else {
                row.RemoveButton.ToolTip = "This category has base data and cannot be removed.";
            }
        } else {
            row.RemoveButton.Click += (sender, e) => {
                this.Colors.Remove(row.Key);
                row.RemoveFromGrid(this.ColorsGrid);
            };
        }

        return row;
    }

    private void BuildLabelRow() {
        this.ColorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        TextBlock CreateLabel(string label, int col) {
            var t = new TextBlock {
                Text = label, Style = (Style)this._settingsControl.FindResource("ColorGrid_ColumnLabel"),
            };
            Grid.SetColumn(t, col);

            return t;
        }

        this.ColorsGrid.Children.Add(CreateLabel("Enabled", 0));
        this.ColorsGrid.Children.Add(CreateLabel(this.Label, 1));
        this.ColorsGrid.Children.Add(CreateLabel("Background", 2));
        this.ColorsGrid.Children.Add(CreateLabel("Foreground", 4));
    }

    private class ColorRow {
        internal K Key { get; }
        internal string KeyAsString { get; }
        internal OverridableTextBoxColor Color { get; }
        internal SHToggleButton EnabledToggle { get; }
        internal Border ClassBox { get; }
        internal TextBlock ClassText { get; }
        internal ColorPicker BgColorPicker { get; }
        internal SHButtonSecondary BgResetButton { get; }
        internal ColorPicker FgColorPicker { get; }
        internal SHButtonSecondary FgResetButton { get; }
        internal SHButtonPrimary RemoveButton { get; }
        internal SHButtonPrimary ResetButton { get; }

        internal ColorRow(
            K key,
            string keyAsString,
            OverridableTextBoxColor color,
            Func<string, object> findResource,
            Action updateInfos
        ) {
            this.Key = key;
            this.KeyAsString = keyAsString;
            this.Color = color;

            var isEnabled = color.IsEnabled;
            var opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;

            this.EnabledToggle = new SHToggleButton {
                IsChecked = isEnabled, Style = (Style)findResource("ColorGrid_EnabledToggle"),
            };

            var currentBgColor =
                WindowsMediaColorExtensions.FromHex(
                    color.BackgroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_BG
                );
            var currentFgColor =
                WindowsMediaColorExtensions.FromHex(
                    color.ForegroundDontCheckEnabled() ?? OverridableTextBoxColor.DEF_FG
                );

            this.ClassBox = new Border {
                Background = new SolidColorBrush(currentBgColor),
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_LabelBorder"),
            };
            Grid.SetColumn(this.ClassBox, 1);

            this.ClassText = new TextBlock {
                Foreground = new SolidColorBrush(currentFgColor),
                Text = this.KeyAsString,
                Style = (Style)findResource("ColorGrid_LabelText"),
            };
            this.ClassBox.Child = this.ClassText;

            this.BgColorPicker = new ColorPicker {
                SelectedColor = currentBgColor,
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_ColorPicker"),
            };
            Grid.SetColumn(this.BgColorPicker, 2);
            this.BgColorPicker.SelectedColorChanged += (sender, e) => {
                this.Color.SetBackground(this.BgColorPicker.SelectedColor.ToString());
                this.ClassBox.Background = new SolidColorBrush(this.BgColorPicker.SelectedColor.Value);
                updateInfos();
            };

            this.BgResetButton = new SHButtonSecondary {
                Opacity = opacity, IsEnabled = isEnabled, Style = (Style)findResource("ColorGrid_ResetButton"),
            };
            Grid.SetColumn(this.BgResetButton, 3);
            this.BgResetButton.Click += (sender, e) => {
                this.ResetBackground();
                updateInfos();
            };

            this.FgColorPicker = new ColorPicker {
                SelectedColor = currentFgColor,
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_ColorPicker"),
            };
            Grid.SetColumn(this.FgColorPicker, 4);
            this.FgColorPicker.SelectedColorChanged += (sender, e) => {
                this.Color.SetForeground(this.FgColorPicker.SelectedColor.ToString());
                this.ClassText.Foreground = new SolidColorBrush(this.FgColorPicker.SelectedColor.Value);
                updateInfos();
            };

            this.FgResetButton = new SHButtonSecondary {
                Opacity = opacity, IsEnabled = isEnabled, Style = (Style)findResource("ColorGrid_ResetButton"),
            };
            Grid.SetColumn(this.FgResetButton, 5);
            this.FgResetButton.Click += (sender, e) => {
                this.ResetForeground();
                updateInfos();
            };

            this.ResetButton = new SHButtonPrimary {
                Style = (Style)findResource("ColorGrid_RemoveButton"), Content = "Reset",
            };
            this.ResetButton.Click += (_, _) => {
                this.Reset();
                updateInfos();
            };
            Grid.SetColumn(this.ResetButton, 6);

            this.RemoveButton = new SHButtonPrimary { Style = (Style)findResource("ColorGrid_RemoveButton") };
            Grid.SetColumn(this.RemoveButton, 7);
            ToolTipService.SetShowOnDisabled(this.RemoveButton, true);

            this.EnabledToggle.Checked += (sender, e) => {
                this.Color.Enable();
                this.ClassBox.IsEnabled = true;
                this.ClassBox.Opacity = 1.0;
                this.BgColorPicker.IsEnabled = true;
                this.BgColorPicker.Opacity = 1.0;
                this.BgResetButton.IsEnabled = true;
                this.BgResetButton.Opacity = 1.0;
                this.FgColorPicker.IsEnabled = true;
                this.FgColorPicker.Opacity = 1.0;
                this.FgResetButton.IsEnabled = true;
                this.FgResetButton.Opacity = 1.0;
                updateInfos();
            };

            this.EnabledToggle.Unchecked += (sender, e) => {
                var opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this.Color.Disable();
                this.ClassBox.IsEnabled = false;
                this.ClassBox.Opacity = opacity;
                this.BgColorPicker.IsEnabled = false;
                this.BgColorPicker.Opacity = opacity;
                this.BgResetButton.IsEnabled = false;
                this.BgResetButton.Opacity = opacity;
                this.FgColorPicker.IsEnabled = false;
                this.FgColorPicker.Opacity = opacity;
                this.FgResetButton.IsEnabled = false;
                this.FgResetButton.Opacity = opacity;
                updateInfos();
            };
        }

        internal void AddToGrid(Grid grid, int row) {
            Grid.SetRow(this.EnabledToggle, row);
            Grid.SetRow(this.ClassBox, row);
            Grid.SetRow(this.BgColorPicker, row);
            Grid.SetRow(this.BgResetButton, row);
            Grid.SetRow(this.FgColorPicker, row);
            Grid.SetRow(this.FgResetButton, row);
            Grid.SetRow(this.RemoveButton, row);
            Grid.SetRow(this.ResetButton, row);

            grid.Children.Add(this.EnabledToggle);
            grid.Children.Add(this.ClassBox);
            grid.Children.Add(this.BgColorPicker);
            grid.Children.Add(this.BgResetButton);
            grid.Children.Add(this.FgColorPicker);
            grid.Children.Add(this.FgResetButton);
            grid.Children.Add(this.RemoveButton);
            grid.Children.Add(this.ResetButton);
        }

        internal void RemoveFromGrid(Grid grid) {
            grid.Children.Remove(this.EnabledToggle);
            grid.Children.Remove(this.ClassBox);
            grid.Children.Remove(this.BgColorPicker);
            grid.Children.Remove(this.BgResetButton);
            grid.Children.Remove(this.FgColorPicker);
            grid.Children.Remove(this.FgResetButton);
            grid.Children.Remove(this.RemoveButton);
            grid.Children.Remove(this.ResetButton);
        }

        internal void Reset() {
            this.ResetForeground();
            this.ResetBackground();
            this.Color.Reset();

            this.EnabledToggle.IsChecked = this.Color.IsEnabled;
        }

        internal void Disable() {
            this.EnabledToggle.IsChecked = false;
        }

        internal void Enable() {
            this.EnabledToggle.IsChecked = true;
        }

        internal void ResetForeground() {
            this.FgColorPicker.SelectedColor =
                WindowsMediaColorExtensions.FromHex(this.Color.BaseForeground() ?? OverridableTextBoxColor.DEF_FG);
            this.Color.ResetForeground();
        }

        internal void ResetBackground() {
            this.BgColorPicker.SelectedColor =
                WindowsMediaColorExtensions.FromHex(this.Color.BaseBackground() ?? OverridableTextBoxColor.DEF_BG);
            this.Color.ResetBackground();
        }
    }
}