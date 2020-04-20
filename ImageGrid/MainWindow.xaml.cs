using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BackgroundImageLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly JPGImagePaths JpgImagePaths = new JPGImagePaths();
        private readonly List<ImageInCell> ImageInCells = new List<ImageInCell>();
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Button Callbacks
        // This callback does all the work. It clears and then creates cells of a given size in the grid,
        // and then populates each cell in the grid with an imageInCell instance creating an 'empty' image 
        // It then invokes various loadImages methods which actually reads in and populates the images from files.
        private void ButtonLoadImages_Click(object sender, RoutedEventArgs e)
        {
            // Start afresh
            this.ImageInCells.Clear();

            // Get the desired grid width from the sliders, and calculate how many we can fit into the current grid.
            // For now, we use a fixed aspect ratio. In practice, we need to take the actual aspect ratio of images into account.
            // What makes this problematic is that different images displayed in the grid may have different aspect ratios e.g., the odd panoramic images
            double aspectRatio = 480.0 / 640.0;
            int gridWidth = Convert.ToInt32(this.SliderImageSize.Value);
            int gridHeight = Convert.ToInt32(gridWidth * aspectRatio);

            // Calculated the number of rows / columns that can fit into the available space,
            this.ImageViewer.CreateCellsToFitSpace(gridWidth, gridHeight);

            // Get as many images as there are cells in the grid, which will be placed in JpgImagePaths.Paths
            this.JpgImagePaths.FindPaths(this.ImageViewer.NumberOfCells);

            // Create and add empty images to each cell in the grid. 
            // Also create an imageInCell object that references the image and its position in the grid.
            // Store all imageInCell objects in a list called ImageInCells 
            int position = 0;
            for (int thisRow = 0; thisRow < this.ImageViewer.NumberOfRows; thisRow++)
            {
                for (int thisColumn = 0; thisColumn < this.ImageViewer.NumberOfColumns; thisColumn++)
                {
                    if (position >= this.JpgImagePaths.Paths.Count)
                    {
                        // We don't have enough images to fill the grid
                        break;
                    }
                    // Add an empty image to the grid set at a particular row/column position
                    Image image = CreateEmptyImageAt(thisRow, thisColumn);
                    this.ImageViewer.Grid.Children.Add(image);

                    // Create an imageInCell object, which we use as a reference to that image, its path and its location in the grid so we can find it later.
                    ImageInCell imageInCell = new ImageInCell
                    {
                        Image = image,
                        Position = position,
                        Path = this.JpgImagePaths.Paths[position]
                    };
                    position++;
                    ImageInCells.Add(imageInCell);
                }
            }

            // Pass this on to the ImageViewer so it can display the images at a given location
            this.ImageViewer.ImageInCells = this.ImageInCells;

            if (sender is Button button)
            {
                if (button == this.ButtonSequential)
                {
                    this.ImageViewer.LoadImagesSynchronous(gridWidth, gridHeight, false);
                }
                else if (button == this.ButtonWorker)
                {
                    this.ImageViewer.LoadImagesWorker(gridWidth, gridHeight, this.CBTwoPass.IsChecked == true);
                }
                else
                {
                    System.Windows.MessageBox.Show("Button not recognized");
                }
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.ImageViewer.ClearGrid();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImageViewer != null)
            {
                this.ImageViewer.CancelUpdate();
            }
        }
        #endregion

        #region Utilities
        private Image CreateEmptyImageAt(int row, int column)
        {
            Image image = new Image();
            Grid.SetRow(image, row);
            Grid.SetColumn(image, column);
            return image;
        }
        #endregion
    }
}
