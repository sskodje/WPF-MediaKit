using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFMediaKit.DirectShow.Controls;

namespace Test_Application
{
    /// <summary>
    /// Interaction logic for FullscreenWindow.xaml
    /// </summary>
    public partial class FullscreenWindow : Window
    {
        public FullscreenWindow()
        {
            InitializeComponent();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        public void SetVideoControl(MediaElementBase element)
        {
            element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            this.LayoutRoot.Children.Clear();
                this.LayoutRoot.Children.Add(element);
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            LayoutRoot.Width = screen.Bounds.Width;
            LayoutRoot.Height = screen.Bounds.Height;
            this.Left = 0;
            this.Top = 0;
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.Close();
            }
        }

        internal MediaSeekingElement DetachVideoControl()
        {
            foreach (UIElement element in this.LayoutRoot.Children)
            {
                if (element is MediaSeekingElement)
                {
                    element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
                    this.LayoutRoot.Children.Remove(element);
                    return element as MediaSeekingElement;
                }
            }
            return null;
        }
    }
}
