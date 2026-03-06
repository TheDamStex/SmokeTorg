namespace SmokeTorg.Presentation.Services;

public interface IWindowService
{
    void ShowWindow<TViewModel>(TViewModel vm) where TViewModel : class;
}
