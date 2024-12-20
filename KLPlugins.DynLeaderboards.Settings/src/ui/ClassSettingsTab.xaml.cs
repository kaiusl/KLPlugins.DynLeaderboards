﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;

using Control = System.Windows.Controls.Control;
using UserControl = System.Windows.Controls.UserControl;

#if DESIGN
using System.Collections.Generic;
#endif

namespace KLPlugins.DynLeaderboards.Settings.UI;

public partial class ClassSettingsTab : UserControl {
    private ClassSettingsTabViewModel _viewModel { get; }

    internal ClassSettingsTab(SettingsControl settingsControl, ClassInfos.Manager classesManager) {
        this.InitializeComponent();

        this._viewModel = new ClassSettingsTabViewModel(settingsControl, classesManager);
        this.DataContext = this._viewModel;

        this._viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(this._viewModel.SelectedClass)) {
                var selectedClass = this._viewModel.SelectedClass;
                if (selectedClass != null) {
                    this.Classes_ListBox.ScrollIntoView(selectedClass);
                }
            }
        };
    }
}

internal class ClassSettingsTabViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ObservableCollection<ClassListBoxItem> _classesListBoxItems = [];

    private ClassListBoxItem? _selectedClass;

    public ClassListBoxItem? SelectedClass {
        get => this._selectedClass;
        set {
            this._selectedClass = value;
            this.SelectedClassViewModel = value == null
                ? null
                : new SelectedClassViewModel(
                    this._classesManager.Get(value._ViewModel.Class)!,
                    this._classesManager,
                    this._settingsControl,
                    this.AllClassesView
                );
            this.InvokePropertyChanged();
            this.InvokePropertyChanged(nameof(ClassSettingsTabViewModel.IsSelectedNull));
        }
    }

    private SelectedClassViewModel? _selectedClassViewModel;

    public SelectedClassViewModel? SelectedClassViewModel {
        get => this._selectedClassViewModel!;
        private set {
            this._selectedClassViewModel = value;
            this.InvokePropertyChanged();
        }
    }

    public bool IsSelectedNull => this.SelectedClass == null;

    public ListCollectionView ClassesListCollectionView { get; }
    public ListCollectionView AllClassesView { get; }

    private ClassInfos.Manager _classesManager { get; }

    private readonly SettingsControl _settingsControl;

    public ICommand MenuResetAllCommand { get; }
    public ICommand MenuResetAllColorsCommand { get; }
    public ICommand MenuResetAllShortNameCommand { get; }
    public ICommand MenuResetAllReplaceWithCommand { get; }

    public ICommand MenuDisableAllCommand { get; }
    public ICommand MenuDisableAllColorsCommand { get; }
    public ICommand MenuDisableAllReplaceWithCommand { get; }

    public ICommand MenuEnableAllCommand { get; }
    public ICommand MenuEnableAllColorsCommand { get; }
    public ICommand MenuEnableAllReplaceWithCommand { get; }

    public ICommand MenuAddNewClassCommand { get; }
    public ICommand MenuRefreshCommand { get; }

    #if DESIGN
    #pragma warning disable CS8618
    // Used by DesignClassSettingsTabViewModel as base constructor so that we can have design time view model
    // CS8618: Non-nullable field must contain a non-null value when exiting constructor.
    //         It's ok since they will never be used
    internal ClassSettingsTabViewModel() { }
    #pragma warning restore CS8618
    #endif

    internal ClassSettingsTabViewModel(
        SettingsControl settingsControl,
        ClassInfos.Manager classesManager
    ) {
        this._settingsControl = settingsControl;
        this._classesManager = classesManager;

        this.AllClassesView = new ListCollectionView(settingsControl._AllClasses) {
            IsLiveSorting = true, CustomSort = new CaseInsensitiveComparer(CultureInfo.InvariantCulture),
        };

        this.ClassesListCollectionView = new ListCollectionView(this._classesListBoxItems) {
            IsLiveSorting = true, CustomSort = new ClassListBoxItemViewModel.KeyComparer(),
        };

        foreach (var item in this._classesManager) {
            this._classesListBoxItems.Add(
                new ClassListBoxItem(new ClassListBoxItemViewModel(item.Value, this, this._classesManager))
            );
        }

        this._classesManager.CollectionChanged += (_, e) => {
            if (e.NewItems != null) {
                ClassListBoxItem? last = null;
                foreach (OverridableClassInfo.Manager item in e.NewItems) {
                    this._classesListBoxItems.Add(
                        new ClassListBoxItem(new ClassListBoxItemViewModel(item, this, this._classesManager))
                    );
                }

                if (last != null) {
                    this.SelectedClass = last;
                }
            }

            if (e.OldItems != null) {
                foreach (OverridableClassInfo.Manager item in e.OldItems) {
                    this._classesListBoxItems.Remove(
                        this._classesListBoxItems.FirstOrDefault(x => x._ViewModel.Class == item._Key)
                    );
                }
            }
        };

        this.UpdateReplaceWiths(); // this needs to be done after all list boxes have been added

        this.SelectedClass = (ClassListBoxItem?)this.ClassesListCollectionView.CurrentItem
            ?? this._classesListBoxItems.FirstOrDefault();

        CommandAfterConfirmation AllClassesCommand(Action<OverridableClassInfo.Manager> action) {
            return new CommandAfterConfirmation(
                () => {
                    foreach (var item in this._classesManager) {
                        action(item.Value);
                    }
                },
                this._settingsControl
            );
        }

        this.MenuResetAllCommand = AllClassesCommand(c => c.Reset());
        this.MenuResetAllColorsCommand = AllClassesCommand(c => c.ResetColors());
        this.MenuResetAllShortNameCommand = AllClassesCommand(c => c.ResetShortName());
        this.MenuResetAllReplaceWithCommand = AllClassesCommand(c => c.ResetReplaceWith());

        this.MenuDisableAllCommand = AllClassesCommand(c => c.DisableAll());
        this.MenuDisableAllColorsCommand = AllClassesCommand(c => c._IsColorEnabled = false);
        this.MenuDisableAllReplaceWithCommand = AllClassesCommand(c => c._IsReplaceWithEnabled = false);

        this.MenuEnableAllCommand = AllClassesCommand(c => c.EnableAll());
        this.MenuEnableAllColorsCommand = AllClassesCommand(c => c._IsColorEnabled = true);
        this.MenuEnableAllReplaceWithCommand = AllClassesCommand(c => c._IsReplaceWithEnabled = true);

        this.MenuAddNewClassCommand = new Command(this.AddNewClass);
        this.MenuRefreshCommand = new Command(() => { this._classesManager.Update(); });
    }

    private void InvokePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void UpdateReplaceWiths() {
        foreach (var item in this._classesListBoxItems) {
            item._ViewModel.UpdateReplaceWith();
        }
    }

    private async void AddNewClass() {
        try {
            var dialogWindow = new ChooseNewClassNameDialog("Add new class", this._classesManager);
            var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (res) {
                case DialogResult.OK:
                    Logging.LogInfo($"Add new class `{dialogWindow.Text}`");
                    var clsName = dialogWindow.Text;
                    // ChooseNewClassNameDialog validates that the entered class name is valid new name and OK cannot be pressed before
                    var cls = new CarClass(clsName!);
                    this._classesManager.TryAdd(cls);
                    break;
                default:
                    break;
            }
        } catch (Exception e) {
            Logging.LogError($"Failed to add a new class: {e}");
        }
    }
}

