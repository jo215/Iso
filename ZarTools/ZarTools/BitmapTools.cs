﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Drawing.Imaging;

namespace ZarTools
{
    /// <summary>
    /// Methods for converting Bitmaps to other formats.
    /// </summary>
    public static class BitmapTools
    {

        /// <summary>
        /// Converts a Bitmap to an ImageSource.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static ImageSource ToImageSource(Bitmap image)
        {
            IntPtr hbitmap = image.GetHbitmap();
            ImageSource bitmapSource = null;
            try
            {
                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                   hbitmap, IntPtr.Zero, Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Clean up the bitmap data
                DeleteObject(hbitmap);
            }
            return bitmapSource;
        }

        /// <summary>
        /// Converts a Bitmap to a BitmapImage.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static BitmapImage ToWpfBitmap(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();

                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                stream.Close();
                stream.Dispose();
                return result;
            }
        }


        //  Free resources
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Converts a Bitmap to a Texture2D.
        /// </summary>
        /// <param name="GraphicsDevice"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Texture2D ToTexture2D(GraphicsDevice GraphicsDevice, Bitmap image)
        {
            // Buffer size is size of color array multiplied by 4 because   
            // each pixel has four color bytes  
            int bufferSize = image.Height * image.Width * 4;

            // Create new memory stream and save image to stream so   
            // we don't have to save and read file  
            MemoryStream memoryStream = new MemoryStream(bufferSize);
            image.Save(memoryStream, ImageFormat.Png);

            // Creates a texture from IO.Stream - our memory stream  
            Texture2D texture = Texture2D.FromStream(GraphicsDevice, memoryStream);
            memoryStream.Close();
            return texture;
        }

        /// <summary>
        /// Seems to be the best method to convert a Bitmap to a BitmapSource;
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapSource ToBitmapSource2(Bitmap bitmap)
        {
            MemoryStream bitmapStream = new MemoryStream();
            bitmap.Save(bitmapStream, ImageFormat.Bmp);

            return BitmapFrame.Create(bitmapStream);
        }

        /// <summary>
        /// Converts a Bitmap to a BitmapSource.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static BitmapSource ToBitmapSource(Bitmap image)
        {
            var rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            try
            {
                var bitmap_data = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);

                try
                {
                    BitmapPalette palette = null;

                    if (image.Palette.Entries.Length > 0)
                    {
                        var palette_colors = image.Palette.Entries.Select(entry => System.Windows.Media.Color.FromArgb(entry.A, entry.R, entry.G, entry.B)).ToList();
                        palette = new BitmapPalette(palette_colors);
                    }

                    return BitmapSource.Create(
                        image.Width,
                        image.Height,
                        image.HorizontalResolution,
                        image.VerticalResolution,
                        ConvertPixelFormat(image.PixelFormat),
                        palette,
                        bitmap_data.Scan0,
                        bitmap_data.Stride * image.Height,
                        bitmap_data.Stride
                    );
                }
                finally
                {
                    image.UnlockBits(bitmap_data);
                }
            }
            catch (Exception e) { return null; }
        }

        private static System.Windows.Media.PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat sourceFormat)
        {
            switch (sourceFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return PixelFormats.Bgr24;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return PixelFormats.Bgra32;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return PixelFormats.Bgr32;
            }

            return new System.Windows.Media.PixelFormat();
        }
    }
}
