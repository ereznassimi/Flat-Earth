using System;
using System.Windows.Input;


namespace FlatEarth;

public class DelegateCommand: ICommand
{
    private readonly Action<object?> Command;

    public DelegateCommand(Action<object?> command)
    {
        this.Command = command;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => this.Command(parameter);

    public event EventHandler? CanExecuteChanged;
}
