using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KhokharumLaa.ViewModels
{
    public class ProjectorViewModel : BaseViewModel
    {
        private string _liveContent = "[Ready]";
        private bool _isScreenBlack = false;
        private System.Windows.Media.Brush _backgroundBrush = System.Windows.Media.Brushes.Black;
        private System.Windows.Media.Brush _foregroundBrush = System.Windows.Media.Brushes.White;

        // --- VIDEO: New properties for video background ---
        private Uri _videoSource;
        private bool _isVideoVisible;

        //properties for Image Display
        private BitmapImage _imageSource;

        private bool _isShowingImage = false;
        private System.Windows.Media.FontFamily _fontFamily = new System.Windows.Media.FontFamily("Arial");
        private int _fontSize = 60;

        public string LiveContent
        {
            get => _liveContent;
            set { if (_liveContent != value) { _liveContent = value; OnPropertyChanged(); } }
        }

        public bool IsScreenBlack
        {
            get => _isScreenBlack;
            set { if (_isScreenBlack != value) { _isScreenBlack = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// The background brush for the projector screen.
        /// </summary>
        public System.Windows.Media.Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set { if (_backgroundBrush != value) { _backgroundBrush = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// The foreground (font color) brush for the projector screen.
        /// </summary>
        public System.Windows.Media.Brush ForegroundBrush
        {
            get => _foregroundBrush;
            set { if (_foregroundBrush != value) { _foregroundBrush = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Holds the image (e.g., the program order) to be displayed.
        /// </summary>
        public BitmapImage ImageSource
        {
            get => _imageSource;
            set { if (_imageSource != value) { _imageSource = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Controls whether the image or the lyrics are currently visible.
        /// </summary>
        public bool IsShowingImage
        {
            get => _isShowingImage;
            set { if (_isShowingImage != value) { _isShowingImage = value; OnPropertyChanged(); } }
        }

        public System.Windows.Media.FontFamily FontFamily
        {
            get => _fontFamily;
            set { if (_fontFamily != value) { _fontFamily = value; OnPropertyChanged(); } }
        }

        public int FontSize
        {
            get => _fontSize;
            set { if (_fontSize != value) { _fontSize = value; OnPropertyChanged(); } }
        }

        // --- VIDEO: Public accessors for the new properties ---
        public Uri VideoSource
        {
            get => _videoSource;
            set { _videoSource = value; OnPropertyChanged(); }
        }

        public bool IsVideoVisible
        {
            get => _isVideoVisible;
            set { _isVideoVisible = value; OnPropertyChanged(); }
        }
    }
}
