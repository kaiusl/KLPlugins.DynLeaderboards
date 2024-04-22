using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