internal class SelectedClassViewModel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    private OverridableClassInfo.Manager _classManager { get; }

    public CarClass Class => this._classManager._Key;

    public bool IsColorEnabled {
        get => this._classManager._IsColorEnabled;
        set => this._classManager._IsColorEnabled = value;
    }

    public string Background {
        get => this._classManager._Background ?? TextBoxColor.DEF_BG;
        set => this._classManager._Background = value;
    }

    public string Foreground {
        get => this._classManager._Foreground ?? TextBoxColor.DEF_FG;
        set => this._classManager._Foreground = value;
    }

    public string ShortName {
        get => this._classManager._ShortName;
        set => this._classManager._ShortName = value;
    }

    public bool IsReplaceWithEnabled {
        get => this._classManager._IsReplaceWithEnabled;
        set => this._classManager._IsReplaceWithEnabled = value;
    }

    public CarClass ReplaceWith {
        get => this._classManager._ReplaceWith ?? CarClass.Default;
        set {
            this._settingsControl.TryAddCarClass(value);
            this._classManager._ReplaceWith = value;
        }
    }

    public bool CanBeRemoved =>
        this._classesManager.CanBeRemoved(this.Class)
        && !this._settingsControl._Settings.Infos.CarInfos.ContainsClass(this.Class);

    public ListCollectionView AllClassesView { get; }

    public ICommand ResetColorsCommand { get; }
    public ICommand ResetShortNameCommand { get; }
    public ICommand ResetReplaceWithCommand { get; }
    public ICommand ResetAllCommand { get; }
    public ICommand DisableAllCommand { get; }
    public ICommand RemoveClassCommand { get; }
    public ICommand DuplicateClassCommand { get; }

    private readonly SettingsControl _settingsControl;
    private readonly ClassInfos.Manager _classesManager;

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal SelectedClassViewModel() { }
    #pragma warning restore CS8618, CS9264
    #endif

    internal SelectedClassViewModel(
        OverridableClassInfo.Manager manager,
        ClassInfos.Manager classesManager,
        SettingsControl settingsControl,
        ListCollectionView allClassesView
    ) {
        this._classManager = manager;
        this._settingsControl = settingsControl;
        this._classesManager = classesManager;
        this.AllClassesView = allClassesView;

        this.ResetColorsCommand = new Command(() => this._classManager.ResetColors());
        this.ResetShortNameCommand = new Command(() => this._classManager.ResetShortName());
        this.ResetReplaceWithCommand = new Command(() => this._classManager.ResetReplaceWith());
        this.ResetAllCommand = new Command(() => this._classManager.Reset());
        this.DisableAllCommand = new Command(() => this._classManager.DisableAll());
        this.RemoveClassCommand = new Command(() => this._classesManager.Remove(this.Class));
        this.DuplicateClassCommand = new Command(this.DuplicateClass);

        this._classManager.PropertyChanged += this.OnManagerPropertyChanged;
    }

    internal void Unsubscribe() {
        this._classManager.PropertyChanged -= this.OnManagerPropertyChanged;
    }

    private void OnManagerPropertyChanged(object sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(OverridableClassInfo.Manager._Foreground):
                this.InvokePropertyChanged(nameof(this.Foreground));
                break;
            case nameof(OverridableClassInfo.Manager._Background):
                this.InvokePropertyChanged(nameof(this.Background));
                break;
            case nameof(OverridableClassInfo.Manager._IsColorEnabled):
                this.InvokePropertyChanged(nameof(this.IsColorEnabled));
                break;
            case nameof(OverridableClassInfo.Manager._ShortName):
                this.InvokePropertyChanged(nameof(this.ShortName));
                break;
            case nameof(OverridableClassInfo.Manager._ReplaceWith):
                this.InvokePropertyChanged(nameof(this.ReplaceWith));
                break;
            case nameof(OverridableClassInfo.Manager._IsReplaceWithEnabled):
                this.InvokePropertyChanged(nameof(this.IsReplaceWithEnabled));
                break;
            case nameof(this.CanBeRemoved):
                this.InvokePropertyChanged(nameof(this.CanBeRemoved));
                break;
        }
    }

    private void InvokePropertyChanged([CallerMemberName] string? propertyName = null) {
        if (propertyName == null) {
            return;
        }

        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void DuplicateClass() {
        try {
            var dialogWindow = new ChooseNewClassNameDialog($"Duplicate {this.Class.AsString()}", this._classesManager);
            var res = await dialogWindow.ShowDialogWindowAsync(this._settingsControl);

            switch (res) {
                case DialogResult.OK:
                    var clsName = dialogWindow.Text!;
                    // ChooseNewClassNameDialog validates that the entered class name is valid new name and OK cannot be pressed before
                    var cls = new CarClass(clsName);
                    this._classesManager.Duplicate(old: this.Class, @new: cls);
                    break;
                case DialogResult.None:
                case DialogResult.Cancel:
                case DialogResult.Abort:
                case DialogResult.Retry:
                case DialogResult.Ignore:
                case DialogResult.Yes:
                case DialogResult.No:
                default:
                    break;
            }
        } catch (Exception e) {
            Logging.LogError($"Failed to duplicate the class: {e}");
        }
    }
}

