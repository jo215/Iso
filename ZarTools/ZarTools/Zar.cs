using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ZarTools
{
    /// <summary>
    /// Represents an individual .zar file.
    /// </summary>
    internal class Zar
    {
        //  The run-length encoded blocks which define the image pixels.
        public List<int> RLEblocks { get; private set; }

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
        /// <param name="RLEblocks"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="ZarType"></param>
        /// <param name="DefaultColor"></param>
        /// <param name="XOffset"></param>
        /// <param name="YOffset"></param>
        /// <param name="Palette"></param>
        public Zar(List<int> RLEblocks, int width, int height, int ZarType, int DefaultColor, int XOffset, int YOffset, Color[,] Palette)
        {
            this.RLEblocks = RLEblocks;
            this.Width = width;
            this.Height = height;
            this.ZarType = ZarType;
            this.DefaultColor = DefaultColor;
            this.XOffset = XOffset;
            this.YOffset = YOffset;
            this.Palette = Palette;
        }
    }
}
