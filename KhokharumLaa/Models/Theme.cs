using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace KhokharumLaa.Models
{
    public class Theme : INotifyPropertyChanged
    {
        private System.Windows.Media.FontFamily _fontFamily;

        public string Name { get; set; }
        public System.Windows.Media.Brush BackgroundBrush { get; set; }
        public System.Windows.Media.Brush ForegroundBrush { get; set; }
        public int FontSize { get; set; }

        public System.Windows.Media.FontFamily FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    OnPropertyChanged(); // This notify the ViewModel of the change
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
