using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BackgroundThumbnailLoader
{
    internal static class BitmapSourceExtensions
    {
        public static WriteableBitmap AsWriteable(this BitmapSource bitmapSource)
        {
            if (bitmapSource is WriteableBitmap)
            {
                return bitmapSource as WriteableBitmap;
            }
            WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapSource);
            writeableBitmap.Freeze();
            return writeableBitmap;
        }

        // Checks whether the image is completely black
        public static unsafe bool IsBlack(this WriteableBitmap image)
        {
            // Check the arguments for null 
            if (image == null)
            {
                System.Diagnostics.Debug.Print("In IsBlack - image is null");
                return false;
            }

            // The RGB offsets from the beginning of the pixel (i.e., 0, 1 or 2)
            GetColorOffsets(image, out int blueOffset, out int greenOffset, out int redOffset);

            // examine only a subset of pixels as otherwise this is an expensive operation
            // check pixels from last to first as most cameras put a non-black status bar or at least non-black text at the bottom of the frame,
            // so reverse order may be a little faster on average in cases of nighttime images with black skies
            // TODO  Calculate pixelStride as a function of image size so future high res images will still be processed quickly.
            byte* currentPixel = (byte*)image.BackBuffer.ToPointer(); // the imageIndex will point to a particular byte in the pixel array
            int pixelStride = 20;
            int totalPixels = image.PixelHeight * image.PixelWidth; // total number of pixels in the image
            for (int pixelIndex = totalPixels - 1; pixelIndex > 0; pixelIndex -= pixelStride)
            {
                // get next pixel of interest
                byte b = *(currentPixel + blueOffset);
                byte g = *(currentPixel + greenOffset);
                byte r = *(currentPixel + redOffset);

                if (r != 0 || b != 0 || g != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static void GetColorOffsets(WriteableBitmap image, out int blueOffset, out int greenOffset, out int redOffset)
        {
            if (image.Format == PixelFormats.Bgr24 ||
                image.Format == PixelFormats.Bgr32 ||
                image.Format == PixelFormats.Bgra32 ||
                image.Format == PixelFormats.Pbgra32)
            {
                blueOffset = 0;
                greenOffset = 1;
                redOffset = 2;
            }
            else if (image.Format == PixelFormats.Rgb24)
            {
                redOffset = 0;
                greenOffset = 1;
                blueOffset = 2;
            }
            else
            {
                throw new NotSupportedException(String.Format("Unhandled image format {0}.", image.Format));
            }
        }
    }

}
