using NReco.VideoConverter;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BackgroundThumbnailLoader
{
    public class Thumbnailer
    {
        #region GetThumbnailFromFile: Sync and Async versions
        // Given a filepath to either a jpg or video file, return a BitmapImage of the desired width / height if it exists
        // Async and sync versions below
        public static Task<BitmapImage> GetThumbnailFromFileAsync(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool useFFMpeg)
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
                return Thumbnailer.GetThumbnailFromFile(path, desiredWidth, desiredHeight, useFFMpeg);
            });
        }

        public static BitmapImage GetThumbnailFromFile(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight, bool useFFMpeg)
        {

            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                // get a bitmap from a jpg file
                return Thumbnailer.GetThumbnailFromJPGFile(path, desiredWidth, desiredHeight);
            }
            if (useFFMpeg)
            {
                // get a bitmap from a video file using ffmpeg
                return GetThumbnailFromVideoFileViaFFMPEG(path, desiredWidth, desiredHeight);
            }
            else
            {
                // get a bitmap from a video file using MediaPlayer
                return Thumbnailer.GetThumbnailFromVideoFileViaMediaPlayer(path, desiredWidth, desiredHeight);
            }
        }
        #endregion

        #region Private: Return a thumbnail as a bitmapImage from a jpg file
        // Return a bitmapImage from a jpg file
        private static BitmapImage GetThumbnailFromJPGFile(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight)
        {
            try
            {
                BitmapCacheOption bitmapCacheOption = BitmapCacheOption.None;
                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                //bitmap.DecodePixelWidth = desiredWidth.Value;
                bitmap.DecodePixelHeight = desiredHeight.Value;
                bitmap.CacheOption = bitmapCacheOption;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                Debug.Print("Could not load JPG: " + path);
                return null;
            }
        }
        #endregion

        #region Private: return a thumbnail as a bitmapImage from a video file 
        // return a thumbnail as a bitmapImage from a video file using  FFMPEG
        // Note that is faster than the MediaPlayer version, but it does rely on NRECO Nuget package which also uses FFMPEG
        public static BitmapImage GetThumbnailFromVideoFileViaFFMPEG(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight)
        {
            try
            {
                // Note: not sure of the cost of creating a new converter every time. May be better to reuse it.
                Stream outputBitmapAsStream = new MemoryStream();
                FFMpegConverter ffMpeg = new NReco.VideoConverter.FFMpegConverter();
                // We can experiment with getting smaller thumbnails, but it doesn't seem to make a huge difference.
                // Note that you can use NRECO probe to get the aspect ratio and thus set a reasonable frame size
                //ConvertSettings convertSettings = new ConvertSettings();
                //convertSettings.SetVideoFrameSize(desiredWidth.Value, desiredHeight.Value);
                //ffMpeg.GetVideoThumbnail(path, outputBitmapAsStream, 1, convertSettings);
                ffMpeg.GetVideoThumbnail(path, outputBitmapAsStream);
                outputBitmapAsStream.Position = 0;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                // bitmap.DecodePixelWidth = desiredWidth.Value;
                bitmap.DecodePixelHeight = desiredHeight.Value;
                bitmap.CacheOption = BitmapCacheOption.None;
                bitmap.StreamSource = outputBitmapAsStream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                Debug.Print("Could not load Video: " + path);
                return null;
            }
        }

        // return a thumbnail as a bitmapImage from a video file using the media player
        // Note that its much slower than ffmpeg, but doesn't rely on any outside processes or code
        public static BitmapImage GetThumbnailFromVideoFileViaMediaPlayer(string path, Nullable<int> desiredWidth, Nullable<int> desiredHeight)
        {
            MediaPlayer mediaPlayer = new MediaPlayer
            {
                Volume = 0.0
            };
            try
            {
                // Open the mediaplayer and play it until we actually get a video frame.
                // Unfortunately, its very time inefficient...
                mediaPlayer.Open(new Uri(path));
                mediaPlayer.Play();

                // MediaPlayer is not actually synchronous despite exposing synchronous APIs, so wait for it get the video loaded.  Otherwise
                // the width and height properties are zero and only black pixels are drawn.  The properties will populate with just a call to
                // Open() but without also Play() only black is rendered
                int timesTried = 1000;
                while ((mediaPlayer.NaturalVideoWidth < 1) || (mediaPlayer.NaturalVideoHeight < 1))
                {
                    // back off briefly to let MediaPlayer do its loading, which typically takes perhaps 75ms
                    // a brief Sleep() is used rather than Yield() to reduce overhead as 500k to 1M+ yields typically occur
                    Thread.Sleep(TimeSpan.FromMilliseconds(1.0));
                    if (timesTried-- <= 0)
                    {
                        return BitmapDrawing.OverlayCenteredVideoPlayButton(new BitmapImage(new Uri("pack://application:,,,/Resources/BlankVideo.jpg")));
                    }
                }

                // sleep one more time as MediaPlayer has a tendency to still return black frames for a moment after the width and height have populated
                Thread.Sleep(TimeSpan.FromMilliseconds(1.0));

                int pixelWidth = mediaPlayer.NaturalVideoWidth;
                int pixelHeight = mediaPlayer.NaturalVideoHeight;
                if (desiredWidth.HasValue)
                {
                    double scaling = desiredWidth.Value / (double)pixelWidth;
                    pixelWidth = (int)(scaling * pixelWidth);
                    pixelHeight = (int)(scaling * pixelHeight);
                }

                // set up to render frame from the video
                mediaPlayer.Pause();
                mediaPlayer.Position = TimeSpan.FromMilliseconds(1.0);

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawVideo(mediaPlayer, new Rect(0, 0, pixelWidth, pixelHeight));
                }

                // render and check for black frame
                // it's assumed the camera doesn't yield all black frames
                int maximumRenderAttempts = 10;
                for (int renderAttempt = 1; renderAttempt <= maximumRenderAttempts; ++renderAttempt)
                {
                    // try render
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Default);
                    renderBitmap.Render(drawingVisual);
                    renderBitmap.Freeze();

                    // check if render succeeded
                    // hopefully it did and most of the overhead here is WriteableBitmap conversion though, at 2-3ms for a 1280x720 frame, this 
                    // is not an especially expensive operation relative to the  O(175ms) cost of this function
                    WriteableBitmap writeableBitmap = renderBitmap.AsWriteable();
                    if (writeableBitmap.IsBlack() == false)
                    {
                        // if the media player is closed before Render() only black is rendered
                        // TraceDebug.PrintMessage(String.Format("Video render returned a non-black frame after {0} times.", renderAttempt - 1));
                        mediaPlayer.Close();
                        return ConvertWriteableBitmapToBitmapImage(writeableBitmap);
                    }
                    // black frame was rendered; backoff slightly to try again
                    Thread.Sleep(TimeSpan.FromMilliseconds(10.0));
                }
                throw new ApplicationException(String.Format("Limit of {0} render attempts was reached.", maximumRenderAttempts));
            }
            catch
            {
                // We don't print the exception // (Exception exception)
                // TraceDebug.PrintMessage(String.Format("VideoRow/LoadBitmap: Loading of {0} failed in Video - LoadBitmap. {0}", imageFolderPath));
                return BitmapDrawing.OverlayCenteredVideoPlayButton(new BitmapImage(new Uri("pack://application:,,,/Resources/BlankVideo.jpg")));
            }
        }

        private static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }
        #endregion
    }
}
