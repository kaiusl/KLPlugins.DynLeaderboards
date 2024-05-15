using System.Windows;
using System.Windows.Controls;

using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

namespace KLPlugins.DynLeaderboards {
    /// <summary>
    /// Interaction logic for ConfimDialog.xaml
    /// </summary>
    public partial class ConfimDialog : SHDialogContentBase {
        public ConfimDialog(string titleText, string msg) {
            this.InitializeComponent();
            this.ShowYes = true;
            this.ShowCancel = true;

            var sp = this.StackPanel;

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
}
