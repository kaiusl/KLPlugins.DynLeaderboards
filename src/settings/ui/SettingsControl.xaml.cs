using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using KLPlugins.DynLeaderboards.Car;


using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

namespace KLPlugins.DynLeaderboards.Settings.UI {
    public class MessageDialog : SHDialogContentBase {
        public MessageDialog(string titleText, string msg) {
            this.ShowOk = true;

            var sp = new StackPanel();
            this.Content = sp;

            var title = new SHSectionTitle() {
                Text = titleText,
                Margin = new Thickness(0, 0, 0, 25)
            };

            sp.Children.Add(title);

            sp.Children.Add(new TextBlock() {
                Text = msg
            });
        }
    }


    public class ButtonMenuItem : MenuItem {
        public bool ShowDropDown {
            get => (bool)this.GetValue(ShowDropDownProperty);
            set => this.SetValue(ShowDropDownProperty, value);
        }
        public static readonly DependencyProperty ShowDropDownProperty =
                DependencyProperty.RegisterAttached("ShowDropDown", typeof(bool), typeof(ButtonMenuItem), new PropertyMetadata(false));
    }


    public class SectionTitle : UserControl {

        public string HelpPath {
            get => (string)this.GetValue(HelpPathProperty);
            set => this.SetValue(HelpPathProperty, value);
        }
        public static readonly DependencyProperty HelpPathProperty =
                DependencyProperty.RegisterAttached("HelpPath", typeof(string), typeof(SectionTitle), new PropertyMetadata("null"));
    }

    public class DocsHyperlink : Hyperlink {
        public string RelativePath {
            get => (string)this.GetValue(RelativePathProperty);
            set => this.SetValue(RelativePathProperty, value);
        }
        public static readonly DependencyProperty RelativePathProperty =
                DependencyProperty.RegisterAttached("RelativePath", typeof(string), typeof(DocsHyperlink), new PropertyMetadata(""));

        public DocsHyperlink() {
            this.RequestNavigate += (sender, e) => {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            };
        }
    }

    public class DocsHelpButton : UserControl {
        public string RelativePath {
            get => (string)this.GetValue(RelativePathProperty);
            set => this.SetValue(RelativePathProperty, value);
        }
        public static readonly DependencyProperty RelativePathProperty =
                DependencyProperty.RegisterAttached("RelativePath", typeof(string), typeof(DocsHelpButton), new PropertyMetadata(""));
    }

