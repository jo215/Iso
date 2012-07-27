using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZarTools
{
    /// <summary>
    /// Animation collections.
    /// </summary>
    public class AnimationCollection
    {
        public BaseZarSprite Sprite;
        public int FileOffset;
        public String Name;
        public int FrameCount, DirCount;
        public Rectangle[] FrameRect;
        public List<Zar> Zars;
        public Texture2D[] Textures;
        public Color[,] Palette;
        public Rectangle CollectionOffset;

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
