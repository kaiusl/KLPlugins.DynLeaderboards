using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Helpers;

using SimHub.Plugins.UI;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    internal class Command(Action execute) : ICommand {
        private readonly Action _execute = execute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            this._execute();
        }
    }
    internal abstract class PropertyViewModelBase {
        public abstract string Name { get; }
        public abstract string Description { get; }
        internal bool IsRowSelected { get; set; } = false;
        public abstract bool IsEnabled { get; set; }
        public string Group { get; set; } = "";
    }

    internal class PropertyViewModel<T> : PropertyViewModelBase, INotifyPropertyChanged {
        private readonly T _prop;
        private IOutProps<T> _setting;

        public event PropertyChangedEventHandler? PropertyChanged;

        public override bool IsEnabled {
            get => this._setting.Includes(this._prop);
            set {
                if (value) {
                    this._setting.Combine(this._prop);
                } else {
                    this._setting.Remove(this._prop);
                };
                this.InvokePropertyChanged();
            }
        }

        private readonly string _name;
        public override string Name => this._name;

        private readonly string _description;
        public override string Description => this._description;

#if DESIGN
        internal PropertyViewModel() { }
#endif
        internal PropertyViewModel(string name, string tooltip, T prop, IOutProps<T> setting) {
            this._prop = prop;
            this._setting = setting;
            this._name = name;
            this._description = tooltip;
        }

        internal void UpdateSetting(IOutProps<T> setting) {
            this._setting = setting;
            this.InvokePropertyChanged(nameof(this.IsEnabled));
        }

        private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
            if (propertyName == null) {
                return;
            }

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

#if DESIGN
        internal class DesignPropertyViewModel<T> : PropertyViewModel<T> {
        public new bool IsEnabled { get; set; } = true;
        public new string Name { get; set; } = "Prop";
        public new string Description { get; set; } = "Desc";
    }
#endif

    public class DataGrid2 : DataGrid {
        public GroupStyle DefaultGroupStyle {
            get => (GroupStyle)this.GetValue(DefaultGroupStyleProperty);
            set => this.SetValue(DefaultGroupStyleProperty, value);
        }

        public static readonly DependencyProperty DefaultGroupStyleProperty =
            DependencyProperty.Register(
                "DefaultGroupStyle",
                typeof(GroupStyle),
                typeof(DataGrid2),
                new UIPropertyMetadata(null, DefaultGroupStyleChanged)
            );

        private static void DefaultGroupStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DataGrid2)o).SetDefaultGroupStyle(e.NewValue as GroupStyle);
        }

        private void SetDefaultGroupStyle(GroupStyle? defaultStyle) {
            if (defaultStyle == null) {
                return;
            }

            // Add style if user has not defined one explicitly as <DataGrid.GroupStyle>
            if (this.GroupStyle.Count == 0) {
                this.GroupStyle.Add(defaultStyle);
            }
        }
    }
}