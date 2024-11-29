using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Common;

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

    internal ColorsTabSection(
        SettingsControl settingsControl,
        string label,
        TextBoxColors<K> colors,
        Menu menu,
        Grid colorsGrid
    ) {
        this._settingsControl = settingsControl;
        this.Label = label;
        this.Colors = colors;
        this._rows = [];
        this.Menu = menu;
        this.ColorsGrid = colorsGrid;
    }

    internal void Build(Func<K, bool> isDef) {
        this.BuildMenu(isDef);
        this.BuildItems(isDef);
    }

    private void BuildMenu(Func<K, bool> isDef) {
        var resetMenu = new ButtonMenuItem { Header = "Reset all" };
        this.Menu.Items.Add(resetMenu);

        resetMenu.Click += (_, _) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Reset();
                    }
                }
            );
        };

        var disableMenu = new ButtonMenuItem { Header = "Disable all" };
        this.Menu.Items.Add(disableMenu);

        disableMenu.Click += (_, _) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Disable();
                    }
                }
            );
        };

        var enableMenu = new ButtonMenuItem { Header = "Enable all" };
        this.Menu.Items.Add(enableMenu);
        enableMenu.Click += (_, _) => {
            this._settingsControl.DoOnConfirmation(
                () => {
                    foreach (var c in this._rows) {
                        c.Value.Enable();
                    }
                }
            );
        };

        void RefreshColors() {
            this.ColorsGrid.Children.Clear();
            this.ColorsGrid.RowDefinitions.Clear();

            this.BuildLabelRow();

            foreach (var (cls, i) in this.Colors.WithIndex()) {
                this.ColorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                if (!this._rows.TryGetValue(cls.Key, out var row)) {
                    row = this.CreateNewRow(cls.Key, cls.Value, isDef);
                    this._rows[cls.Key] = row;
                }

                row.AddToGrid(this.ColorsGrid, i + 1);
            }
        }

        var refreshBtn = new ButtonMenuItem {
            Header = "Refresh",
            ToolTip =
                "Refresh colors. This will check if new classes or categories have been added and will add them here for customization.",
        };
        refreshBtn.Click += (_, _) => RefreshColors();
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

        foreach (var (cls, i) in this.Colors.WithIndex()) {
            this.ColorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var row = this.CreateNewRow(cls.Key, cls.Value, isDef);
            this._rows[cls.Key] = row;
            row.AddToGrid(this.ColorsGrid, i + 1);
        }
    }

    private ColorRow CreateNewRow(K cls, OverridableTextBoxColor color, Func<K, bool> isDef) {
        var row = new ColorRow(cls, cls!.ToString(), color, this._settingsControl.FindResource);
        if (color.HasBase() || isDef(cls)) {
            row._RemoveButton.IsEnabled = false;
            row._RemoveButton.Opacity = SettingsControl.DISABLED_OPTION_OPACITY;
            row._RemoveButton.ToolTip = isDef(cls)
                ? "This category is the default and cannot be removed."
                : "This category has base data and cannot be removed.";
        } else {
            row._RemoveButton.Click += (_, _) => {
                this.Colors.Remove(row._Key);
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
        internal K _Key { get; }
        internal string _KeyAsString { get; }
        internal OverridableTextBoxColor _Color { get; }
        internal SHToggleButton _EnabledToggle { get; }
        internal Border _ClassBox { get; }
        internal TextBlock _ClassText { get; }
        internal ColorPicker _BgColorPicker { get; }
        internal ColorPicker _FgColorPicker { get; }
        internal SHButtonPrimary _RemoveButton { get; }
        internal SHButtonPrimary _ResetButton { get; }

        internal ColorRow(
            K key,
            string keyAsString,
            OverridableTextBoxColor color,
            Func<string, object> findResource
        ) {
            this._Key = key;
            this._KeyAsString = keyAsString;
            this._Color = color;

            var isEnabled = color.IsEnabled;
            var opacity = isEnabled ? 1.0 : SettingsControl.DISABLED_OPTION_OPACITY;

            this._EnabledToggle = new SHToggleButton {
                IsChecked = isEnabled, Style = (Style)findResource("ColorGrid_EnabledToggle"),
            };

            var currentBgColor =
                ColorTools.FromHex(color.BackgroundDontCheckEnabled() ?? TextBoxColor.DEF_BG);
            var currentFgColor =
                ColorTools.FromHex(color.ForegroundDontCheckEnabled() ?? TextBoxColor.DEF_FG);

            this._ClassBox = new Border {
                Background = new SolidColorBrush(currentBgColor),
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_LabelBorder"),
            };
            Grid.SetColumn(this._ClassBox, 1);

            this._ClassText = new TextBlock {
                Foreground = new SolidColorBrush(currentFgColor),
                Text = this._KeyAsString,
                Style = (Style)findResource("ColorGrid_LabelText"),
            };
            this._ClassBox.Child = this._ClassText;

            this._BgColorPicker = new ColorPicker {
                SelectedColor = currentBgColor,
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_ColorPicker"),
            };
            Grid.SetColumn(this._BgColorPicker, 2);
            this._BgColorPicker.SelectedColorChanged += (_, _) => {
                this._Color.SetBackground(this._BgColorPicker.SelectedColor.ToString());
                this._ClassBox.Background = new SolidColorBrush(this._BgColorPicker.SelectedColor.Value);
            };

            this._FgColorPicker = new ColorPicker {
                SelectedColor = currentFgColor,
                Opacity = opacity,
                IsEnabled = isEnabled,
                Style = (Style)findResource("ColorGrid_ColorPicker"),
            };
            Grid.SetColumn(this._FgColorPicker, 4);
            this._FgColorPicker.SelectedColorChanged += (_, _) => {
                this._Color.SetForeground(this._FgColorPicker.SelectedColor.ToString());
                this._ClassText.Foreground = new SolidColorBrush(this._FgColorPicker.SelectedColor.Value);
            };

            this._ResetButton = new SHButtonPrimary {
                Style = (Style)findResource("ColorGrid_RemoveButton"), Content = "Reset",
            };
            this._ResetButton.Click += (_, _) => { this.Reset(); };
            Grid.SetColumn(this._ResetButton, 6);

            this._RemoveButton = new SHButtonPrimary {
                Style = (Style)findResource("ColorGrid_RemoveButton"), Content = "Remove",
            };
            Grid.SetColumn(this._RemoveButton, 7);
            ToolTipService.SetShowOnDisabled(this._RemoveButton, true);

            this._EnabledToggle.Checked += (_, _) => {
                this._Color.Enable();
                this._ClassBox.IsEnabled = true;
                this._ClassBox.Opacity = 1.0;
                this._BgColorPicker.IsEnabled = true;
                this._BgColorPicker.Opacity = 1.0;
                this._FgColorPicker.IsEnabled = true;
                this._FgColorPicker.Opacity = 1.0;
            };

            this._EnabledToggle.Unchecked += (_, _) => {
                var opacity = SettingsControl.DISABLED_OPTION_OPACITY;
                this._Color.Disable();
                this._ClassBox.IsEnabled = false;
                this._ClassBox.Opacity = opacity;
                this._BgColorPicker.IsEnabled = false;
                this._BgColorPicker.Opacity = opacity;
                this._FgColorPicker.IsEnabled = false;
                this._FgColorPicker.Opacity = opacity;
            };
        }

        internal void AddToGrid(Grid grid, int row) {
            Grid.SetRow(this._EnabledToggle, row);
            Grid.SetRow(this._ClassBox, row);
            Grid.SetRow(this._BgColorPicker, row);
            Grid.SetRow(this._FgColorPicker, row);
            Grid.SetRow(this._RemoveButton, row);
            Grid.SetRow(this._ResetButton, row);

            grid.Children.Add(this._EnabledToggle);
            grid.Children.Add(this._ClassBox);
            grid.Children.Add(this._BgColorPicker);
            grid.Children.Add(this._FgColorPicker);
            grid.Children.Add(this._RemoveButton);
            grid.Children.Add(this._ResetButton);
        }

        internal void RemoveFromGrid(Grid grid) {
            grid.Children.Remove(this._EnabledToggle);
            grid.Children.Remove(this._ClassBox);
            grid.Children.Remove(this._BgColorPicker);
            grid.Children.Remove(this._FgColorPicker);
            grid.Children.Remove(this._RemoveButton);
            grid.Children.Remove(this._ResetButton);
        }

        internal void Reset() {
            this._FgColorPicker.SelectedColor =
                ColorTools.FromHex(this._Color.BaseForeground() ?? TextBoxColor.DEF_FG);
            this._BgColorPicker.SelectedColor =
                ColorTools.FromHex(this._Color.BaseBackground() ?? TextBoxColor.DEF_BG);
            this._Color.Reset();

            this._EnabledToggle.IsChecked = this._Color.IsEnabled;
        }

        internal void Disable() {
            this._EnabledToggle.IsChecked = false;
        }

        internal void Enable() {
            this._EnabledToggle.IsChecked = true;
        }
    }
}