public class ClassListBoxItem : Control {
    internal ClassListBoxItem(ClassListBoxItemViewModel vm) {
        this.DataContext = vm;
    }

    internal ClassListBoxItemViewModel _ViewModel => (ClassListBoxItemViewModel)this.DataContext;
}

internal class ClassListBoxItemViewModel : INotifyPropertyChanged {
    private OverridableClassInfo.Manager? _replacedWithManager = null;
    private readonly OverridableClassInfo.Manager _classManager;
    private readonly ClassInfos.Manager _classesManager;

    public CarClass Class => this._classManager._Key;
    public ClassPreviewViewModel ClassPreview { get; }
    public ClassPreviewViewModel? ReplaceWithPreview { get; private set; } = null;
    public bool HasReplacement => this._replacedWithManager != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    #if DESIGN
    #pragma warning disable CS8618, CS9264
    internal ClassListBoxItemViewModel() { }
    #pragma warning restore CS8618, CS9264
    #endif

    internal ClassListBoxItemViewModel(
        OverridableClassInfo.Manager manager,
        ClassSettingsTabViewModel vm,
        ClassInfos.Manager classesManager
    ) {
        this._classManager = manager;
        this._classesManager = classesManager;
        this.ClassPreview = new ClassPreviewViewModel(this._classManager);

        this._classManager.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(OverridableClassInfo.Manager._ReplaceWith)) {
                if (this._classManager._ReplaceWith != null) {
                    this._classesManager.TryAdd(this._classManager._ReplaceWith.Value);
                }

                vm.UpdateReplaceWiths();
            }
        };
    }

    internal void UpdateReplaceWith() {
        var newManager = this._classesManager.GetOrAddFollowReplaceWith(this._classManager._Key);
        if (newManager._Key == this._classManager._Key) {
            newManager = null;
        }

        this.SetReplaceWith(newManager);
    }

    internal void SetReplaceWith(OverridableClassInfo.Manager? replacement) {
        this.ReplaceWithPreview
            ?.Unsubscribe(); // unsubscribe old preview from the manager so that the manager doesn't hold a reference to it and the preview could be destroyed

        this._replacedWithManager = replacement;
        this.ReplaceWithPreview = this._replacedWithManager != null
            ? new ClassPreviewViewModel(this._replacedWithManager)
            : null;

        this.PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nameof(ClassListBoxItemViewModel.ReplaceWithPreview))
        );
        this.PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nameof(ClassListBoxItemViewModel.HasReplacement))
        );
    }

    internal class KeyComparer : IComparer {
        public int Compare(object? x, object? y) {
            if (x == null || y == null) {
                throw new ArgumentException("x and y must not be null");
            }

            if (x is ClassListBoxItem x2 && y is ClassListBoxItem y2) {
                return string.Compare(
                    x2._ViewModel._classManager._Key.AsString(),
                    y2._ViewModel._classManager._Key.AsString(),
                    StringComparison.OrdinalIgnoreCase
                );
            }

            throw new ArgumentException($"x and y must be {typeof(ClassListBoxItem)}");
        }
    }
}

