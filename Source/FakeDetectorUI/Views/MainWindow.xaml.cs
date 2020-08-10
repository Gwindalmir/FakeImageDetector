using Gwindalmir.FakeDetectorUI.ViewModels;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gwindalmir.FakeDetectorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            dialog.Filter = "JPEG Images (*.jpg)|*.jpg;*.jpe;*.jfif|PNG Images (*.png)|*.png|All Images|*.jpg;*.jpe;*.jfif;*.png";
            if(dialog.ShowDialog() == true)
            {
                (DataContext as MainWindowViewModel).Filename = dialog.FileName;
                tabControl.SelectedIndex = 0;
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                (DataContext as MainWindowViewModel).ClearConfusionMatrices();
                (DataContext as MainWindowViewModel).CalculateConfusionMatrix(dialog.FileName);
                tabControl.SelectedIndex = 1;
            }
        }

        private void ClassifyButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as MainWindowViewModel).ClassLabel = (DataContext as MainWindowViewModel).CalculateOverallClass();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var dc = DataContext as MainWindowViewModel;
            if(dc.OriginalImage != null)
            {
                var imgWidth = dc.OriginalImage.PixelWidth;
                var imgHeight = dc.OriginalImage.PixelHeight;
                var aspect = (float)imgWidth / imgHeight * 2;
                var offset = 120;

                if (aspect > 0)
                {
                    if (e.HeightChanged)
                        Width = (e.NewSize.Height - offset) * aspect;
                    if (e.WidthChanged)
                        Height = (e.NewSize.Width * (1 / aspect)) + offset;

                    //e.Handled = true;
                }
            }
        }

        private void Image_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            const int maxWidth = 700;
            const int maxHeight = 800;

            var image = sender as Image;
            if (image.Source != null)
            {
                var width = image.Source.Width;
                var height = image.Source.Height;
                var aspect = width / height;

                var resizedWidth = width;
                var resizedHeight = height;

                if (width < height && height > maxHeight)
                {
                    resizedHeight = maxHeight;
                    resizedWidth = maxHeight * aspect;
                }
                else if (width > height && width > maxWidth)
                {
                    resizedWidth = maxWidth;
                    resizedHeight = maxWidth * (1 / aspect);
                }
                else if (width == height)
                {
                    if (width > maxWidth)
                    {
                        resizedWidth = maxWidth;
                        resizedHeight = maxHeight;
                    }
                }

                // Resize window to fit
                Height = Height - image.DesiredSize.Height + resizedHeight;
                Width = Width - image.DesiredSize.Width + resizedWidth * 2;
            }
        }
    }
}
