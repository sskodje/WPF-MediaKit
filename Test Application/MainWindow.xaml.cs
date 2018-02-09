using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using WPFMediaKit.DirectShow.Controls;

namespace Test_Application
{
    public partial class MainWindow : Window
    {
        string durationString;
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.mediaUriElement.MediaFailed += MediaUriElement_MediaFailed;
            this.mediaUriElement.MediaOpened += MediaUriElement_MediaOpened;
            this.mediaUriElement.MediaPlaybackStateChanged += MediaUriElement_MediaPlaybackStateChanged;
            this.mediaUriElement.MediaPositionChanged += MediaUriElement_MediaPositionChanged;
            this.mediaUriElement.MediaEnded += MediaUriElement_MediaEnded;
            this.mediaUriElement.MouseLeftButtonDown += mediaUriElement_MouseLeftButtonDown;
        }

        private void MediaUriElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            //mediaUriElement.MediaPosition = 0;
        }

        private void MediaUriElement_MediaPositionChanged(object sender, MediaPositionChangedEventArgs e)
        {
            tbProgress.Text = string.Format("{0} / {1}", TimeSpan.FromTicks(e.MediaPosition).ToString(@"mm\:ss"), durationString);
        }

        private void MediaUriElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            durationString = TimeSpan.FromTicks(mediaUriElement.MediaDuration).ToString(@"mm\:ss");

            ResizeAndCenterWindow();
        }
        private void MediaUriElement_MediaPlaybackStateChanged(object sender, MediaStateChangedEventArgs e)
        {
            switch (e.MediaState)
            {
                case WPFMediaKit.DirectShow.MediaPlayers.MediaState.Play:
                    tbStatus.Text = "Playing";
                    break;
                case WPFMediaKit.DirectShow.MediaPlayers.MediaState.Stop:
                    tbStatus.Text = "Stopped";
                    break;
                case WPFMediaKit.DirectShow.MediaPlayers.MediaState.Pause:
                    tbStatus.Text = "Paused";
                    break;
                case WPFMediaKit.DirectShow.MediaPlayers.MediaState.Close:
                    tbStatus.Text = "Closed";
                    break;
                default:
                    break;
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result != true)
                return;
            errorText.Text = null;
            mediaUriElement.Source = new Uri(dlg.FileName);
            tbStatus.Text = "Opening...";
        }

        private void MediaUriElement_MediaFailed(object sender, WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => errorText.Text = e.Message));
            tbStatus.Text = "Failed to open media";
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mediaUriElement.Close();
        }

        private void btnOpenUri_Click(object sender, RoutedEventArgs e)
        {
            UriInputWindow window = new UriInputWindow();
            bool? success = window.ShowDialog();
            if (success.GetValueOrDefault() == true)
            {

                if (Uri.IsWellFormedUriString(window.InputText, UriKind.Absolute))
                {
                    Uri uri = new Uri(window.InputText);
                    mediaUriElement.Source = uri;
                    tbStatus.Text = "Opening...";
                }
            }
        }

        private void btnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            OpenFullscreen();
        }
        private void FullscreenWin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FullscreenWindow fullscreenWin = (FullscreenWindow)sender;

            var videoElement = fullscreenWin.DetachVideoControl();
            videoElement.MouseLeftButtonDown += mediaUriElement_MouseLeftButtonDown;
            long position = videoElement.MediaPosition;
            Debug.WriteLine("Closing fullscreen, media has position  {0}", position);
            this.VideoContainerPanel.Children.Add(videoElement);
        }

        private void OpenFullscreen()
        {
            long position = mediaUriElement.MediaPosition;
            FullscreenWindow fullscreenWin = new FullscreenWindow();
            var videoElement = this.mediaUriElement;
            videoElement.MouseLeftButtonDown -= mediaUriElement_MouseLeftButtonDown;
            this.VideoContainerPanel.Children.Remove(videoElement);
            fullscreenWin.SetVideoControl(videoElement);
            Debug.WriteLine("Playing fullscreen video, media has status  {0}", videoElement.MediaPlaybackState);
            fullscreenWin.Closing += FullscreenWin_Closing;
            fullscreenWin.Show();
        }
        private void ResizeAndCenterWindow()
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            double heightRatio = this.mediaUriElement.NaturalVideoWidth / this.Width;

            double newHeight = Math.Min(screen.WorkingArea.Height, this.Height * heightRatio);
            double newWidth;
            if (newHeight == screen.WorkingArea.Height)
            {
                double widthRatio = newHeight / (this.Height * heightRatio);
                newWidth = this.mediaUriElement.NaturalVideoWidth * widthRatio;
            }
            else
            {
                newWidth = this.mediaUriElement.NaturalVideoWidth;
            }
            this.Height = newHeight;
            this.Width = newWidth;
            this.Left = Math.Max(0, (screen.WorkingArea.Width - this.Width) / 2);
            this.Top = Math.Max(0, (screen.WorkingArea.Height - this.Height) / 2);
        }
        private void mediaUriElement_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OpenFullscreen();
            }
        }
    }
}