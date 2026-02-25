using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmokeTorg.Common.Base;

public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count != 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || !_errors.TryGetValue(propertyName, out var errors))
        {
            return Enumerable.Empty<string>();
        }

        return errors;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);

        if (!string.IsNullOrWhiteSpace(name))
        {
            ValidateProperty(name);
        }

        return true;
    }

    protected virtual void ValidateProperty(string propertyName)
    {
    }

    protected virtual bool ValidateAll()
    {
        var properties = GetType().GetProperties()
            .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        foreach (var propertyName in properties)
        {
            ValidateProperty(propertyName);
        }

        return !HasErrors;
    }

    protected void AddError(string propertyName, string errorMessage)
    {
        if (!_errors.TryGetValue(propertyName, out var errors))
        {
            errors = [];
            _errors[propertyName] = errors;
        }

        if (errors.Contains(errorMessage))
        {
            return;
        }

        errors.Add(errorMessage);
        RaiseErrorsChanged(propertyName);
    }

    protected void ClearErrors(string propertyName)
    {
        if (!_errors.Remove(propertyName))
        {
            return;
        }

        RaiseErrorsChanged(propertyName);
    }

    protected void ClearAllErrors()
    {
        var propertyNames = _errors.Keys.ToList();
        _errors.Clear();

        foreach (var propertyName in propertyNames)
        {
            RaiseErrorsChanged(propertyName);
        }
    }

    protected void RaiseErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        OnErrorsChanged(propertyName);
    }

    protected virtual void OnErrorsChanged(string propertyName)
    {
    }
}
