using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StringifyDesktop.Views;

public partial class MainDashboardView : Avalonia.Controls.UserControl
{
    public MainDashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
