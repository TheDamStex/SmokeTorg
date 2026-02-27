using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmokeTorg.Presentation.Behaviors;

public static class EnterKeyNavigationBehavior
{
    public static readonly DependencyProperty MoveFocusOnEnterProperty =
        DependencyProperty.RegisterAttached(
            "MoveFocusOnEnter",
            typeof(bool),
            typeof(EnterKeyNavigationBehavior),
            new PropertyMetadata(false, OnMoveFocusOnEnterChanged));

    public static readonly DependencyProperty EnterCommandProperty =
        DependencyProperty.RegisterAttached(
            "EnterCommand",
            typeof(ICommand),
            typeof(EnterKeyNavigationBehavior),
            new PropertyMetadata(null));

    public static bool GetMoveFocusOnEnter(DependencyObject obj) => (bool)obj.GetValue(MoveFocusOnEnterProperty);
    public static void SetMoveFocusOnEnter(DependencyObject obj, bool value) => obj.SetValue(MoveFocusOnEnterProperty, value);

    public static ICommand? GetEnterCommand(DependencyObject obj) => (ICommand?)obj.GetValue(EnterCommandProperty);
    public static void SetEnterCommand(DependencyObject obj, ICommand? value) => obj.SetValue(EnterCommandProperty, value);

    private static void OnMoveFocusOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            element.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not UIElement element)
        {
            return;
        }

        var command = GetEnterCommand(element);
        if (command is not null && command.CanExecute(null))
        {
            command.Execute(null);
            e.Handled = true;
            return;
        }

        element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        e.Handled = true;
    }
}
