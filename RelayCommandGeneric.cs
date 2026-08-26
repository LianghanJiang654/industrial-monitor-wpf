using System;
using System.Windows.Input;

namespace FactorialApp
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;

        public RelayCommand(Action<T> execute)
        {
            this.execute = execute;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            execute((T)parameter);
        }

        public event EventHandler? CanExecuteChanged;
    }
}