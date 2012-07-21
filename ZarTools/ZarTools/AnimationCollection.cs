using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZarTools
{
    /// <summary>
    /// Animation collections.
    /// </summary>
    internal class AnimationCollection
    {
        internal BaseZarSprite Sprite;
        internal int FileOffset;
        internal String Name;
        internal int FrameCount, DirCount;
        internal Rectangle[] FrameRect;
        internal List<Zar> Zars;
        internal Texture2D[] Textures;
        internal Color[,] Palette;
        internal Rectangle CollectionOffset;

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
            FileOffset = fileOffset;
            CollectionOffset = collectionRect;
            Name = colName;
            FrameCount = frameCount;
            DirCount = dirCount;
            FrameRect = frameRect;
            Palette = palette;
            Sprite = sprite;
            Zars = new List<Zar>();
            Textures = new Texture2D[frameCount * dirCount];
            for (var i = 0; i < frameRect.Length; i++)
            {
                if (frameRect[i].Width <= 0)
                    frameRect[i].Width = CollectionOffset.Width ;
                if (frameRect[i].Height <= 0)
                    frameRect[i].Height = CollectionOffset.Height;
            }
        }
    }
}
