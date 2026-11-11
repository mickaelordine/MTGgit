using System.Windows;

namespace CommanderDeckBuilder.UI;

/// \brief Represents the main WPF application window.
public partial class MainWindow : Window
{
    /// \brief Initializes the window and assigns its view model as data context.
    /// \param viewModel The view model presented by the window.
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}