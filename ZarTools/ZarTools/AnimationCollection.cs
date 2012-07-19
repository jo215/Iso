using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZarTools
{
    /// <summary>
    /// Animation collections.
    /// </summary>
    internal class AnimationCollection
    {
        internal BaseZarSprite sprite;
        internal int fileOffset;
        internal String name;
        internal int frameCount, dirCount;
        internal Rectangle[] frameRect;
        internal List<Zar> zars;
        internal Texture2D[] textures;
        internal Color[,] palette;
        internal Rectangle collectionOffset;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileOffset"></param>
        /// <param name="colName"></param>
        /// <param name="frameCount"></param>
        /// <param name="dirCount"></param>
        /// <param name="frameRect"></param>
        /// <param name="palette"></param>
        /// <param name="collectionRect"></param>
        /// <param name="sprite"></param>
        public AnimationCollection(int fileOffset, string colName, int frameCount, int dirCount, Rectangle[] frameRect, Color[,] palette, Rectangle collectionRect, BaseZarSprite sprite)
        {
            this.fileOffset = fileOffset;
            this.collectionOffset = collectionRect;
            this.name = colName;
            this.frameCount = frameCount;
            this.dirCount = dirCount;
            this.frameRect = frameRect;
            this.palette = palette;
            this.sprite = sprite;
            this.zars = new List<Zar>();
            this.textures = new Texture2D[frameCount * dirCount];
            for (int i = 0; i < frameRect.Length; i++)
            {
                if (frameRect[i].Width <= 0)
                    frameRect[i].Width = collectionOffset.Width ;
                if (frameRect[i].Height <= 0)
                    frameRect[i].Height = collectionOffset.Height;
            }
        }
    }
}
