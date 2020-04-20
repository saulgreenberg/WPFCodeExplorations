using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private void ButtonLoadImages_Click(object sender, RoutedEventArgs e)
        {
            // Clear everything so we can start afresh
            this.ImageViewer.ClearGrid();
            this.ImageInCells.Clear();

            // Get the desired grid width from the sliders, and calculate how many we can fit into the current grid.
            int gridWidth = Convert.ToInt32(this.SliderImageSize.Value);
            int gridHeight = gridWidth;

            // Calculated the number of rows / columns that can fit into the available space,
            Rect gridSize = new Rect(0, 0, this.ImageViewer.ActualWidth, this.ImageViewer.ActualHeight);
            this.ImageViewer.MaxColumns = Convert.ToInt32(gridSize.Width / gridWidth);
            this.ImageViewer.MaxRows = Convert.ToInt32(gridSize.Height / gridHeight);

            // Get as many images as there are cells in the grid, which will be placed in JpgImagePaths.Paths
            int numberOfCells = this.ImageViewer.MaxColumns * this.ImageViewer.MaxRows;
            this.JpgImagePaths.FindPaths(numberOfCells);

            // Add cells to the grid by first adding as many columns as can fit into the available space, then as many rows.
            for (int thisColumn = 0; thisColumn < this.ImageViewer.MaxColumns; thisColumn++)
            {
                this.ImageViewer.Grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition() { Width = new GridLength(gridWidth, GridUnitType.Pixel) });
            }
            for (int thisRow = 0; thisRow < this.ImageViewer.MaxRows; thisRow++)
            {
                this.ImageViewer.Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(gridHeight, GridUnitType.Pixel) });
            }

            // Create and add empty images to each cell in the grid. 
            // Also create an imageInCell object that references the image and its position in the grid.
            // Store all imageInCell objects in a list called ImageInCells 
            int position = 0;
            for (int thisRow = 0; thisRow < this.ImageViewer.MaxRows; thisRow++)
            {
                for (int thisColumn = 0; thisColumn < this.ImageViewer.MaxColumns; thisColumn++)
                {
                    if (position >= this.JpgImagePaths.Paths.Count)
                    {
                        // We don't have enough images to fill the grid
                        break;
                    }
                    // Add an empty image to the grid at a particular row/column position
                    Image image = new Image();
                    Grid.SetRow(image, thisRow);
                    Grid.SetColumn(image, thisColumn);
                    this.ImageViewer.Grid.Children.Add(image);

                    // Create an imageInCell object, which we use as a reference to that image and its location so we can find it later.
                    ImageInCell imageInCell = new ImageInCell
                    {
                        Row = thisRow,
                        Column = thisColumn,
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
            if (this.ImageViewer?.BackgroundWorker != null)
            { 
                this.ImageViewer.BackgroundWorker.CancelAsync();
            }
        }
    }
}