public class ValidClassNameRule : ValidationRule {
    public override ValidationResult Validate(object? value, CultureInfo cultureInfo) {
        if (value == null) {
            return new ValidationResult(false, "Value cannot be null");
        }

        if (value is string str) {
            if (str == "") {
                return new ValidationResult(false, "Class name cannot be empty");
            }

            return ValidationResult.ValidResult;
        }

        if (value is CarClass) {
            return ValidationResult.ValidResult;
        }

        return new ValidationResult(false, $"Class name cannot be of type {value.GetType().Name}");
    }
}

public class ValidNewClassNameRule : ValidationRule {
    private readonly ClassInfos.Manager _classesManager;

    internal ValidNewClassNameRule(ClassInfos.Manager classesManager) {
        this._classesManager = classesManager;
    }

    public override ValidationResult Validate(object? value, CultureInfo cultureInfo) {
        if (value == null) {
            return new ValidationResult(false, "Class name cannot be null");
        }

        if (value is not string str) {
            return new ValidationResult(false, $"Class name cannot be of type {value.GetType().Name}");
        }

        if (str == "") {
            return new ValidationResult(false, "Class name cannot be empty");
        }

        var cls = new CarClass(str);
        if (this._classesManager.ContainsClass(cls)) {
            return new ValidationResult(false, $"Class name `{str}` already exists");
        }

        return ValidationResult.ValidResult;
    }
}

