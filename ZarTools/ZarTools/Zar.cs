using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ZarTools
{
    /// <summary>
    /// Represents an individual .zar file.
    /// </summary>
    public class Zar
    {
        //  The run-length encoded blocks which define the image pixels.
        public List<int> RleBlocks { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int XOffset { get; private set; }
        public int YOffset { get; private set; }

        public int ZarType { get; private set; }
        public int DefaultColor { get; private set; }
        public Color[,] Palette { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rlEblocks"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="zarType"></param>
        /// <param name="defaultColor"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="palette"></param>
        public Zar(List<int> rlEblocks, int width, int height, int zarType, int defaultColor, int xOffset, int yOffset, Color[,] palette)
        {
            RleBlocks = rlEblocks;
            Width = width;
            Height = height;
            ZarType = zarType;
            DefaultColor = defaultColor;
            XOffset = xOffset;
            YOffset = yOffset;
            Palette = palette;
        }
    }
}
