using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace BackgroundThumbnailLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<ThumbnaillnCell> ThumbnailInCells = new List<ThumbnaillnCell>();
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Button Callbacks
        // This callback does all the work. It clears and then creates cells of a given size in the grid,
        // and then populates each cell in the grid with a thumbnailInCell instance creating an 'empty' thumbnail 
        // It then invokes various loadThumbnails methods which actually reads in and populates the thumbnails from files.
        private void ButtonLoadThumbnails_Click(object sender, RoutedEventArgs e)
        {
            Tuple<int,int> gridSize = this.RecreateGrid(Convert.ToInt32(this.SliderThumbnailSize.Value));

            if (sender is Button button)
            {
                if (button == this.ButtonSequential)
                {
                    this.ThumbnailViewer.LoadThumbnailsSynchronous(gridSize.Item1, gridSize.Item2, false, this.RBFFMPeghumbnailer.IsChecked == true);
                }
                else if (button == this.ButtonWorker)
                {
                    this.ThumbnailViewer.LoadThumbnailsWorker(gridSize.Item1, gridSize.Item2, this.CBTwoPass.IsChecked == true, this.RBFFMPeghumbnailer.IsChecked == true);
                }
                else
                {
                    System.Windows.MessageBox.Show("Button not recognized");
                }
            }
        }

        private Tuple<int,int> RecreateGrid(int desiredWidth)
        {
            this.CancelThumbnailUpdate();
            this.ThumbnailInCells.Clear();

            // Get the desired grid width from the sliders, and calculate how many we can fit into the current grid.
            // For now, we use a fixed aspect ratio. In practice, we need to take the actual aspect ratio of thumbnails into account.
            // What makes this problematic is that different thumbnails displayed in the grid may have different aspect ratios e.g., the odd panoramic thumbnail
            double aspectRatio = 480.0 / 640.0;
            int gridWidth = desiredWidth;
            int gridHeight = Convert.ToInt32(gridWidth * aspectRatio);

            // Calculated the number of rows / columns that can fit into the available space,
            this.ThumbnailViewer.CreateCellsToFitSpace(gridWidth, gridHeight);

            // Get as many file paths to source images and videos as there are cells in the grid
            List<string> filePaths = ImageAndVideoFiles.GetPaths(this.ThumbnailViewer.NumberOfCells);

            // Create and add empty thumbnails to each cell in the grid. 
            // Also create a thumbnailInCell object that references the thumbnail and its position in the grid.
            // Store all thumbnailInCell objects in a list called thumbnailInCell 
            int position = 0;
            for (int thisRow = 0; thisRow < this.ThumbnailViewer.NumberOfRows; thisRow++)
            {
                for (int thisColumn = 0; thisColumn < this.ThumbnailViewer.NumberOfColumns; thisColumn++)
                {
                    if (position >= filePaths.Count)
                    {
                        // We don't have enough thumbnails to fill the grid
                        break;
                    }
                    // Add an empty thumbnail to the grid set at a particular row/column position
                    Image thumbnail = CreateEmptyThumbnailAt(thisRow, thisColumn);
                    this.ThumbnailViewer.Grid.Children.Add(thumbnail);

                    // Create an thumbnailInCell instance, which we use as a reference to that thumbnail, its path and its location in the grid so we can find it later.
                    ThumbnaillnCell thumbnailInCell = new ThumbnaillnCell
                    {
                        Image = thumbnail,
                        Position = position,
                        Path = filePaths[position]
                    };
                    position++;
                    ThumbnailInCells.Add(thumbnailInCell);
                }
            }
            // Pass this on to the ThumbnailViewer so it can display the thumbnail at a given location
            this.ThumbnailViewer.ThumbnailInCells = this.ThumbnailInCells;
            return new Tuple<int,int>(gridWidth, gridHeight);
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.CancelThumbnailUpdate();
            this.ThumbnailViewer.ClearGrid();
        }

        // Cancel the current asynchronous operation
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelThumbnailUpdate();
        }

        private void CancelThumbnailUpdate()
        {
            if (this.ThumbnailViewer != null)
            {
                this.ThumbnailViewer.CancelUpdate();
            }
        }
        #endregion

        #region Utilities
        private Image CreateEmptyThumbnailAt(int row, int column)
        {
            Image image = new Image();
            Grid.SetRow(image, row);
            Grid.SetColumn(image, column);
            return image;
        }
        #endregion

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                this.SliderThumbnailSize.Value = Math.Min(this.SliderThumbnailSize.Value + 50, this.SliderThumbnailSize.Maximum);
            }
            else if (e.Delta < 0)
            {
                this.SliderThumbnailSize.Value = Math.Max(this.SliderThumbnailSize.Value - 50, this.SliderThumbnailSize.Minimum);
            }
            Tuple<int, int> gridSize = this.RecreateGrid(Convert.ToInt32(this.SliderThumbnailSize.Value));
            this.ThumbnailViewer.LoadThumbnailsWorker(gridSize.Item1, gridSize.Item2, this.CBTwoPass.IsChecked == true, this.RBFFMPeghumbnailer.IsChecked == true);
        }
    }
}
