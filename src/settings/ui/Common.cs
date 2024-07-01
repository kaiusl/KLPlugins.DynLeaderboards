using System;
using System.Windows.Input;

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
}