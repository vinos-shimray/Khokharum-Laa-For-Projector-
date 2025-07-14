using KhokharumLaa.DataAccess;
using KhokharumLaa.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace KhokharumLaa.ViewModels
{
    public class NamedBrush { public string Name { get; set; } public System.Windows.Media.Brush Brush { get; set; } }

    public class OperatorViewModel : BaseViewModel
    {
        //-------------------------------------------------------------------------
        // Private Fields
        //-------------------------------------------------------------------------
        //private readonly ProjectorViewModel _projectorViewModel;
        private readonly HymnalRepository _repository;
        private Song _selectedSong;
        private SongPart _selectedPart;
        private string _searchText = string.Empty;
        private ObservableCollection<Song> _allSongs;
        private Song _liveSong;
        private Theme _selectedTheme;
        private int _selectedFontSize;
        private bool _isVideoModeActive = false;
        private NamedBrush _selectedFontColor;

        //-------------------------------------------------------------------------
        // Public Properties
        //-------------------------------------------------------------------------
        public ProjectorViewModel ProjectorViewModel { get; }
        public ObservableCollection<Song> FilteredSongs { get; private set; }
        public ObservableCollection<Theme> AvailableThemes { get; private set; }
        public ObservableCollection<System.Windows.Media.FontFamily> SystemFonts { get; private set; }
        public ObservableCollection<NamedBrush> FontColors { get; private set; }

        public Song SelectedSong { get => _selectedSong; set { _selectedSong = value; OnPropertyChanged(); SelectedPart = null; } }
        public SongPart SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (_selectedPart != value)
                {
                    _selectedPart = value;
                    OnPropertyChanged();
                    if (_liveSong != null && _selectedPart != null && _liveSong == SelectedSong)
                    {
                        GoLive(null);
                    }
                }
            }
        }
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); FilterSongList(); } }
        public Theme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    if (_selectedTheme != null)
                    {
                        _selectedTheme.PropertyChanged -= SelectedTheme_PropertyChanged;
                    }
                    _selectedTheme = value;
                    OnPropertyChanged();
                    if (_selectedTheme != null)
                    {
                        //SelectedFontSize = _selectedTheme.FontSize;
                        //_selectedTheme.PropertyChanged += SelectedTheme_PropertyChanged;
                        SelectedFontSize = _selectedTheme.FontSize;
                        //SelectedFontColor = FontColors.FirstOrDefault(c => c.Brush.ToString() == _selectedTheme.ForegroundBrush.ToString()) ?? FontColors.First();
                        var matchingColor = FontColors.FirstOrDefault(c => c.Brush.ToString() == _selectedTheme.ForegroundBrush.ToString());
                        if (matchingColor != null)
                        {
                            SelectedFontColor = matchingColor;
                        }
                        else
                        {
                            // If not found, add a new NamedBrush for the theme's foreground
                            var themeBrush = new NamedBrush
                            {
                                Name = $"Theme Foreground ({_selectedTheme.ForegroundBrush})",
                                Brush = _selectedTheme.ForegroundBrush
                            };
                            FontColors.Insert(0, themeBrush); // Optionally insert at the top
                            SelectedFontColor = themeBrush;
                        }
                    }
                    ApplyTheme();
                }
            }
        }
        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set
            {
                if (_selectedFontSize != value)
                {
                    _selectedFontSize = value;
                    OnPropertyChanged();
                    ApplyTheme();
                }
            }
        }
        public NamedBrush SelectedFontColor
        {
            get => _selectedFontColor;
            set
            {
                if (_selectedFontColor != value)
                {
                    _selectedFontColor = value;
                    OnPropertyChanged();
                    ApplyTheme();
                   
                }
            }
        }

        // --- Commands ---
        public ICommand GoLiveCommand { get; }
        public ICommand ClearScreenCommand { get; }
        public ICommand BlankScreenCommand { get; }
        public ICommand LoadImageCommand { get; }
        public ICommand ShowImageCommand { get; }
        public ICommand ShowLyricsCommand { get; }
        public ICommand IncreaseFontSizeCommand { get; }
        public ICommand DecreaseFontSizeCommand { get; }
        public ICommand LoadVideoCommand { get; }
        public ICommand StopVideoCommand { get; }
        public ICommand AddSongCommand { get; }

        //-------------------------------------------------------------------------
        // Constructor
        //-------------------------------------------------------------------------
        public OperatorViewModel()
        {
            ProjectorViewModel = new ProjectorViewModel();

            _repository = new HymnalRepository("hymnal.db");
            SystemFonts = new ObservableCollection<System.Windows.Media.FontFamily>(System.Windows.Media.Fonts.SystemFontFamilies.OrderBy(f => f.Source));
            LoadData();
            SetupFontColors();
            SetupThemes();
            
            SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Name.Contains("Light")) ?? AvailableThemes.FirstOrDefault();

            GoLiveCommand = new RelayCommand(GoLive, CanGoLive);
            ClearScreenCommand = new RelayCommand(ClearScreen);
            BlankScreenCommand = new RelayCommand(BlankScreen);
            LoadImageCommand = new RelayCommand(LoadImage);
            ShowImageCommand = new RelayCommand(ShowImage, CanShowImage);
            ShowLyricsCommand = new RelayCommand(ShowLyrics);
            IncreaseFontSizeCommand = new RelayCommand(IncreaseFontSize);
            DecreaseFontSizeCommand = new RelayCommand(DecreaseFontSize, CanDecreaseFontSize);
            LoadVideoCommand = new RelayCommand(LoadVideo);
            StopVideoCommand = new RelayCommand(StopVideo);
            AddSongCommand = new RelayCommand(AddNewSong);

        }

        //-------------------------------------------------------------------------
        // Public Methods
        //-------------------------------------------------------------------------
        public void InitializeProjector()
        {
            var projectorWindow = new ProjectorWindow
            {
                DataContext = ProjectorViewModel
            };
            projectorWindow.Show();
        }

        //-------------------------------------------------------------------------
        // Command Handlers & Helper Methods
        //-------------------------------------------------------------------------
        private void IncreaseFontSize(object parameter) => SelectedFontSize++;
        private void DecreaseFontSize(object parameter) => SelectedFontSize--;
        private bool CanDecreaseFontSize(object parameter) => SelectedFontSize > 10;


        // --- ADD SONG: Method to handle adding a new song ---
        private void AddNewSong(object parameter)
        {
            var addSongViewModel = new AddSongViewModel();
            var addSongWindow = new AddSongWindow
            {
                DataContext = addSongViewModel,
                Owner = System.Windows.Application.Current.MainWindow // Set owner for proper dialog behavior
            };

            // Show the window as a dialog, which blocks the main window until it's closed.
            if (addSongWindow.ShowDialog() == true)
            {
                // The user clicked "Save". Get the new song from the dialog's ViewModel.
                var newSong = addSongViewModel.NewSong;

                // Save the new song to the database
                _repository.AddSong(newSong);

                // Add the new song to our in-memory collections to update the UI instantly
                _allSongs.Add(newSong);
                FilteredSongs.Add(newSong);
                SelectedSong = newSong;
            }
        }

        private void SelectedTheme_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the property that changed was the FontFamily, we must re-apply the theme
            if (e.PropertyName == nameof(Theme.FontFamily))
            {
                ApplyTheme();
            }
        }

        private void LoadVideo(object parameter)
        {
            // --- USER-FRIENDLY LOAD: Define the shipped resources folder name ---
            const string videoFolderName = "VideoBackgrounds";

            // --- USER-FRIENDLY LOAD: Get the full path to the application's startup directory ---
            string startupPath = AppDomain.CurrentDomain.BaseDirectory;
            string videoFolderPath = Path.Combine(startupPath, videoFolderName);

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a Video Background",
                Filter = "Video Files|*.mp4;*.wmv;*.avi;*.mov|GIF Files|*.gif|All files (*.*)|*.*"
            };

            // --- USER-FRIENDLY LOAD: Check if the custom folder exists ---
            if (Directory.Exists(videoFolderPath))
            {
                // If it exists, set it as the starting directory
                openFileDialog.InitialDirectory = videoFolderPath;
            }
            else
            {
                // Otherwise, fall back to the default "My Videos" folder
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }

            if (openFileDialog.ShowDialog() == true)
            {
                
                try
                {
                    // --- VALIDATION: Check the file extension before attempting to load ---
                    string extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                    string[] validVideoExtensions = { ".mp4", ".wmv", ".avi", ".mov", ".gif" };
                    if (!validVideoExtensions.Contains(extension))
                    {
                        throw new InvalidOperationException("Unsupported file type selected for video background.");
                    }

                    ProjectorViewModel.VideoSource = new Uri(openFileDialog.FileName);
                    ProjectorViewModel.IsVideoVisible = true;
                    _isVideoModeActive = true;
                    ApplyTheme();
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("The selected file is not a valid or supported video. Please select an MP4, WMV, AVI, MOV, or GIF file.", "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void StopVideo(object parameter)
        {
            ProjectorViewModel.IsVideoVisible = false;
            ProjectorViewModel.VideoSource = null;
            _isVideoModeActive = false;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (SelectedTheme != null && ProjectorViewModel != null)
            {
                if (_isVideoModeActive) { ProjectorViewModel.BackgroundBrush = System.Windows.Media.Brushes.Transparent; }
                else { ProjectorViewModel.BackgroundBrush = SelectedTheme.BackgroundBrush; }
                ProjectorViewModel.FontFamily = SelectedTheme.FontFamily;
               
                ProjectorViewModel.FontSize = SelectedFontSize;
               
                if (SelectedFontColor != null)
                {
                    ProjectorViewModel.ForegroundBrush = SelectedFontColor.Brush;
                }
            }
        }

        private void SetupFontColors()
        {
            FontColors = new ObservableCollection<NamedBrush>
            {
                new NamedBrush { Name = "White", Brush = System.Windows.Media.Brushes.White },
                new NamedBrush { Name = "Black", Brush = System.Windows.Media.Brushes.Black },
                new NamedBrush { Name = "Yellow", Brush = System.Windows.Media.Brushes.Yellow },
                new NamedBrush { Name = "Cyan", Brush = System.Windows.Media.Brushes.Cyan },
                new NamedBrush { Name = "Light Green", Brush = System.Windows.Media.Brushes.LightGreen },
                new NamedBrush { Name = "Pink", Brush = System.Windows.Media.Brushes.Pink },
                new NamedBrush { Name = "Orange", Brush = System.Windows.Media.Brushes.Orange },
                new NamedBrush { Name = "Red", Brush = System.Windows.Media.Brushes.Red },
                new NamedBrush { Name = "Gray", Brush = System.Windows.Media.Brushes.Gray },
                new NamedBrush { Name = "Dark Gray", Brush = System.Windows.Media.Brushes.DarkGray },
                // Standard WPF Colors
      
                new NamedBrush { Name = "Light Gray", Brush = System.Windows.Media.Brushes.LightGray },
                new NamedBrush { Name = "Silver", Brush = System.Windows.Media.Brushes.Silver },
                new NamedBrush { Name = "Dim Gray", Brush = System.Windows.Media.Brushes.DimGray },
                new NamedBrush { Name = "Blue", Brush = System.Windows.Media.Brushes.Blue },
                new NamedBrush { Name = "Sky Blue", Brush = System.Windows.Media.Brushes.SkyBlue },
                new NamedBrush { Name = "Steel Blue", Brush = System.Windows.Media.Brushes.SteelBlue },
                new NamedBrush { Name = "Royal Blue", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4169E1")) },
                new NamedBrush { Name = "Green", Brush = System.Windows.Media.Brushes.Green },
                new NamedBrush { Name = "Forest Green", Brush = System.Windows.Media.Brushes.ForestGreen },
                new NamedBrush { Name = "Lime Green", Brush = System.Windows.Media.Brushes.LimeGreen },
                new NamedBrush { Name = "Red", Brush = System.Windows.Media.Brushes.Red },
                new NamedBrush { Name = "Crimson", Brush = System.Windows.Media.Brushes.Crimson },
                new NamedBrush { Name = "Firebrick", Brush = System.Windows.Media.Brushes.Firebrick },
                new NamedBrush { Name = "Purple", Brush = System.Windows.Media.Brushes.Purple },
                new NamedBrush { Name = "Indigo", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4B0082")) },
                new NamedBrush { Name = "Violet", Brush = System.Windows.Media.Brushes.Violet },
                new NamedBrush { Name = "Gold", Brush = System.Windows.Media.Brushes.Gold },
                new NamedBrush { Name = "Orange", Brush = System.Windows.Media.Brushes.Orange },
                new NamedBrush { Name = "Dark Orange", Brush = System.Windows.Media.Brushes.DarkOrange },
                new NamedBrush { Name = "Coral", Brush = System.Windows.Media.Brushes.Coral },
                new NamedBrush { Name = "Brown", Brush = System.Windows.Media.Brushes.Brown },
                new NamedBrush { Name = "Chocolate", Brush = System.Windows.Media.Brushes.Chocolate },
                new NamedBrush { Name = "Saddle Brown", Brush = System.Windows.Media.Brushes.SaddleBrown },
                new NamedBrush { Name = "Pink", Brush = System.Windows.Media.Brushes.Pink },
                new NamedBrush { Name = "Hot Pink", Brush = System.Windows.Media.Brushes.HotPink },
                new NamedBrush { Name = "Deep Pink", Brush = System.Windows.Media.Brushes.DeepPink },

                
                new NamedBrush { Name = "Navy Blue", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#003049")) },
                new NamedBrush { Name = "Teal", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#008080")) },
                new NamedBrush { Name = "Emerald", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E8B57")) },
                new NamedBrush { Name = "Olive", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B8E23")) },
                new NamedBrush { Name = "Maroon", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#800000")) },
                new NamedBrush { Name = "Burgundy", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#800020")) },
                new NamedBrush { Name = "Slate Gray", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#708090")) },
                new NamedBrush { Name = "Midnight Blue", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#191970")) },
                new NamedBrush { Name = "Dark Cyan", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#008B8B")) },
                new NamedBrush { Name = "Light Coral", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F08080")) },
                new NamedBrush { Name = "Salmon", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FA8072")) },
                new NamedBrush { Name = "Peach", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDAB9")) },
                new NamedBrush { Name = "Lavender", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E6E6FA")) },
                new NamedBrush { Name = "Mint", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#98FF98")) },
                new NamedBrush { Name = "Ivory", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFF0")) },
                new NamedBrush { Name = "Beige", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5DC")) },
                new NamedBrush { Name = "Charcoal", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#36454F")) },
                new NamedBrush { Name = "Jet Black", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#343434")) },
                new NamedBrush { Name = "Rose Gold", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B76E79")) },
                new NamedBrush { Name = "Amber", Brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFBF00")) },
                new NamedBrush {Name="Navy Blue", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#003049"))},
                new NamedBrush {Name="Dark Green", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#005F73"))},
                new NamedBrush {Name="Dark Red", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9D0208"))},
                new NamedBrush {Name="Dark Purple", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5C4D7D"))},
                new NamedBrush {Name="Dark Brown", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4A3C31"))},
                new NamedBrush {Name="Dark Orange", Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D00000"))},
                new NamedBrush {Name="Light Pink",Brush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fdf0d5"))},

            };
        }

        private void SetupThemes()
        {
            var defaultFontFamily = new System.Windows.Media.FontFamily("Arial");
            int defaultFontSize = 60;
            AvailableThemes = new ObservableCollection<Theme>
            {
                new Theme { Name = "Dark (White on Black)", BackgroundBrush = System.Windows.Media.Brushes.Black, ForegroundBrush = System.Windows.Media.Brushes.White, FontFamily = defaultFontFamily, FontSize = defaultFontSize },
                new Theme { Name = "Light (Black on White)", BackgroundBrush = System.Windows.Media.Brushes.White, ForegroundBrush = System.Windows.Media.Brushes.Black, FontFamily = defaultFontFamily, FontSize = defaultFontSize },
                new Theme { Name = "Blue (White on Navy)", BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF001F3F")), ForegroundBrush = System.Windows.Media.Brushes.White, FontFamily = defaultFontFamily, FontSize = defaultFontSize },
                new Theme { Name = "Classic (Black on Yellow)", BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF5D6")), ForegroundBrush = System.Windows.Media.Brushes.Black, FontFamily = defaultFontFamily, FontSize = defaultFontSize },
                new Theme {Name="System Light", BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f1faee")),
                                                               ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1d3557")),FontFamily = defaultFontFamily, FontSize = defaultFontSize},
                new Theme
                    {
                        Name = "Midnight Blue",
                        BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#0A0F2B")),
                        ForegroundBrush = System.Windows.Media.Brushes.LightCyan,
                        FontFamily = defaultFontFamily,
                        FontSize = defaultFontSize
                    },
                new Theme
            {
                Name = "Dark Teal",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#003D3B")),
                ForegroundBrush = System.Windows.Media.Brushes.White,
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                                new Theme
            {
                Name = "Emerald Green",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#004D00")),
                ForegroundBrush = System.Windows.Media.Brushes.White,
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Crimson Red",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#4A0000")),
                ForegroundBrush = System.Windows.Media.Brushes.White,
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Charcoal",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#36454F")),
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Cream Paper",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFF9F0")),
                ForegroundBrush = System.Windows.Media.Brushes.Black,
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Light Mint",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#F0FFF4")),
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#1A2E22")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Blue Laser",
                BackgroundBrush = System.Windows.Media.Brushes.Black,
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#00B4FF")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Amber Glow",
                BackgroundBrush = System.Windows.Media.Brushes.Black,
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFA500")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Green Screen",
                BackgroundBrush = System.Windows.Media.Brushes.Black,
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FF00")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Theater Red",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E0000")),
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Slate Pro",
                BackgroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D3748")),
                ForegroundBrush = System.Windows.Media.Brushes.White,
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
            {
                Name = "Spotify Green",
                BackgroundBrush = System.Windows.Media.Brushes.Black,
                ForegroundBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1DB954")),
                FontFamily = defaultFontFamily,
                FontSize = defaultFontSize
            },
                new Theme
                {
                    Name="Peh Village (Light)",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e0fbfc")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#293241")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                { 
                    Name="Love is in the Air",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff0054")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize= defaultFontSize
                },
                new Theme
                {
                    Name="Peh Village (Dark)",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#293241")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e0fbfc")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },
                new Theme
                {
                    Name="Vibrant",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9e0059")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f8f9fa")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Sunset Glow",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff6f61")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Ocean Breeze",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00bcd4")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Forest Whisper",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4caf50")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Twilight Purple",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#673ab7")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Golden Hour",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffc107")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#212121")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Midnight Black",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#000000")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Pastel Dreams",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fce4ec")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#880e4f")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Retro Vibes",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffeb3b")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#212121")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Cyberpunk Neon",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#121212")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ffea")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Earthy Tones",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#795548")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Soft Pastels",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f8bbd0")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#880e4f")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Elegant Gray",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9e9e9e")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#212121")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Bright Citrus",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff9800")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#212121")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Cool Mint",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#b2dfdb")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#004d40")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Sunrise Orange",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff5722")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                new Theme
                {
                    Name="Twilight Blue",
                    BackgroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3f51b5")),
                    ForegroundBrush=new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff")),
                    FontFamily = defaultFontFamily,
                    FontSize = defaultFontSize
                },

                 new Theme
        {
            Name = "Sunrise Gradient",
            BackgroundBrush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(System.Windows.Media.Colors.Orange, 0.0),
                    new GradientStop(System.Windows.Media.Colors.Yellow, 0.5),
                    new GradientStop(System.Windows.Media.Colors.LightBlue, 1.0)
                }
            },
            ForegroundBrush = System.Windows.Media.Brushes.Black,
            FontFamily = defaultFontFamily,
             FontSize = defaultFontSize
        },

                 // Add these inside your AvailableThemes = new ObservableCollection<Theme> { ... };

new Theme
{
    Name = "Apple Big Sur Gradient",
    BackgroundBrush = new LinearGradientBrush
    {
        StartPoint = new System.Windows.Point(0, 1),
        EndPoint = new System.Windows.Point(1, 0),
        GradientStops = new GradientStopCollection
        {
            new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFC5C7D"), 0.0), // Pink
            new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF6A82FB"), 0.5), // Blue
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF36D1C4"), 1.0)  // Teal
        }
    },
    ForegroundBrush = System.Windows.Media.Brushes.White,
    FontFamily = defaultFontFamily,
    FontSize = defaultFontSize
},

new Theme
{
    Name = "Sunset Beach",
    BackgroundBrush = new LinearGradientBrush
    {
        StartPoint = new System.Windows.Point(0, 0),
        EndPoint = new System.Windows.Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFFE8C00"), 0.0), // Orange
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFF83600"), 0.5), // Red
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFEE0979"), 1.0)  // Pink
        }
    },
    ForegroundBrush = System.Windows.Media.Brushes.White,
    FontFamily = defaultFontFamily,
    FontSize = defaultFontSize
},

new Theme
{
    Name = "Aurora Borealis",
    BackgroundBrush = new LinearGradientBrush
    {
        StartPoint = new System.Windows.Point(0, 0),
        EndPoint = new System.Windows.Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF00C3FF"), 0.0), // Light Blue
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFC00"), 0.5), // Yellow
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FFFF1B6B"), 1.0)  // Pink
        }
    },
    ForegroundBrush = System.Windows.Media.Brushes.Black,
    FontFamily = defaultFontFamily,
    FontSize = defaultFontSize
},

new Theme
{
    Name = "Deep Space",
    BackgroundBrush = new LinearGradientBrush
    {
        StartPoint = new System.Windows.Point(0, 0),
        EndPoint = new System.Windows.Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF232526"), 0.0), // Dark Gray
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF414345"), 1.0)  // Lighter Gray
        }
    },
    ForegroundBrush = System.Windows.Media.Brushes.White,
    FontFamily = defaultFontFamily,
    FontSize = defaultFontSize
},

new Theme
{
    Name = "Minty Fresh",
    BackgroundBrush = new LinearGradientBrush
    {
        StartPoint = new System.Windows.Point(0, 0),
        EndPoint = new System.Windows.Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF43E97B"), 0.0), // Mint Green
            new GradientStop((System.Windows.Media.Color ) System.Windows.Media.ColorConverter.ConvertFromString("#FF38F9D7"), 1.0)  // Aqua
        }
    },
    ForegroundBrush = System.Windows.Media.Brushes.Black,
    FontFamily = defaultFontFamily,
    FontSize = defaultFontSize
},
            };


    //       
        }

        private bool CanGoLive(object parameter) => SelectedPart != null;

        private void GoLive(object parameter)
        {
            if (SelectedPart != null)
            {
                ProjectorViewModel.IsShowingImage = false;
                ProjectorViewModel.LiveContent = SelectedPart.Lyrics;
                ProjectorViewModel.IsScreenBlack = false;
                _liveSong = SelectedSong;
            }
        }

        private void ClearScreen(object parameter)
        {
            ProjectorViewModel.LiveContent = string.Empty;
            ProjectorViewModel.IsScreenBlack = false;
            ProjectorViewModel.IsShowingImage = false;
            _liveSong = null;
            if (_isVideoModeActive) { StopVideo(null); }
        }

        private void BlankScreen(object parameter) { ProjectorViewModel.IsScreenBlack = !ProjectorViewModel.IsScreenBlack; }

        private void LoadImage(object p)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Title = "Select a Program Order Image", Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true)
            {
                
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    // The Freeze call forces the image to load immediately. If it's not a valid image, it will throw an exception.
                    bitmap.Freeze();

                    ProjectorViewModel.ImageSource = bitmap;
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("The selected file is not a valid or supported image. Please select a JPG, PNG, or BMP file.", "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanShowImage(object p) => ProjectorViewModel.ImageSource != null;

        private void ShowImage(object p)
        {
            ProjectorViewModel.IsShowingImage = true;
            ProjectorViewModel.IsScreenBlack = false;
            _liveSong = null;
            ProjectorViewModel.IsVideoVisible = false;
        }

        private void ShowLyrics(object p)
        {
            ProjectorViewModel.IsShowingImage = false;
            if (_isVideoModeActive) { ProjectorViewModel.IsVideoVisible = true; }
            if (CanGoLive(p)) { GoLive(p); }
        }

        private void LoadData()
        {
            var songsFromDb = _repository.GetAllSongs();
            _allSongs = new ObservableCollection<Song>(songsFromDb);
            FilteredSongs = new ObservableCollection<Song>(_allSongs);
        }

        private void FilterSongList()
        {
            FilteredSongs.Clear();
            if (string.IsNullOrWhiteSpace(SearchText)) { foreach (var song in _allSongs) FilteredSongs.Add(song); }
            else
            {
                string lowerCaseFilter = SearchText.ToLower();
                var filtered = _allSongs.Where(s => s.Title.ToLower().Contains(lowerCaseFilter) || s.SongID.ToString().Contains(lowerCaseFilter));
                foreach (var song in filtered) FilteredSongs.Add(song);
            }
        }

        
       
    }
}
