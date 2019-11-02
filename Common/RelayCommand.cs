using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{
	private readonly Action<object> FExecute;
	private readonly Predicate<object> FCanExecute;

	public RelayCommand(Action<object> execute)
		: this(execute, null)
	{
	}

	public RelayCommand(Action<object> execute, Predicate<object> canExecute)
	{
		if (execute == null)
			throw new ArgumentNullException("execute");

		FExecute = execute;
		FCanExecute = canExecute;
	}

	public bool CanExecute(object parameter)
	{
		return FCanExecute == null ? true : FCanExecute(parameter);
	}

	public event EventHandler CanExecuteChanged
	{
		add { CommandManager.RequerySuggested += value; }
		remove { CommandManager.RequerySuggested -= value; }
	}

	public void Execute(object parameter)
	{
		FExecute(parameter);
	}
}
