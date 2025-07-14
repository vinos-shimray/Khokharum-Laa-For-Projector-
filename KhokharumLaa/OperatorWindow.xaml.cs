using KhokharumLaa.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace KhokharumLaa
{
    /// <summary>
    /// Interaction logic for OperatorWindow.xaml
    /// </summary>
    public partial class OperatorWindow : Window
    {
        private Window _projectorWindow;
        public OperatorWindow()
        {
            InitializeComponent();
            // --- DECOUPLING: Hook into the Loaded event ---
            this.Loaded += OperatorWindow_Loaded;
            this.Closing += OperatorWindow_Closing;
        }

        private void OperatorWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            if (this.DataContext is OperatorViewModel viewModel)
            {
                _projectorWindow = new ProjectorWindow
                {
                    DataContext = viewModel.ProjectorViewModel
                };
                _projectorWindow.Show();
            }

            // Unsubscribe from the event so it doesn't run again
            this.Loaded -= OperatorWindow_Loaded;
        }

        private void OperatorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // --- LIFECYCLE: When this window closes, close the projector window too ---
            _projectorWindow?.Close();

          
            System.Windows.Application.Current.Shutdown();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Open the link in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
