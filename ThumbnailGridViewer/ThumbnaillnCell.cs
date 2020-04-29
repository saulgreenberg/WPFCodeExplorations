using System.Windows.Controls;

namespace BackgroundThumbnailLoader
{

    public class ThumbnaillnCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public Image Image { get; set; }
        public string Path { get; set; }
        public int Position { get; set; }

        public ThumbnaillnCell()
        {
        }
    }
}
