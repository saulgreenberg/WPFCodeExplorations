using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BackgroundThumbnailLoader
{
    /// <summary>
    /// Interaction logic for ThumbnailViewer
    /// </summary>
    public partial class ThumbnailViewer : UserControl
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
        public List<ThumbnaillnCell> ThumbnailInCells { get; set; }

        public Grid MyGrid { get; set; }

        #region Constructor
        public ThumbnailViewer()
        {
            InitializeComponent();
            MyGrid = this.Grid;
        }
        #endregion

        #region Syncronous image loader
        // The completely synchronous version. Images won't be displayed until the operation is done.
        public void LoadThumbnailsSynchronous(Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool _, bool useFFMpeg)
        {
            // Add images
            foreach (ThumbnaillnCell imageInCell in this.ThumbnailInCells)
            {
                BitmapImage bitmapImage = Thumbnailer.GetThumbnailFromFile(imageInCell.Path, desiredWidth, desiredHeight, useFFMpeg);
                LoadImageProgressStatus lip = new LoadImageProgressStatus
                {
                    ThumbnailInCell = this.ThumbnailInCells[imageInCell.Position],
                    BitmapImage = bitmapImage,
                    Position = imageInCell.Position,
                };
                this.UpdateThumbnailsLoadProgress(lip);
            }
        }
        #endregion

        #region Async thumbnail loader using background worker
        // NOTE: STILL TO RESOLVE. If we run this again while it is in the middle of doing something, 
        // the CancellationPending doesn't terminate everything i.e., it will raise an exception:
        // System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute' 
        // The try-catch mitigates this somewhat, but it can still fail on rapid re-running. 
        // Also put a try/catch in all asyn tasks, but this seems like a poor way of doing things
        public void LoadThumbnailsWorker(Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool useTwoPasses, bool useFFMpeg)
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
                try
                {
                    List<ThumbnaillnCell> VideoInCell = new List<ThumbnaillnCell>();
                    if (useTwoPasses)
                    {

                        foreach (ThumbnaillnCell thumbnailInCell in this.ThumbnailInCells)
                        {
                            if (this.BackgroundWorker.CancellationPending == true)
                            {
                                ea.Cancel = true;
                                return;
                            }
                            LoadImageProgressStatus lip = new LoadImageProgressStatus
                            {
                                ThumbnailInCell = this.ThumbnailInCells[thumbnailInCell.Position],
                                // Uncomment this to try the asyn version, but it doesn't seem to add anything and appears slower
                                //BitmapImage = Thumbnailer.GetThumbnailFromFileAsync(imageInCell.Path, 20, 20, useFFMpeg).Result,
                                BitmapImage = (thumbnailInCell.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == true)
                                ? Thumbnailer.GetThumbnailFromFile(thumbnailInCell.Path, 20, 20, useFFMpeg)
                                : null,
                                Position = thumbnailInCell.Position,
                            };
                            this.BackgroundWorker.ReportProgress(0, lip);
                            if (this.BackgroundWorker.CancellationPending == true)
                            {
                                ea.Cancel = true;
                                return;
                            }
                        }
                    }

                    // Add placeholder for each video (as its otherwise slow) on the first part of the first pass
                    foreach (ThumbnaillnCell thumbnailInCell in this.ThumbnailInCells)
                    {
                        // BitmapImage bitmapImage;
                        if (this.BackgroundWorker.CancellationPending == true)
                        {
                            ea.Cancel = true;
                            return;
                        }

                        if (thumbnailInCell.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            // Skip videos
                            continue;
                        }
                        LoadImageProgressStatus lip = new LoadImageProgressStatus
                        {
                            ThumbnailInCell = this.ThumbnailInCells[thumbnailInCell.Position],
                            //BitmapImage = Thumbnailer.GetThumbnailFromFileAsync(imageInCell.Path, desiredWidth, desiredHeight, useFFMpeg).Result,
                            BitmapImage = Thumbnailer.GetThumbnailFromFile(thumbnailInCell.Path, desiredWidth, desiredHeight, useFFMpeg),
                            Position = thumbnailInCell.Position,
                        };
                        this.BackgroundWorker.ReportProgress(0, lip);
                        if (this.BackgroundWorker.CancellationPending == true)
                        {
                            ea.Cancel = true;
                            return;
                        }
                    }

                    // Add a better resultion version of each video, where the resolution matches the grid size
                    //foreach (ThumbnaillnCell imageInCell in this.ImageInCells)
                    //{
                    foreach (ThumbnaillnCell imageInCell in this.ThumbnailInCells)
                    {
                        // BitmapImage bitmapImage;
                        if (this.BackgroundWorker.CancellationPending == true)
                        {
                            ea.Cancel = true;
                            return;
                        }

                        if (imageInCell.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Skip images
                            continue;
                        }
                        LoadImageProgressStatus lip = new LoadImageProgressStatus
                        {
                            ThumbnailInCell = this.ThumbnailInCells[imageInCell.Position],
                            //BitmapImage = Thumbnailer.GetThumbnailFromFileAsync(imageInCell.Path, desiredWidth, desiredHeight, useFFMpeg).Result,
                            BitmapImage = Thumbnailer.GetThumbnailFromFile(imageInCell.Path, desiredWidth, desiredHeight, useFFMpeg),
                            Position = imageInCell.Position,
                        };
                        this.BackgroundWorker.ReportProgress(0, lip);

                        if (this.BackgroundWorker.CancellationPending == true)
                        {
                            ea.Cancel = true;
                            return;
                        }
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.Print("DoWork Aborted");
                }
            };

            this.BackgroundWorker.ProgressChanged += (o, ea) =>
                {
                    // this gets called on the UI thread
                    LoadImageProgressStatus lip = (LoadImageProgressStatus)ea.UserState;
                    this.UpdateThumbnailsLoadProgress(lip);
                };

            this.BackgroundWorker.RunWorkerCompleted += (o, ea) =>
                    {
                        Mouse.OverrideCursor = null;

                        // All images should be loaded by now
                        // BackgroundWorker aborts execution on an exception and transfers it to completion for handling
                        if (ea.Error != null)
                        {
                            throw new FileLoadException("Thumbnail loading failed unexpectedly.  See inner exception for details.", ea.Error);
                        }
                    };
            Mouse.OverrideCursor = Cursors.Wait;
            // Get the thumbnails
            this.BackgroundWorker.RunWorkerAsync();
        }
        #endregion

        #region Public Grid-related Utility methods
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
        // Its in a try catch as it can generate an exception (index out of range) if the async operation is aborted while this is still pending
        // I'd rather try to cancel this cleanly, but need to figure out how.
        private void UpdateThumbnailsLoadProgress(LoadImageProgressStatus lip)
        {
            try
            {
                if (lip.ThumbnailInCell.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    // Its an image
                    if (lip.BitmapImage != null)
                    {
                        this.ThumbnailInCells[lip.ThumbnailInCell.Position].Image.Source = lip.BitmapImage;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(String.Format("No bitmap image available at position {0}", lip.ThumbnailInCell.Position));
                    }
                }
                else if (lip.BitmapImage == null)
                {
                    // Its a video, but as it is null just put up an empty video bitmap
                    this.ThumbnailInCells[lip.ThumbnailInCell.Position].Image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/BlankVideo.jpg"));
                }
                else
                {
                    // Overlay a playbutton atop the video thumbnail image
                    this.ThumbnailInCells[lip.ThumbnailInCell.Position].Image.Source = BitmapDrawing.OverlayCenteredVideoPlayButton(lip.BitmapImage);
                }
            }
            catch
            {
                System.Diagnostics.Debug.Print("UpdateImageLoadProgress Aborted");
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

        #region Class: LoadImageProgressStatus
        // Used by ReportProgress to pass specific values to Progress Changed as a parameter 
        internal class LoadImageProgressStatus
        {
            public ThumbnaillnCell ThumbnailInCell { get; set; }
            public BitmapImage BitmapImage { get; set; }
            public int Position { get; set; }

            public LoadImageProgressStatus()
            {
                this.ThumbnailInCell = null;
                this.BitmapImage = null;
                this.Position = 0;
            }
        }
        #endregion
    }
}
