using SmokeTorg.Common.Base;

namespace SmokeTorg.Presentation.ViewModels;

public class PlaceholderViewModel : ViewModelBase
{
    private string _title = "Модуль";
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public PlaceholderViewModel WithTitle(string title)
    {
        Title = title;
        return this;
    }
}
