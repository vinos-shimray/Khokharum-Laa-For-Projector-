using KhokharumLaa.Models;
using System.Windows;

namespace KhokharumLaa
{
    public partial class AddSongWindow : Window
    {
       

        public AddSongWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            
            this.DialogResult = false;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            
            this.DialogResult = true;
        }


    }
}