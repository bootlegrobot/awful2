using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Commands
{
    public delegate void ExecuteCallback(object parameter);
    public delegate bool CanExecuteCallback(object parameter);

    public class RelayCommand : ICommand
    {
        private readonly ExecuteCallback _execute;
        private readonly CanExecuteCallback _canExecute;

        public RelayCommand(ExecuteCallback execute, CanExecuteCallback canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public RelayCommand(ExecuteCallback execute) : this(execute, RelayCommand.CanExecuteDefault) { }

        private static bool CanExecuteDefault(object parameter) { return true; }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;


        public void Execute(object parameter)
        {
            if (this._canExecute(parameter))
                this._execute(parameter);
        }
    }
}
