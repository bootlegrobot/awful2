using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Common
{
    public class ActionCommand : ICommand
    {
        private Action<object> _action;
        public ActionCommand(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return _action != null;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
                _action(parameter);
        }
    }
}
