using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using KLPlugins.DynLeaderboards.Helpers;

namespace KLPlugins.DynLeaderboards.Settings.UI;

/// <summary>
///     Interaction logic for DynamicLeaderboardsTab.xaml
/// </summary>
public partial class DynamicLeaderboardsTab : UserControl {
    internal DynamicLeaderboardsTab(
        PluginSettings settings,
        DynLeaderboardsPlugin plugin,
        SettingsControl settingsControl
    ) {
        this.InitializeComponent();

        var vm = new DynamicLeaderboardsTabViewModel(settings, plugin, settingsControl);
        this.DataContext = vm;
    }

    private void ContainerDontScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
        // This allows to scroll while the mouse is over the container
        // From: https://stackoverflow.com/a/14368464 and https://stackoverflow.com/a/22341075
        this.SelectedLeaderboard_ScrollViewer.ScrollToVerticalOffset(
            this.SelectedLeaderboard_ScrollViewer.ContentVerticalOffset - e.Delta * 0.5
        );
    }
}

internal class DynamicLeaderboardsTabViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<LeaderboardComboBoxItem> _leaderboards { get; } = [];
    public ListCollectionView Leaderboards { get; }
    private LeaderboardComboBoxItem? _selectedLeaderboardListBoxItem;

    public LeaderboardComboBoxItem? SelectedLeaderboardListBoxItem {
        get => this._selectedLeaderboardListBoxItem;
        set => this.SetSelectedLeaderboardListBoxItem(value);
    }

    public SelectedLeaderboardViewModel? SelectedLeaderboardViewModel { get; private set; }
    public bool IsSelectedNull => this.SelectedLeaderboardListBoxItem == null;

    private readonly DynLeaderboardsPlugin _plugin;
    private readonly SettingsControl _settingsControl;
    private readonly PluginSettings _settings;

    public ICommand AddNewLeaderboardCommand { get; }

    #if DESIGN
    internal DynamicLeaderboardsTabViewModel() { }
    #endif

    public DynamicLeaderboardsTabViewModel(
        PluginSettings settings,
        DynLeaderboardsPlugin plugin,
        SettingsControl settingsControl
    ) {
        this._plugin = plugin;
        this._settingsControl = settingsControl;
        this._settings = settings;

        foreach (var cfg in settings.DynLeaderboardConfigs) {
            var vm = new LeaderboardComboBoxItemViewModel(cfg);
            this._leaderboards.Add(new LeaderboardComboBoxItem(vm));
        }

        this.SelectedLeaderboardListBoxItem = this._leaderboards?[0];

        this.AddNewLeaderboardCommand = new Command(this.AddNewLeaderboardWithDialog);

        this.Leaderboards = new ListCollectionView(this._leaderboards) {
            SortDescriptions = {
                new SortDescription(nameof(LeaderboardComboBoxItemViewModel.Name), ListSortDirection.Ascending),
            },
        };
    }

    private void SetSelectedLeaderboardListBoxItem(LeaderboardComboBoxItem? value) {
        if (value == null) {
            // Do nothing
        } else if (this.SelectedLeaderboardViewModel == null) {
            this.SelectedLeaderboardViewModel = new SelectedLeaderboardViewModel(
                value.ViewModel.Cfg,
                this._settings,
                this._settingsControl
            );
            this.SelectedLeaderboardViewModel.RemoveLeaderboard += this.RemoveLeaderboard;
            this.SelectedLeaderboardViewModel.DuplicateLeaderboard += this.DuplicateLeaderboard;
            this.SelectedLeaderboardViewModel.PropertyChanged += value.ViewModel.OnCfgChange;
        } else {
            if (this._selectedLeaderboardListBoxItem != null) {
                this.SelectedLeaderboardViewModel.PropertyChanged -=
                    this._selectedLeaderboardListBoxItem.ViewModel.OnCfgChange;
            }

            this.SelectedLeaderboardViewModel.SetCfg(value.ViewModel.Cfg);
            this.SelectedLeaderboardViewModel.PropertyChanged += value.ViewModel.OnCfgChange;
        }

        // Actually set the value last so that we can unsubscribe old events if necessary
        this._selectedLeaderboardListBoxItem = value;
        this.InvokePropertyChanged(nameof(this.SelectedLeaderboardListBoxItem));
        this.InvokePropertyChanged(nameof(this.IsSelectedNull));
    }

    private void RemoveLeaderboard(DynLeaderboardConfig cfg) {
        var index = this._leaderboards.FirstIndex(x => x.ViewModel.Cfg.Name == cfg.Name);
        if (index < 0) {
            var msg =
                $"Tried to remove leaderboard but could not find the specified leaderboard in the list. Leaderboard name: {cfg.Name}";
            Debug.Fail(msg);
            DynLeaderboardsPlugin.LogError(msg);
        }

        this._leaderboards.RemoveAt(index);
        this._plugin.RemoveLeaderboard(cfg);
    }

    private void AddNewLeaderboard(DynLeaderboardConfig newCfg) {
        var vm = new LeaderboardComboBoxItemViewModel(newCfg);
        var it = new LeaderboardComboBoxItem(vm);
        this._leaderboards.Add(it);
        this._plugin.AddNewLeaderboard(newCfg);
        this.SelectedLeaderboardListBoxItem = it;
    }

    private async void AddNewLeaderboardWithDialog() {
        var dialogWindow = new AskTextDialog(
            "Add new dynamic leaderboard",
            "Name",
            [new NewLeaderboardNameValidationRule(this._settings.DynLeaderboardConfigs)]
        );
        var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

        switch (res) {
            case System.Windows.Forms.DialogResult.OK:
                var name = dialogWindow.Text!;
                // AskTextDialog validates that the entered leaderboard name is valid new name and OK cannot be pressed before
                var newCfg = new DynLeaderboardConfig(name);
                this.AddNewLeaderboard(newCfg);
                break;
        }

        ;
    }

    private async void DuplicateLeaderboard(DynLeaderboardConfig cfg) {
        var dialogWindow = new AskTextDialog(
            $"Duplicate dynamic leaderboard '{cfg.Name}'",
            "Name",
            [new NewLeaderboardNameValidationRule(this._settings.DynLeaderboardConfigs)],
            cfg.Name
        );
        var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

        switch (res) {
            case System.Windows.Forms.DialogResult.OK:
                var name = dialogWindow.Text!;
                // AskTextDialog validates that the entered leaderboard name is valid new name and OK cannot be pressed before
                var newCfg = cfg.DeepClone();
                newCfg.Name = name;
                this.AddNewLeaderboard(newCfg);
                break;
        }

        ;
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class NewLeaderboardNameValidationRule : ValidationRule {
    private readonly List<DynLeaderboardConfig> _cfgs;

    internal NewLeaderboardNameValidationRule(List<DynLeaderboardConfig> cfgs) {
        this._cfgs = cfgs;
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
        if (value == null) {
            return new ValidationResult(false, "Dynamic leaderboard name cannot be null");
        }

        if (value is not string str) {
            return new ValidationResult(false, $"Dynamic leaderboard name cannot be of type {value.GetType()}");
        }

        if (str == "") {
            return new ValidationResult(false, "Dynamic leaderboard name cannot be empty");
        }

        if (this._cfgs.Contains(cfg => cfg.Name == str)) {
            return new ValidationResult(false, $"Dynamic leaderboard with name `{str}` already exists");
        }

        return ValidationResult.ValidResult;
    }
}

public class LeaderboardComboBoxItem : Control {
    internal LeaderboardComboBoxItemViewModel ViewModel { get; }

    internal LeaderboardComboBoxItem(LeaderboardComboBoxItemViewModel vm) {
        this.ViewModel = vm;
        this.DataContext = this.ViewModel;
    }
}

internal class LeaderboardComboBoxItemViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    internal readonly DynLeaderboardConfig Cfg;
    public string Name { get; set; }
    public bool IsEnabled { get; set; }

    #if DESIGN
    internal LeaderboardComboBoxItemViewModel() { }
    #endif

    internal LeaderboardComboBoxItemViewModel(DynLeaderboardConfig cfg) {
        this.Name = cfg.Name;
        this.IsEnabled = cfg.IsEnabled;
        this.Cfg = cfg;
    }

    internal void OnCfgChange(object sender, PropertyChangedEventArgs e) {
        this.Name = this.Cfg.Name;
        this.IsEnabled = this.Cfg.IsEnabled;

        this.InvokePropertyChanged(nameof(this.Name));
        this.InvokePropertyChanged(nameof(this.IsEnabled));
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class SelectedLeaderboardViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<DynLeaderboardConfig>? RemoveLeaderboard;
    public event Action<DynLeaderboardConfig>? DuplicateLeaderboard;

    public string Name { get; set; }
    public ObservableCollection<LeaderboardRotationItem> RotationItems { get; } = [];
    public int? SelectedRotationIndex { get; set; }
    public string ControlsNextLeaderboardActionName => $"DynLeaderboardsPlugin.{this._cfg.NextLeaderboardActionName}";

    public string ControlsPreviousLeaderboardActionName =>
        $"DynLeaderboardsPlugin.{this._cfg.PreviousLeaderboardActionName}";

    public ICommand SelectedRotationUpCommand { get; }
    public ICommand SelectedRotationDownCommand { get; }

    private readonly List<NumPosItemViewModel> _numPosItems = [];
    public ListCollectionView NumPosItems { get; }
    private List<PropertyViewModelBase> _exposedProperties { get; } = [];
    public ListCollectionView ExposedProperties { get; }

    public ICommand ExposedPropertiesEnableSelectedCommand { get; } =
        new SelectedPropertiesCommand(p => p.IsEnabled = true);

    public ICommand ExposedPropertiesDisableSelectedCommand { get; } =
        new SelectedPropertiesCommand(p => p.IsEnabled = false);

    public ICommand RenameCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand RemoveCommand { get; }

    private DynLeaderboardConfig _cfg { get; set; }
    private readonly PluginSettings _settings;
    private readonly SettingsControl _settingsControl;

    #if DESIGN
    public SelectedLeaderboardViewModel() { }
    #endif
    public SelectedLeaderboardViewModel(
        DynLeaderboardConfig cfg,
        PluginSettings settings,
        SettingsControl settingsControl
    ) {
        this._cfg = cfg;
        this.Name = cfg.Name;
        this._settings = settings;
        this._settingsControl = settingsControl;

        foreach (var l in this._cfg.Order) {
            var vm = new LeaderboardRotationItemViewModel(l);
            this.RotationItems.Add(new LeaderboardRotationItem(vm));
        }

        this.SelectedRotationUpCommand = new Command(this.MoveSelectedRotationUp);
        this.SelectedRotationDownCommand = new Command(this.MoveSelectedRotationDown);
        this.RemoveCommand = new Command(() => this.RemoveLeaderboard?.Invoke(this._cfg));
        this.DuplicateCommand = new Command(() => this.DuplicateLeaderboard?.Invoke(this._cfg));
        this.RenameCommand = new Command(this.Rename);

        this.AddNumPosItems();
        this.AddProperties();

        this.NumPosItems = new ListCollectionView(this._numPosItems) {
            GroupDescriptions = { new PropertyGroupDescription(nameof(NumPosItemViewModel.Group)) },
        };

        this.ExposedProperties = new ListCollectionView(this._exposedProperties) {
            GroupDescriptions = {
                new PropertyGroupDescription(nameof(PropertyViewModelBase.Group)),
                new PropertyGroupDescription(nameof(PropertyViewModelBase.SubGroup)),
            },
        };
    }

    private async void Rename() {
        var dialogWindow = new AskTextDialog(
            $"Rename dynamic leaderboard '{this.Name}'",
            "Name",
            [new NewLeaderboardNameValidationRule(this._settings.DynLeaderboardConfigs)],
            this._cfg.Name
        );
        var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

        switch (res) {
            case System.Windows.Forms.DialogResult.OK:
                var name = dialogWindow.Text!;
                // AskTextDialog validates that the entered leaderboard name is valid new name and OK cannot be pressed before
                this._cfg.Rename(name);
                this.Name = name;

                this.InvokePropertyChanged(nameof(this.Name));
                this.InvokePropertyChanged(nameof(this.ControlsNextLeaderboardActionName));
                this.InvokePropertyChanged(nameof(this.ControlsPreviousLeaderboardActionName));
                break;
        }

        ;
    }

    public void SetCfg(DynLeaderboardConfig cfg) {
        this._cfg = cfg;
        this.Name = cfg.Name;

        this.RotationItems.Clear();
        foreach (var l in this._cfg.Order) {
            var vm = new LeaderboardRotationItemViewModel(l);
            this.RotationItems.Add(new LeaderboardRotationItem(vm));
        }

        this.InvokePropertyChanged(nameof(this.Name));
        this.InvokePropertyChanged(nameof(this.ControlsNextLeaderboardActionName));
        this.InvokePropertyChanged(nameof(this.ControlsPreviousLeaderboardActionName));

        foreach (var p in this._numPosItems) {
            p.Update(p.PosItem.GetSetting(this._cfg));
        }

        foreach (var p in this._exposedProperties) {
            if (p is PropertyViewModel<OutCarProp> pcar) {
                pcar.UpdateSetting(this._cfg.OutCarProps);
            } else if (p is PropertyViewModel<OutPitProp> pPit) {
                pPit.UpdateSetting(this._cfg.OutPitProps);
            } else if (p is PropertyViewModel<OutLapProp> pLap) {
                pLap.UpdateSetting(this._cfg.OutLapProps);
            } else if (p is PropertyViewModel<OutStintProp> pSting) {
                pSting.UpdateSetting(this._cfg.OutStintProps);
            } else if (p is PropertyViewModel<OutGapProp> pGaps) {
                pGaps.UpdateSetting(this._cfg.OutGapProps);
            } else if (p is PropertyViewModel<OutPosProp> pPos) {
                pPos.UpdateSetting(this._cfg.OutPosProps);
            } else if (p is PropertyViewModel<OutDriverProp> pDriver) {
                pDriver.UpdateSetting(this._cfg.OutDriverProps);
            } else {
                var msg = $"Unknown property type {p.GetType()} in {this.Name}";
                Debug.Fail(msg);
                DynLeaderboardsPlugin.LogError(msg);
            }
        }
    }

    protected internal virtual void AddProps<T>(
        T[] order,
        Func<T, bool> isNotDef,
        Func<T, string> nameFunc,
        Func<T, string> descriptionFunc,
        OutPropsBase<T> setting,
        string group,
        string subGroup = ""
    )
        where T : struct {
        var a = order
            .Where(isNotDef)
            .Select(
                v => new PropertyViewModel<T>(nameFunc(v), descriptionFunc(v), v, setting) {
                    Group = group, SubGroup = subGroup,
                }
            );
        this._exposedProperties.AddRange(a);
    }

    internal void AddProperties() {
        this._exposedProperties.Clear();

        this.AddProps(
            OutCarPropExtensions.OrderCarInformation(),
            v => v != OutCarProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutCarProps,
            "Car info"
        );

        this.AddProps(
            OutPitPropExtensions.Order(),
            v => v != OutPitProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutPitProps,
            "Pit info"
        );

        this.AddProps(
            OutLapPropExtensions.Order(),
            v => v != OutLapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutLapProps,
            "Lap info"
        );

        this.AddProps(
            OutLapPropExtensions.OrderDeltaBestToBest(),
            v => v != OutLapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutLapProps,
            "Lap info",
            "Delta - best to best"
        );

        this.AddProps(
            OutLapPropExtensions.OrderDeltaLastToBest(),
            v => v != OutLapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutLapProps,
            "Lap info",
            "Delta - last to best"
        );

        this.AddProps(
            OutLapPropExtensions.OrderDeltaLastToLast(),
            v => v != OutLapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutLapProps,
            "Lap info",
            "Delta - last to last"
        );

        this.AddProps(
            OutLapPropExtensions.OrderDynamic(),
            v => v != OutLapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutLapProps,
            "Lap info",
            "Delta - dynamic"
        );

        this.AddProps(
            OutStintPropExtensions.Order(),
            v => v != OutStintProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutStintProps,
            "Stint info"
        );

        this.AddProps(
            OutGapPropExtensions.Order(),
            v => v != OutGapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutGapProps,
            "Gaps"
        );

        this.AddProps(
            OutGapPropExtensions.OrderDynamic(),
            v => v != OutGapProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutGapProps,
            "Gaps",
            "Dynamic"
        );

        this.AddProps(
            OutPosPropExtensions.Order(),
            v => v != OutPosProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutPosProps,
            "Positions"
        );

        this.AddProps(
            OutPosPropExtensions.OrderDynamic(),
            v => v != OutPosProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutPosProps,
            "Positions",
            "Dynamic"
        );

        this.AddProps(
            OutCarPropExtensions.OrderOther(),
            v => v != OutCarProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutCarProps,
            "Misc"
        );

        this.AddProps(
            OutDriverPropExtensions.Order(),
            v => v != OutDriverProp.NONE,
            v => v.ToPropName(),
            v => v.ToolTipText(),
            this._cfg.OutDriverProps,
            "Drivers"
        );
    }

    protected void AddNumPosItems() {
        this._numPosItems.Clear();

        var a = PosItemExt.Order().Select(v => new NumPosItemViewModel(v, v.GetSetting(this._cfg)));
        this._numPosItems.AddRange(a);
    }

    private void MoveSelectedRotationUp() {
        if (this.SelectedRotationIndex == null || this.SelectedRotationIndex <= 0) {
            return;
        }

        var selectedIndex = this.SelectedRotationIndex.Value;
        this.RotationItems.Move(selectedIndex, selectedIndex - 1);
        this._cfg.Order.MoveElementAt(selectedIndex, selectedIndex - 1);
    }

    private void MoveSelectedRotationDown() {
        if (this.SelectedRotationIndex == null
            || this.SelectedRotationIndex < 0
            || this.SelectedRotationIndex >= this.RotationItems.Count - 1
        ) {
            return;
        }

        var selectedIndex = this.SelectedRotationIndex.Value;
        this.RotationItems.Move(selectedIndex, selectedIndex + 1);
        this._cfg.Order.MoveElementAt(selectedIndex, selectedIndex + 1);
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class LeaderboardRotationItem : Control {
    internal LeaderboardRotationItemViewModel ViewModel { get; }

    internal LeaderboardRotationItem(LeaderboardRotationItemViewModel vm) {
        this.ViewModel = vm;
        this.DataContext = this.ViewModel;
    }
}

internal class LeaderboardRotationItemViewModel {
    public bool IsEnabled {
        get => this._leaderboard.IsEnabled;
        set => this._leaderboard.IsEnabled = value;
    }

    public string Name => this._leaderboard.Kind.ToString();

    public bool RemoveIfSingleClass {
        get => this._leaderboard.RemoveIfSingleClass;
        set => this._leaderboard.RemoveIfSingleClass = value;
    }

    public bool RemoveIfSingleCup {
        get => this._leaderboard.RemoveIfSingleCup;
        set => this._leaderboard.RemoveIfSingleCup = value;
    }

    private readonly Leaderboard _leaderboard;

    #if DESIGN
    internal LeaderboardRotationItemViewModel() { }
    #endif

    internal LeaderboardRotationItemViewModel(Leaderboard leaderboard) {
        this._leaderboard = leaderboard;
    }
}

internal enum PosItem {
    OVERALL,
    CLASS,
    CUP,
    OVERALL_RELATIVE,
    CLASS_RELATIVE,
    CUP_RELATIVE,
    RELATIVE_ON_TRACK,
    PARTIAL_RELATIVE_OVERALL_TOP,
    PARTIAL_RELATIVE_OVERALL_RELATIVE,
    PARTIAL_RELATIVE_CLASS_TOP,
    PARTIAL_RELATIVE_CLASS_RELATIVE,
    PARTIAL_RELATIVE_CUP_TOP,
    PARTIAL_RELATIVE_CUP_RELATIVE,
    DRIVERS,
}

internal static class PosItemExt {
    public static PosItem[] Order() {
        return [
            PosItem.OVERALL,
            PosItem.CLASS,
            PosItem.CUP,
            PosItem.OVERALL_RELATIVE,
            PosItem.CLASS_RELATIVE,
            PosItem.CUP_RELATIVE,
            PosItem.RELATIVE_ON_TRACK,
            PosItem.PARTIAL_RELATIVE_OVERALL_TOP,
            PosItem.PARTIAL_RELATIVE_OVERALL_RELATIVE,
            PosItem.PARTIAL_RELATIVE_CLASS_TOP,
            PosItem.PARTIAL_RELATIVE_CLASS_RELATIVE,
            PosItem.PARTIAL_RELATIVE_CUP_TOP,
            PosItem.PARTIAL_RELATIVE_CUP_RELATIVE,
            PosItem.DRIVERS,
        ];
    }

    public static string GroupName(this PosItem item) {
        return item switch {
            PosItem.OVERALL
                or PosItem.CLASS
                or PosItem.CUP => "Overall leaderboards",

            PosItem.OVERALL_RELATIVE
                or PosItem.CLASS_RELATIVE
                or PosItem.CUP_RELATIVE
                or PosItem.RELATIVE_ON_TRACK => "Relative leaderboards",

            PosItem.PARTIAL_RELATIVE_OVERALL_TOP
                or PosItem.PARTIAL_RELATIVE_OVERALL_RELATIVE
                or PosItem.PARTIAL_RELATIVE_CLASS_TOP
                or PosItem.PARTIAL_RELATIVE_CLASS_RELATIVE
                or PosItem.PARTIAL_RELATIVE_CUP_TOP
                or PosItem.PARTIAL_RELATIVE_CUP_RELATIVE => "Partial relative leaderboards",

            PosItem.DRIVERS => "Drivers",
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
        };
    }

    public static string Name(this PosItem item) {
        return item switch {
            PosItem.OVERALL or PosItem.OVERALL_RELATIVE => "Overall",
            PosItem.CLASS or PosItem.CLASS_RELATIVE => "Class",
            PosItem.CUP or PosItem.CUP_RELATIVE => "Cup",
            PosItem.RELATIVE_ON_TRACK => "On track",
            PosItem.PARTIAL_RELATIVE_OVERALL_TOP => "Overall - top positions",
            PosItem.PARTIAL_RELATIVE_OVERALL_RELATIVE => "Overall - relative positions",
            PosItem.PARTIAL_RELATIVE_CLASS_TOP => "Class    - top positions",
            PosItem.PARTIAL_RELATIVE_CLASS_RELATIVE => "Class    - relative positions",
            PosItem.PARTIAL_RELATIVE_CUP_TOP => "Cup      - top positions",
            PosItem.PARTIAL_RELATIVE_CUP_RELATIVE => "Cup      - relative positions",
            PosItem.DRIVERS => "Number of drivers",
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
        };
    }

    public static Box<int> GetSetting(this PosItem item, DynLeaderboardConfig cfg) {
        return item switch {
            PosItem.OVERALL => cfg.NumOverallPos,
            PosItem.CLASS => cfg.NumClassPos,
            PosItem.CUP => cfg.NumCupPos,
            PosItem.OVERALL_RELATIVE => cfg.NumOverallRelativePos,
            PosItem.CLASS_RELATIVE => cfg.NumClassRelativePos,
            PosItem.CUP_RELATIVE => cfg.NumCupRelativePos,
            PosItem.RELATIVE_ON_TRACK => cfg.NumOnTrackRelativePos,
            PosItem.PARTIAL_RELATIVE_OVERALL_TOP => cfg.PartialRelativeOverallNumOverallPos,
            PosItem.PARTIAL_RELATIVE_OVERALL_RELATIVE => cfg.PartialRelativeOverallNumRelativePos,
            PosItem.PARTIAL_RELATIVE_CLASS_TOP => cfg.PartialRelativeClassNumClassPos,
            PosItem.PARTIAL_RELATIVE_CLASS_RELATIVE => cfg.PartialRelativeClassNumRelativePos,
            PosItem.PARTIAL_RELATIVE_CUP_TOP => cfg.PartialRelativeCupNumCupPos,
            PosItem.PARTIAL_RELATIVE_CUP_RELATIVE => cfg.PartialRelativeCupNumRelativePos,
            PosItem.DRIVERS => cfg.NumDrivers,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
        };
    }
}

internal class NumPosItem : Control {
    internal NumPosItemViewModel ViewModel { get; }

    internal NumPosItem(NumPosItemViewModel vm) {
        this.ViewModel = vm;
        this.DataContext = this.ViewModel;
    }
}

internal class NumPosItemViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    internal PosItem PosItem { get; }
    public string Name { get; }
    public string Group { get; }

    public int Pos {
        get => this._setting.Value;
        set => this._setting.Value = value;
    }

    private Box<int> _setting;

    #if DESIGN
    internal NumPosItemViewModel() { }
    #endif

    internal NumPosItemViewModel(PosItem it, Box<int> setting) {
        this.PosItem = it;
        this._setting = setting;
        this.Name = this.PosItem.Name();
        this.Group = this.PosItem.GroupName();
    }

    internal void Update(Box<int> setting) {
        this._setting = setting;
        this.InvokePropertyChanged(nameof(this.Pos));
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#if DESIGN
internal class DesignDynamicLeaderboardTabViewModel : DynamicLeaderboardsTabViewModel {
    public new List<LeaderboardComboBoxItem> Leaderboards { get; set; } =
        DesignDynamicLeaderboardTabViewModel.CreateLeaderboards();

    public new LeaderboardComboBoxItem SelectedLeaderboardListBoxItem { get; set; } =
        DesignDynamicLeaderboardTabViewModel.CreateLeaderboards().First();

    public new SelectedLeaderboardViewModel SelectedLeaderboardViewModel { get; set; } =
        new DesignSelectedLeaderboardViewModel();

    public new bool IsSelectedNull { get; set; } = false;

    private static List<LeaderboardComboBoxItem> CreateLeaderboards() {
        var list = new List<LeaderboardComboBoxItem>();
        list.Add(new LeaderboardComboBoxItem(new DesignLeaderboardComboBoxItemViewModel()));
        list.Add(new LeaderboardComboBoxItem(new DesignLeaderboardComboBoxItemViewModel()));
        return list;
    }
}

internal class DesignSelectedLeaderboardViewModel : SelectedLeaderboardViewModel {
    public new string Name { get; set; } = "Dynamic";

    public new List<LeaderboardRotationItem> RotationItems { get; set; } =
        DesignSelectedLeaderboardViewModel.CreateRotation();

    public new LeaderboardRotationItem? SelectedRotationItem { get; set; } =
        DesignSelectedLeaderboardViewModel.CreateRotation().First();

    public new string ControlsNextLeaderboardActionName => "DynLeaderboardsPlugin.NextLeaderboard";
    public new string ControlsPreviousLeaderboardActionName => "DynLeaderboardsPlugin.PreviousLeaderboard";
    private List<PropertyViewModelBase> _exposedProperties { get; } = [];
    public new ListCollectionView ExposedProperties { get; }

    private readonly List<NumPosItemViewModel> _numPosItems = [];
    public new ListCollectionView NumPosItems { get; }

    public DesignSelectedLeaderboardViewModel() {
        this.SetCfg(new DynLeaderboardConfig());

        this.AddNumPosItems();
        this.AddProperties();

        this.NumPosItems = new ListCollectionView(this._numPosItems) {
            GroupDescriptions = { new PropertyGroupDescription(nameof(NumPosItemViewModel.Group)) },
        };

        this.ExposedProperties = new ListCollectionView(this._exposedProperties) {
            GroupDescriptions = {
                new PropertyGroupDescription(nameof(PropertyViewModelBase.Group)),
                new PropertyGroupDescription(nameof(PropertyViewModelBase.SubGroup)),
            },
        };
    }

    protected internal override void AddProps<T>(
        T[] order,
        Func<T, bool> isNotDef,
        Func<T, string> nameFunc,
        Func<T, string> descriptionFunc,
        OutPropsBase<T> setting,
        string group,
        string subGroup = ""
    )
        where T : struct {
        var random = new Random();
        var a = order
            .Where(isNotDef)
            .Select(
                v => new DesignPropertyViewModel<T> {
                    Name = nameFunc(v),
                    Description = descriptionFunc(v),
                    IsEnabled = random.Next(0, 2) == 0,
                    Group = group,
                    SubGroup = subGroup,
                }
            );
        this._exposedProperties.AddRange(a);
    }

    protected new void AddNumPosItems() {
        this._numPosItems.Clear();

        var a = PosItemExt.Order().Select(v => new NumPosItemViewModel(v, new Box<int>(5)));
        this._numPosItems.AddRange(a);
    }


    private static List<LeaderboardRotationItem> CreateRotation() {
        var list = new List<LeaderboardRotationItemViewModel>();
        list.Add(new DesignLeaderboardRotationItemViewModel { Name = "Overall" });
        list.Add(new DesignLeaderboardRotationItemViewModel { Name = "PartialRelativeOverall" });
        list.Add(new DesignLeaderboardRotationItemViewModel { Name = "RelativeOnTrack" });
        list.Add(new DesignLeaderboardRotationItemViewModel());
        return list.Select(c => new LeaderboardRotationItem(c)).ToList();
    }
}

internal class DesignLeaderboardComboBoxItemViewModel : LeaderboardComboBoxItemViewModel {
    public new string Name { get; set; } = "Dynamic";
    public new bool IsEnabled { get; set; } = true;
}

internal class DesignLeaderboardRotationItemViewModel : LeaderboardRotationItemViewModel {
    public new bool IsEnabled { get; set; } = true;
    public new string Name { get; set; } = "Dynamic";
    public new bool RemoveIfSingleClass { get; set; } = true;
    public new bool RemoveIfSingleCup { get; set; } = true;
}

internal class DesignNumPosItemViewModel : NumPosItemViewModel {
    public new string Name { get; } = "Overall";
    public new string Group { get; } = "Overall leaderboards";
    public new int Pos { get; set; } = 5;
}
#endif