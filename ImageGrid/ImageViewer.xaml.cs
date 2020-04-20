using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BackgroundImageLoader
{
    /// <summary>
    /// Interaction logic for ClickableImagesGrid.xaml
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        public int NumberOfRows { get; set; }
        public int NumberOfColumns { get; set; }
        public int NumberOfCells
        {
            get
            {
                return NumberOfRows * NumberOfColumns;
            }
        }
        // Required so we can perform a cancel
        private BackgroundWorker BackgroundWorker;

        // Need to set this from the calling method before any loading is done every time the grid size is changed
        public List<ImageInCell> ImageInCells { get; set; }

        public Grid MyGrid { get; set; }

        #region Constructor
        public ImageViewer()
        {
            InitializeComponent();
            MyGrid = this.Grid;
        }
        #endregion

        #region Syncronous image loader
        // The completely synchronous version. Images won't be displayed until the operation is done.
        public void LoadImagesSynchronous(Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool _)
        {
            // Add images
            foreach (ImageInCell imageInCell in this.ImageInCells)
            {
                BitmapImage bitmapImage = this.GetBitmapFromFile(imageInCell.Path, desiredWidth, desiredHeight);
                LoadImageProgressStatus lip = new LoadImageProgressStatus
                {
                    ImageInCell = this.ImageInCells[imageInCell.Position],
                    BitmapImage = bitmapImage,
                    Position = imageInCell.Position,
                };
                this.UpdateImageLoadProgress(lip);
            }
        }
        #endregion

        #region Async image loader using background worker


        public void LoadImagesWorker(Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool useTwoPasses)
        {

            LoadImageProgressStatus loadImageProgress = new LoadImageProgressStatus();

            // BackgroundWorker backgroundWorker = new BackgroundWorker
            this.BackgroundWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            this.BackgroundWorker.DoWork += (ow, ea) =>
            {
                if (useTwoPasses)
                {
                    // Add very low resultion versions of each image on the first pass
                    foreach (ImageInCell imageInCell in this.ImageInCells)
                    {
                        if (this.BackgroundWorker.CancellationPending == true)
                        {
                            ea.Cancel = true;
                            return;
                        }
                        //Thread.Sleep(1);
                        LoadImageProgressStatus lip = new LoadImageProgressStatus
                        {
                            ImageInCell = this.ImageInCells[imageInCell.Position],
                            BitmapImage = this.GetBitmapFromFileAsync(imageInCell.Path, desiredWidth / 2, desiredHeight / 2).Result,
                            Position = imageInCell.Position,
                        };
                        this.BackgroundWorker.ReportProgress(0, lip);
                    }
                }

                // Add a better resultion version of each image, where the resolution matches the grid size
                foreach (ImageInCell imageInCell in this.ImageInCells)
                {
                    if (this.BackgroundWorker.CancellationPending == true)
                    {
                        ea.Cancel = true;
                        return;
                    }
                    LoadImageProgressStatus lip = new LoadImageProgressStatus
                    {
                        ImageInCell = this.ImageInCells[imageInCell.Position],
                        //BitmapImage = this.GetBitmapFromFile(imageInCell.Path, desiredWidth, desiredHeight),
                        BitmapImage = this.GetBitmapFromFileAsync(imageInCell.Path, desiredWidth, desiredHeight).Result,
                        Position = imageInCell.Position,
                    };
                    this.BackgroundWorker.ReportProgress(0, lip);
                }
            };

            this.BackgroundWorker.ProgressChanged += (o, ea) =>
                {
                    // this gets called on the UI thread
                    LoadImageProgressStatus lip = (LoadImageProgressStatus)ea.UserState;
                    this.UpdateImageLoadProgress(lip);
                };

            this.BackgroundWorker.RunWorkerCompleted += (o, ea) =>
                    {
                        Mouse.OverrideCursor = null;

                        // All images should be loaded by now
                        // BackgroundWorker aborts execution on an exception and transfers it to completion for handling
                        if (ea.Error != null)
                        {
                            throw new FileLoadException("Image loading failed unexpectedly.  See inner exception for details.", ea.Error);
                        }
                    };
            Mouse.OverrideCursor = Cursors.Wait;
            // Get the images
            this.BackgroundWorker.RunWorkerAsync();
        }
        #endregion

        #region GetBitmapFromFile: Sync and Async versions
        // Given a filepath, return a BitmapImage of the desired width / height
        // Async and sync versions below
        public Task<BitmapImage> GetBitmapFromFileAsync(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight)
        {
            // Should probably replace this with true async file retrieval, if I can figure out how to do that.
            return Task.Run(() =>
            {
                // Note to bridge the gap between the out parameter and the requirements of the task, this uses
                // a tuple to carry both.
                if (File.Exists(path) == false)
                {
                    return null;
                }
                return this.GetBitmapFromFile(path, desiredWidth, desiredHeight);
            });
        }

        public virtual BitmapImage GetBitmapFromFile(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight)
        {
            BitmapCacheOption bitmapCacheOption = BitmapCacheOption.None;
            BitmapImage bitmap = new BitmapImage();
            try
            {
                bitmap.BeginInit();
                bitmap.DecodePixelWidth = desiredWidth.Value;
                bitmap.DecodePixelHeight = desiredHeight.Value;
                bitmap.CacheOption = bitmapCacheOption;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                Debug.Print("Could not load: " + path);
                return null;
            }
        }

        #endregion

        #region Public utility methods
        public void ClearGrid()
        {
            // Clear the Grid so we can start afresh
            this.Grid.RowDefinitions.Clear();
            this.Grid.ColumnDefinitions.Clear();
            this.Grid.Children.Clear();
        }

        public void CreateCellsToFitSpace(int desiredCellWidth, int desiredCellHeight)
        {
            // Clear everything as we need to start afresh
            this.ClearGrid();

            // Calculated the number of rows / columns that can fit into the available space,);
            this.NumberOfColumns = Convert.ToInt32(this.ActualWidth / desiredCellWidth);
            this.NumberOfRows = Convert.ToInt32(this.ActualHeight / desiredCellHeight);

            // Add cells to the grid by first adding as many columns as can fit into the available space, then as many rows.
            for (int thisColumn = 0; thisColumn < this.NumberOfColumns; thisColumn++)
            {
                this.Grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition() { Width = new GridLength(desiredCellWidth, GridUnitType.Pixel) });
            }
            for (int thisRow = 0; thisRow < this.NumberOfRows; thisRow++)
            {
                this.Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(desiredCellHeight, GridUnitType.Pixel) });
            }
        }
        #endregion

        #region Progress
        private void UpdateImageLoadProgress(LoadImageProgressStatus lip)
        {
            if (lip.BitmapImage != null)
            {
                this.ImageInCells[lip.ImageInCell.Position].Image.Source = lip.BitmapImage;
            }
            else
            {
                System.Windows.MessageBox.Show(String.Format("No bitmap image available at position {0}", lip.ImageInCell.Position));
            }
        }

        public void CancelUpdate()
        {
            try
            {
                if (this.BackgroundWorker != null)
                {
                    this.BackgroundWorker.CancelAsync();
                }
            }
            catch { }
        }
        #endregion
    }

    #region Class: LoadImageProgressStatus
    // Used by ReportProgress to pass specific values to Progress Changed as a parameter 
    internal class LoadImageProgressStatus
    {
        public ImageInCell ImageInCell { get; set; }
        public BitmapImage BitmapImage { get; set; }
        public int Position { get; set; }

        public LoadImageProgressStatus()
        {
            this.ImageInCell = null;
            this.BitmapImage = null;
            this.Position = 0;
        }
    }
    #endregion
}
