using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BackgroundThumbnailLoader
{
    // A convenience class.
    // Get all jpg and video file paths of images found in two fixed locations 
    // They are returned is a single alphabetically sorted list of paths
    // The fixed locations assumes that the app runs in Debug or Release, and that the 
    // - image (.jpg) files are in ..\..\..\Images\ 
    // - video (.mp4, .avi, .asf) files are in ..\..\..\Videos\
    public static class ImageAndVideoFiles
    {

        // Retrieve a list of jpg and mp4 file paths, up to max paths, found in a path to a folder
        public static List<string> GetPaths(int maxPaths)
        {
            string imageFolderPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Images"));
            string videoFolderPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Videos"));
            IEnumerable<string> fullPaths = new List<string>();

            // Get the images
            if (Directory.Exists(imageFolderPath))
            {
                fullPaths = Directory.EnumerateFiles(imageFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s =>
                s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
            }

            // Get the videos
            if (Directory.Exists(videoFolderPath))
            {
                fullPaths = fullPaths.Union(Directory.EnumerateFiles(videoFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s =>
                    s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".asf", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)));
                fullPaths = fullPaths.OrderBy(s => Path.GetFileName(s), StringComparer.CurrentCultureIgnoreCase);
                fullPaths = fullPaths.Take(Math.Min(fullPaths.Count(), maxPaths)); 
            }
            return fullPaths.ToList<string>();
        }
    }
}
