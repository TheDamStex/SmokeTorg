namespace SmokeTorg.Presentation.Services;

public interface IDialogService
{
    bool? ShowDialog<TViewModel>(TViewModel vm) where TViewModel : class;
}
