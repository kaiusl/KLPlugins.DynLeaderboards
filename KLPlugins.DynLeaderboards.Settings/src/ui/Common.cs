using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using KLPlugins.DynLeaderboards.Common;

using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

using WoteverCommon.Extensions;

namespace KLPlugins.DynLeaderboards.Settings.UI;

internal class Command(Action execute) : ICommand {
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object parameter) {
        return true;
    }

    public void Execute(object parameter) {
        execute();
    }
}

internal class CommandAfterConfirmation(Action execute, SettingsControl settingsControl) : ICommand {
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object parameter) {
        return true;
    }

    public void Execute(object parameter) {
        settingsControl.DoOnConfirmation(execute);
    }
}

public class ControlsEditor2 : ControlsEditor {
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();

        if (this.GetTemplateChild("brd") is Border r) {
            r.Background = new SolidColorBrush(WindowsMediaColorExtensions.FromHex("#0bffffff"));
        }
    }
}

internal abstract class PropertyViewModelBase {
    public abstract string Name { get; }
    public abstract string Description { get; }
    internal bool IsRowSelected { get; set; } = false;
    public abstract bool IsEnabled { get; set; }
    public string Group { get; set; } = "";
    public string SubGroup { get; set; } = "";
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
            }

            this.InvokePropertyChanged();
        }
    }

    private readonly string _name;
    public override string Name => this._name;

    private readonly string _description;
    public override string Description => this._description;

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal PropertyViewModel() { }
    #pragma warning restore CS8618, CS9264
    #endif

    internal PropertyViewModel(string name, string tooltip, T prop, IOutProps<T> setting) {
        this._prop = prop;
        this._setting = setting;
        this._name = name;
        this._description = tooltip;
    }

    internal void UpdateSetting(IOutProps<T> setting) {
        this._setting = setting;
        this.InvokePropertyChanged(nameof(PropertyViewModel<T>.IsEnabled));
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

internal class SelectedPropertiesCommand(Action<PropertyViewModelBase> execute) : ICommand {
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object parameter) {
        return true;
    }

    public void Execute(object? parameter) {
        if (parameter is not System.Collections.IList selectedItems) {
            var msg = $"Expected the parameter to be `IList`. Got `{parameter?.GetType()}`";
            Debug.Fail(msg);
            Logging.LogError(msg);
            return;
        }

        foreach (var v in selectedItems) {
            if (v is not PropertyViewModelBase vm) {
                var msg = $"Expected the list element to be `PropertyViewModelBase`. Got `{v?.GetType()}`";
                Debug.Fail(msg);
                Logging.LogError(msg);
                continue;
            }

            execute(vm);
        }
    }
}

public class DataGrid2 : DataGrid {
    public GroupStyle DefaultGroupStyle {
        get => (GroupStyle)this.GetValue(DataGrid2.DefaultGroupStyleProperty);
        set => this.SetValue(DataGrid2.DefaultGroupStyleProperty, value);
    }

    public static readonly DependencyProperty DefaultGroupStyleProperty =
        DependencyProperty.Register(
            nameof(DataGrid2.DefaultGroupStyle),
            typeof(GroupStyle),
            typeof(DataGrid2),
            new UIPropertyMetadata(null, DataGrid2.DefaultGroupStyleChanged)
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

public class ListView2 : ListView {
    public IEnumerable<GroupStyle> DefaultGroupStyle {
        get => (List<GroupStyle>)this.GetValue(ListView2.DefaultGroupStyleProperty);
        set => this.SetValue(ListView2.DefaultGroupStyleProperty, value);
    }

    public static readonly DependencyProperty DefaultGroupStyleProperty =
        DependencyProperty.Register(
            nameof(ListView2.DefaultGroupStyle),
            typeof(IEnumerable<GroupStyle>),
            typeof(ListView2),
            new UIPropertyMetadata(null, ListView2.DefaultGroupStyleChanged)
        );

    private static void DefaultGroupStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
        ((ListView2)o).SetDefaultGroupStyle(e.NewValue as IEnumerable<GroupStyle>);
    }

    private void SetDefaultGroupStyle(IEnumerable<GroupStyle>? defaultStyle) {
        if (defaultStyle == null) {
            return;
        }

        // Add style if user has not defined one explicitly as <DataGrid.GroupStyle>
        if (this.GroupStyle.Count == 0) {
            this.GroupStyle.AddAll(defaultStyle);
        }
    }
}

internal class AskTextDialog : SHDialogContentBase {
    public string? Text { get; set; }

    internal AskTextDialog(
        string title,
        string? textBoxLabel,
        IEnumerable<ValidationRule>? validationRules = null,
        string? defaultText = null
    ) {
        this.ShowOk = true;
        this.ShowCancel = true;

        var sp = new StackPanel();
        this.Content = sp;

        sp.Children.Add(new SHSectionTitle { Text = title, Margin = new Thickness(0, 0, 0, 25) });

        var textSp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(textSp);

        var label = new TextBlock {
            Text = textBoxLabel ?? "",
            Padding = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        Grid.SetColumn(label, 0);
        Grid.SetRow(label, 0);
        textSp.Children.Add(label);

        var tb = new TextBox();
        Grid.SetColumn(tb, 1);
        Grid.SetRow(tb, 0);
        textSp.Children.Add(tb);

        var textBinding = new Binding("Text") {
            Mode = BindingMode.TwoWay,
            Source = this,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            NotifyOnValidationError = true,
        };
        if (validationRules != null) {
            foreach (var rule in validationRules) {
                textBinding.ValidationRules.Add(rule);
            }
        }

        tb.SetBinding(TextBox.TextProperty, textBinding);
        tb.Text = defaultText ?? ""; // force validation
        this.IsOkEnabled = false;

        Validation.AddErrorHandler(
            tb,
            (_, e) => {
                if (e.Action == ValidationErrorEventAction.Added) {
                    this.IsOkEnabled = false;
                } else if (e.Action == ValidationErrorEventAction.Removed) {
                    this.IsOkEnabled = true;
                }
            }
        );
    }
}

/// <summary>
///     This expects the DataContext property to be set to an instance of ClassPreviewViewModel
/// </summary>
public class ClassPreview : Control { }

internal class ClassPreviewViewModel : INotifyPropertyChanged {
    private readonly OverridableClassInfo.Manager _manager;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClassName => this._manager.Key.AsString();
    public bool IsColorEnabled => this._manager.IsColorEnabled;
    public string Background => this._manager.Background ?? OverridableTextBoxColor.DEF_BG;
    public string Foreground => this._manager.Foreground ?? OverridableTextBoxColor.DEF_FG;

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal ClassPreviewViewModel() { }
    #pragma warning restore CS8618, CS9264
    #endif

    internal ClassPreviewViewModel(OverridableClassInfo.Manager manager) {
        this._manager = manager;
        this._manager.PropertyChanged += this.OnManagerPropertyChanged;
    }

    internal void Unsubscribe() {
        this._manager.PropertyChanged -= this.OnManagerPropertyChanged;
    }

    private void OnManagerPropertyChanged(object sender, PropertyChangedEventArgs e) {
        this.PropertyChanged?.Invoke(this, e); // property names are same, can just forward
    }
}

#if DESIGN

internal class DesignClassPreviewViewModel : ClassPreviewViewModel {
    public new string ClassName { get; set; } = "Test";
    public new bool IsColorEnabled { get; set; } = true;
    public new string Background { get; set; } = "black";
    public new string Foreground { get; set; } = "white";
}

#endif