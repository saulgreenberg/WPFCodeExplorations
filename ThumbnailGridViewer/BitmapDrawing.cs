using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BackgroundThumbnailLoader
{
    public static class BitmapDrawing
    {
        /// <summary>
        /// Given a bitmapSource, add a play button atop of it as a cue that its a video thumbnail
        /// NOTE: its in a try / catch as it can fail due to an exception 
        /// System.InvalidOperationException: 'The calling thread cannot access this object because a different thread owns it.'
        /// that could occur when many frequent relaods are being done. Not sure why...
        /// </summary>
        public static BitmapImage OverlayCenteredVideoPlayButton(BitmapSource bmp)
        {
            try
            {
                float radius = (float)(bmp.Height / 4.0);
                RenderTargetBitmap target = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, bmp.DpiX, bmp.DpiY, PixelFormats.Pbgra32);
                DrawingVisual visual = new DrawingVisual();

                using (DrawingContext r = visual.RenderOpen())
                {
                    // We will draw based on the center of the bitmap
                    Point center = new Point(bmp.Width / 2, bmp.Height / 2);
                    PointCollection trianglePoints = GetTriangleVerticesInscribedInCircle(center, radius);

                    // Construct the triangle
                    StreamGeometry triangle = new StreamGeometry();
                    using (StreamGeometryContext geometryContext = triangle.Open())
                    {
                        geometryContext.BeginFigure(trianglePoints[0], true, true);
                        PointCollection points = new PointCollection
                                             {
                                                trianglePoints[1],
                                                trianglePoints[2]
                                             };
                        geometryContext.PolyLineTo(points, true, true);
                    }

                    // Define the translucent bruches for the triangle an circle
                    SolidColorBrush triangleBrush = new SolidColorBrush(Colors.LightBlue)
                    {
                        Opacity = 0.5
                    };

                    SolidColorBrush circleBrush = new SolidColorBrush(Colors.White)
                    {
                        Opacity = 0.5
                    };

                    // Draw everything
                    r.DrawImage(bmp, new Rect(0, 0, bmp.Width, bmp.Height));
                    r.DrawGeometry(triangleBrush, null, triangle);
                    r.DrawEllipse(circleBrush, null, center, radius + 5, radius + 5);
                }
                target.Render(visual);
                return ConvertBitmapSourceToBitmapImage(target);
            }
            catch
            {
                System.Diagnostics.Debug.Print("Aborted in BitmapDrawing");
                return null;
            }
        }

        // Return  3 points (vertices) that inscribe a triangle into the circle defined by a center point and a radius, 
        private static PointCollection GetTriangleVerticesInscribedInCircle(Point center, float radius)
        {
            PointCollection points = new PointCollection();
            for (int i = 0; i < 3; i++)
            {
                Point v = new Point
                {
                    X = center.X + radius * (float)Math.Cos(i * 2 * Math.PI / 3),
                    Y = center.Y + radius * (float)Math.Sin(i * 2 * Math.PI / 3)
                };
                points.Add(v);
            }
            return points;
        }

        // Lots of conversion going on - if we want this to be efficient, we should see if we can skip some of these.
        // However, it works so we can just leave it in this test program.
        private static BitmapImage ConvertBitmapSourceToBitmapImage(RenderTargetBitmap renderTargetBitmap)
        {
            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
    }
}
