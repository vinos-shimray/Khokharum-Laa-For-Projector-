
using KhokharumLaa.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace KhokharumLaa
{
    public partial class ProjectorWindow : Window
    {
        private bool isPlayerAReady = false;
        private bool isPlayerBReady = false;

        public ProjectorWindow()
        {
            InitializeComponent();

            // Hook up all media events 
            DataContextChanged += ProjectorWindow_DataContextChanged;
            VideoPlayerA.MediaOpened += Player_MediaOpened;
            VideoPlayerB.MediaOpened += Player_MediaOpened;
            VideoPlayerA.MediaEnded += Player_MediaEnded;
            VideoPlayerB.MediaEnded += Player_MediaEnded;
        }

        private void ProjectorWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ProjectorViewModel viewModel)
            {
              
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                // Handle the initial video source when the window first loads
                UpdateVideoSource(viewModel);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
          
            if (e.PropertyName == nameof(ProjectorViewModel.VideoSource))
            {
                UpdateVideoSource(sender as ProjectorViewModel);
            }
        }

        private void UpdateVideoSource(ProjectorViewModel viewModel)
        {
            if (viewModel == null) return;

          
            VideoPlayerA.Stop();
            VideoPlayerB.Stop();
            VideoPlayerA.BeginAnimation(OpacityProperty, null);
            VideoPlayerB.BeginAnimation(OpacityProperty, null);

          
            isPlayerAReady = false;
            isPlayerBReady = false;
            VideoPlayerA.Opacity = 1;
            VideoPlayerB.Opacity = 0;


            if (viewModel.VideoSource != null)
            {
             
                VideoPlayerA.Source = viewModel.VideoSource;
                VideoPlayerB.Source = viewModel.VideoSource;
            }
            else
            {
                
                VideoPlayerA.Source = null;
                VideoPlayerB.Source = null;
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (sender == VideoPlayerA)
            {
                isPlayerAReady = true;
              
                VideoPlayerA.Play();
            }
            else if (sender == VideoPlayerB)
            {
                isPlayerBReady = true;
               
                PrimePlayer(VideoPlayerB);
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            // When a player ends, cross-fade to the other,
            if (sender == VideoPlayerA && isPlayerBReady)
            {
                Crossfade(VideoPlayerA, VideoPlayerB);
            }
            else if (sender == VideoPlayerB && isPlayerAReady)
            {
                Crossfade(VideoPlayerB, VideoPlayerA);
            }
        }

        private void Crossfade(MediaElement fadeOutPlayer, MediaElement fadeInPlayer)
        {
            // Start the next player. 


            fadeInPlayer.Position = TimeSpan.Zero;
            fadeInPlayer.Play();

            // Set up the fade animations
            var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(5));
            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(5));

            
            PrimePlayer(fadeOutPlayer);

            // Apply the animations to start the cross-fade
            fadeOutPlayer.BeginAnimation(OpacityProperty, fadeOutAnimation);
            fadeInPlayer.BeginAnimation(OpacityProperty, fadeInAnimation);
        }


        /// <summary>
        /// Primes a MediaElement by playing and immediately pausing it.
        /// This forces it to buffer the media 
        /// </summary>
        private void PrimePlayer(MediaElement player)
        {
            player.Position = TimeSpan.Zero;
            player.Play();
            player.Pause();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