    public sealed class DocsPathConverter : IValueConverter {
#if DOCS_DEBUG
        public const string DOCS_ROOT = "http://127.0.0.1:8000/KLPlugins.DynLeaderboards/";
#else
        public const string DOCS_ROOT = "https://kaiusl.github.io/KLPlugins.DynLeaderboards/2.0.x/";
#endif

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                string fullPath = DOCS_ROOT + (string)value;
                return fullPath;
            } catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }

    public partial class SettingsControl : UserControl {
        internal DynLeaderboardsPlugin Plugin { get; }
        internal PluginSettings Settings => DynLeaderboardsPlugin.Settings;
        internal DynLeaderboardConfig CurrentDynLeaderboardSettings { get; private set; }
        internal CarSettingsTab CarSettingsTab { get; private set; }
        internal ClassInfos.Manager ClassesManager { get; private set; }

        public const double DISABLED_OPTION_OPACITY = 0.333;

        //internal SettingsControl() {
        //    InitializeComponent();
        //    DataContext = this;
        //}

        internal SettingsControl(DynLeaderboardsPlugin plugin) {
            this.InitializeComponent();
            this.DataContext = this;

            this.Plugin = plugin;

            this.ClassesManager = new ClassInfos.Manager(this.Plugin.Values.ClassInfos);
            this.ClassesManager.CollectionChanged += (s, e) => {
                if (e.NewItems != null) {
                    foreach (OverridableClassInfo.Manager item in e.NewItems) {
                        this.TryAddCarClass(item.Key);
                    }
                }
                //if (e.OldItems != null) {
                //    foreach (OverridableClassInfo.Manager item in e.OldItems) {
                //    }

                //}
            };

            this.GeneralSettingsTab_SHTabItem.Content = new GeneralSettingsTab(this.Settings);
            this.DynamicLeaderboardsTab_SHTabItem.Content = new DynamicLeaderboardsTab(this.Settings, this.Plugin, this);
            this.ClassSettingsTab_SHTabItem.Content = new ClassSettingsTab(this, this.Plugin.Values, this.ClassesManager);

            if (this.Settings.DynLeaderboardConfigs.Count == 0) {
                this.Plugin.AddNewLeaderboard(new DynLeaderboardConfig("Dynamic"));
            }
            this.CurrentDynLeaderboardSettings = this.Settings.DynLeaderboardConfigs[0];



            this.SetAllClassesAndManufacturers();
            this.CarSettingsTab = new CarSettingsTab(this, this.Plugin);
            this.CarSettingsTab.Build();
            this.AddColorsTab();
        }

        internal ObservableCollection<string> AllClasses = new();
        internal ObservableCollection<string> AllManufacturers = new();

        /// <summary>
        /// Tries to add a new class but does nothing if the class already exists.
        /// </summary>
        internal void TryAddCarClass(CarClass cls) {
            var clsStr = cls.AsString();
            if (!this.AllClasses.Contains(clsStr)) {
                this.AllClasses.Add(clsStr);
            }
        }

        internal void AddCarManufacturer(string manufacturer) {
            if (!this.AllManufacturers.Contains(manufacturer)) {
                this.AllManufacturers.Add(manufacturer);
            }
        }

        internal async void DoOnConfirmation(Action action) {
            var dialogWindow = new ConfimDialog("Are you sure?", "All custom overrides will be lost.");
            var res = await dialogWindow.ShowDialogWindowAsync(this);

            switch (res) {
                case System.Windows.Forms.DialogResult.Yes:
                    action();
                    break;
                default:
                    break;

            };
        }

        void SetAllClassesAndManufacturers() {
            // Go through all cars and check for class colors. 
            // If there are new classes then trying to Values.CarClassColors.Get will add them to the dictionary.
            foreach (var c in this.Plugin.Values.CarInfos) {
                CarClass?[] classes = [c.Value.ClassDontCheckEnabled(), c.Value.BaseClass()];
                foreach (var cls in classes) {
                    if (cls != null) {
                        var info = this.Plugin.Values.ClassInfos.Get(cls.Value);
                        if (info.ReplaceWithDontCheckEnabled() != null) {
                            var _ = this.Plugin.Values.ClassInfos.Get(info.ReplaceWithDontCheckEnabled()!.Value);
                        }
                    }
                }

                string?[] manufacturers = [c.Value.Manufacturer(), c.Value.BaseManufacturer()];
                foreach (var manufacturer in manufacturers) {
                    if (manufacturer != null) {
                        this.AddCarManufacturer(manufacturer);
                    }
                }
            }

            foreach (var c in this.Plugin.Values.ClassInfos) {
                this.TryAddCarClass(c.Key);
            }
        }


        void AddColorsTab() {
            new ColorsTabSection<TeamCupCategory>(
                this,
                this.Plugin,
                "Category",
                this.Plugin.Values.TeamCupCategoryColors,
                this.ColorsTab_TeamCupCategoryColors_Menu,
                this.ColorsTab_TeamCupCategoryColors_Grid,
                this.Plugin.Values.UpdateTeamCupInfos
            ).Build(c => c == TeamCupCategory.Default);

            new ColorsTabSection<DriverCategory>(
                this,
                this.Plugin,
                "Category",
                this.Plugin.Values.DriverCategoryColors,
                this.ColorsTab_DriverCategoryColors_Menu,
                this.ColorsTab_DriverCategoryColors_Grid,
                this.Plugin.Values.UpdateDriverInfos
            ).Build(c => c == DriverCategory.Default);
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
            if (e.NewValue != null) {
                this.Settings.BroadcastDataUpdateRateMs = (int)e.NewValue;
            }
        }
    }
}