using KhokharumLaa.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;

namespace KhokharumLaa
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
           
            base.OnStartup(e);

            
            // 1. Create the main ViewModel.
            var operatorViewModel = new OperatorViewModel();

            // 2. Create the OperatorWindow, set its DataContext, and show it.
           
            var operatorWindow = new OperatorWindow { DataContext = operatorViewModel };
            operatorWindow.Show();
        
    }
    }

}
