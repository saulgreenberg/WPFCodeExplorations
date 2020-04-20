using System;
using System.Collections.Generic;
using System.IO;

namespace BackgroundImageLoader
{
    // Get all jpg file paths of images found in a fixed location 
    // Assumes that the app runs in Debug or Release, and that the image is in ..\..\..\Images\img.jpg
    public class JPGImagePaths
    {
        public List<string> Paths {get; set;}

        private readonly string imageFolderPath;
        public JPGImagePaths ()
        {
            this.imageFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            this.imageFolderPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(this.imageFolderPath, @"..\..\..\Images"));
            if (Directory.Exists(this.imageFolderPath) == false)
            {
                System.Windows.MessageBox.Show("Couldn't find the folder of images: " + this.imageFolderPath);
            }
        }

        public List<string> Get()
        {
            return GetImagePaths(this.imageFolderPath);
        }

        public List<string> FindPaths(int maxPaths)
        {
            this.Paths = GetImagePaths(this.imageFolderPath, maxPaths);
            return this.Paths;
        }

        #region Private methods
        // Get all images (well, unless there happens to be more than 100000 images!)
        private static List<string> GetImagePaths(string imageFolderpath)
        {
            return GetImagePaths(imageFolderpath, 100000);
        }

        // Retrieve a list of jpg file paths, up to max paths, found in a path to an image folder
        private static List<string> GetImagePaths(string imageFolderpath, int maxPaths)
        {
            int i = 0;
            List<string> fullPaths = new List<string>(Directory.GetFiles(imageFolderpath, "*.jpg"));
            List<string> imagePaths = new List<string>();
            foreach (string path in fullPaths)
            {
                if (i >= maxPaths)
                {
                    break;
                }
                imagePaths.Add(path);
                i++;
            }
            return imagePaths;
        }
        #endregion
    }
}
