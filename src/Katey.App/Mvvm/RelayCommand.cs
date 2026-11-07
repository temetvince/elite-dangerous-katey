using System.Windows.Input;

namespace Katey.App.Mvvm;

/// <summary>A command that runs a synchronous action.</summary>
/// <param name="execute">What the command does.</param>
/// <param name="canExecute">Whether it may run now, or <see langword="null"/> for always.</param>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => execute();

    /// <summary>Tells bound controls to ask <see cref="CanExecute"/> again.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>A command that runs a synchronous action with the bound parameter.</summary>
/// <typeparam name="T">The parameter type. A parameter of any other type is ignored.</typeparam>
/// <param name="execute">What the command does with the parameter.</param>
/// <param name="canExecute">Whether it may run for a parameter, or <see langword="null"/> for always.</param>
public sealed class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
{
    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => parameter is T value ? canExecute?.Invoke(value) ?? true : parameter is null && canExecute is null;

    /// <inheritdoc/>
    public void Execute(object? parameter)
    {
        if (parameter is T value)
        {
            execute(value);
        }
    }

    /// <summary>Tells bound controls to ask <see cref="CanExecute"/> again.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