internal class ChooseNewClassNameDialog : AskTextDialog {
    internal ChooseNewClassNameDialog(string title, ClassInfos.Manager classesManager) : base(
        title,
        "Name",
        [new ValidNewClassNameRule(classesManager)]
    ) { }
}

#if DESIGN
internal class DesignClassSettingsTabViewModel : ClassSettingsTabViewModel {
    public new DesignSelectedClassViewModel SelectedClassViewModel { get; set; } = new();
    public new bool IsSelectedNull { get; set; } = false;

    public new ListCollectionView ClassesListCollectionView { get; set; } =
        DesignClassSettingsTabViewModel.CreateClasses();

    public new ListCollectionView AllClassesView { get; set; } = new(new[] { "a", "b" });

    private static ListCollectionView CreateClasses() {
        List<ClassListBoxItem> list = [DesignClassSettingsTabViewModel.CreateSelectedItem()];

        var c2 = new DesignClassListBoxItemViewModel {
            ClassPreview = new DesignClassPreviewViewModel {
                ClassName = "LMDh", IsColorEnabled = false, Background = "tomato", Foreground = "black",
            },
            ReplaceWithPreview = null,
        };
        list.Add(new ClassListBoxItem(c2));

        var c3 = new DesignClassListBoxItemViewModel {
            ClassPreview = new DesignClassPreviewViewModel {
                ClassName = "sport classics", IsColorEnabled = false, Background = "green", Foreground = "white",
            },
            ReplaceWithPreview = new DesignClassPreviewViewModel {
                ClassName = "sports", IsColorEnabled = true, Background = "dodgerblue", Foreground = "white",
            },
        };
        list.Add(new ClassListBoxItem(c3));

        var c4 = new DesignClassListBoxItemViewModel {
            ClassPreview = new DesignClassPreviewViewModel {
                ClassName = "sport classics", IsColorEnabled = true, Background = "green", Foreground = "white",
            },
            ReplaceWithPreview = new DesignClassPreviewViewModel {
                ClassName = "sports", IsColorEnabled = false, Background = "dodgerblue", Foreground = "white",
            },
        };
        list.Add(new ClassListBoxItem(c4));

        return new ListCollectionView(list);
    }

    private static ClassListBoxItem CreateSelectedItem() {
        var c1 = new DesignClassListBoxItemViewModel {
            ClassPreview = new DesignClassPreviewViewModel {
                ClassName = "GT4", IsColorEnabled = true, Background = "black", Foreground = "white",
            },
            ReplaceWithPreview = null,
        };
        return new ClassListBoxItem(c1);
    }
}

internal class DesignClassListBoxItemViewModel : ClassListBoxItemViewModel {
    public new DesignClassPreviewViewModel ClassPreview { get; set; } = new();
    public new DesignClassPreviewViewModel? ReplaceWithPreview { get; set; } = null;
    public new bool HasReplacement => this.ReplaceWithPreview != null;
}

internal class DesignSelectedClassViewModel : SelectedClassViewModel {
    public new CarClass Class { get; set; } = new("Test22");
    public new bool IsColorEnabled { get; set; } = true;
    public new string Background { get; set; } = "tomato";
    public new string Foreground { get; set; } = "black";
    public new string ShortName { get; set; } = "Test";
    public new bool IsReplaceWithEnabled { get; set; } = false;
    public new CarClass ReplaceWith { get; set; } = new("Test2");
    public new bool CanBeRemoved { get; set; } = false;
}
#endif