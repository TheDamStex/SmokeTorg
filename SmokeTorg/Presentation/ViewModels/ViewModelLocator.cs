using Microsoft.Extensions.DependencyInjection;
namespace SmokeTorg.Presentation.ViewModels;

public class ViewModelLocator
{
    public MainViewModel Main => App.Services.GetRequiredService<MainViewModel>();
}
