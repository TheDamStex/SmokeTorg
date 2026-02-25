namespace SmokeTorg.Presentation.Services;

public interface IDialogRequestClose
{
    event EventHandler<bool?>? RequestClose;
}
