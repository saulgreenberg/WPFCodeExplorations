using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using ImageProcessor;
using ImageProcessor.Imaging.Formats;

namespace ImageTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ImageManipulationControls imc;
        string img1Path;
        string img2Path;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Assumes that the app runs in Debug or Release, and that the image is in ..\..\img.jpg
            string path = AppDomain.CurrentDomain.BaseDirectory;
            path = System.IO.Path.GetFullPath(System.IO.Path.Combine(path, @"..\..\"));
            img1Path = System.IO.Path.Combine(path, "img1.jpg");
            img2Path = System.IO.Path.Combine(path, "img2.jpg");

            this.SourceImage.Source = new BitmapImage(new Uri(img1Path));
            this.ShowControlsWindow();
        }


        private void ShowControlsWindow()
        {
            imc = new ImageManipulationControls(this);
            imc.ManipulatedImage = this.ManipulatedImage;
            imc.ManipulatedImagePath = img1Path;
            imc.Show();
        }
        private void SetImage(string path)
        {
            imc.ManipulatedImagePath = path;
            this.SourceImage.Source = new BitmapImage(new Uri(path));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Name == "Image1")
            {
                this.SetImage(img1Path);
            }
            else if (button.Name == "Image2")
            {
                this.SetImage(img2Path); 
            }
        }

        private void ShowControls_Click(object sender, RoutedEventArgs e)
        {
            if (imc.IsActive == false)
            {
                this.ShowControlsWindow();
            }
        }
    }
}
