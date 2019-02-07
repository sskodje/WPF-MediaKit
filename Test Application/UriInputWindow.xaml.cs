using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Test_Application
{
    /// <summary>
    /// Interaction logic for UriInputWindow.xaml
    /// </summary>
    public partial class UriInputWindow : Window
    {
        public string VideoInputText { get; set; }

        public string AudioInputText { get; set; }

        public UriInputWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void bnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void bnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private void bnAddAudioTrack_Click(object sender, RoutedEventArgs e)
        {
            this.AudioTrackPanel.Visibility = Visibility.Visible;
            ((Button)sender).Visibility = Visibility.Collapsed;
        }
    }
}
