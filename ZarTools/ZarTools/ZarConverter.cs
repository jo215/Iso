using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZarTools
{
    /// <summary>
    /// Converts an AnimationCollection's .zar files to Texture2Ds.
    /// </summary>
    internal class ZarConverter
    {
        private static Color[,] palette = null;
        private static Zar currentZar;
        private static int drawPos;
        private static int zPos;
        private static int totalFrames;
        private static int layerWidth, xOff, yOff;
        private static SpriteBatch sb;
        private static Texture2D pixel;

        /// <summary>
        /// Decodes a .zar file into a useable Texture2D
        /// </summary>
        /// <param name="device"></param>
        /// <param name="col"></param>
        public static void makeBims(GraphicsDevice device, AnimationCollection col)
        {
            if (sb == null)
                sb = new SpriteBatch(device);
            if (pixel == null)
            {
                pixel = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
                pixel.SetData(new[] { Color.White });
            }
		    //	Empty any existing BufferedImage data
		    //col.getBims().clear();
		    //	Number of images to convert/create
		    totalFrames = col.dirCount * col.frameCount;
		    //	Temporary variables
		    int defaultColor;
            int command, blockLength;
		    List<int> RLEblocks;
		    RenderTarget2D bigBim;
		    //	Loop through each zar
		    for ( int zar = 0 ; zar < totalFrames ; zar ++ ) {
			    // Check zar not empty
			    if ( zar >= col.zars.Count) break;
			    //	Final image composite of all 4 layers
			    bigBim = new RenderTarget2D(device, col.frameRect[zar].Width , col.frameRect[zar].Height );
                device.SetRenderTarget(bigBim);
                device.Clear(Color.Transparent);
                
                //	Draw each layer 
                sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
			    for ( int layer = 0; layer < col.sprite.layers; layer ++ ) {

				    // Check zar not empty
				    if (col.zars[zar + (layer * totalFrames)] == null) continue;

				    //	Get the correct zar
				    currentZar = col.zars[zar + (layer * totalFrames)];
				    if (currentZar.Width == 0) {
					    layerWidth = 1;
				    } else {
					    //	Get this layer's width and offsets
					    layerWidth = currentZar.Width;
				    }
				    xOff = currentZar.XOffset;
				    yOff = currentZar.YOffset;
				    //	Get correct palette - should normally be collection's default palette
				    palette = col.palette;
				    defaultColor = currentZar.DefaultColor;
				    //	Sequentially decode the RLE blocks
				    zPos = 0;
				    drawPos = 0;
				    RLEblocks = currentZar.RLEblocks;
                    
				    while ( zPos < RLEblocks.Count ) {
					    //	2-bit Command / 6-bit blockLength
					    command = RLEblocks[zPos] & 3 ;
					    blockLength = RLEblocks[zPos] >> 2;
					    zPos ++;
					    //	Carry out RLE command
					    for ( int i = 0; i < blockLength; i ++ ) {
						    switch (command) {
							    case 0 :	//	Skip the next blockLength pixels
								    drawPos ++;
							    break;
							    case 1 :	//	Pixel RGB from palette, alpha = 255
								    fastSetPixel(palette[RLEblocks[zPos],layer].R, palette[RLEblocks[zPos],layer].G, palette[RLEblocks[zPos],layer].B, 255);
							    break;
							    case 2 :	//	Pixel RGB from palette, alpha value (pair - so extra zPos increment)
								    fastSetPixel(palette[RLEblocks[zPos],layer].R, palette[RLEblocks[zPos],layer].G, palette[RLEblocks[zPos],layer].B, (int)RLEblocks[zPos + 1]);
								    zPos ++;
							    break;
							    case 3 :	//	Pixel RGB from palette's default color, alpha values
								    fastSetPixel(palette[defaultColor,layer].R, palette[defaultColor,layer].G, palette[defaultColor,layer].B, (int)RLEblocks[zPos]);
							    break;
						    }
					    }
				    }
			    }
                sb.End();
			    //	All 4 layers are drawn - add to collection
			    col.textures[zar] = (Texture2D)bigBim;
		    }
            device.SetRenderTarget(null);
	    }

        /// <summary>
        /// Plots a single pixel.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void fastSetPixel(int r, int g, int b, int a)
        {
            sb.Draw(pixel, new Vector2(drawPos % layerWidth + xOff, drawPos / layerWidth + yOff), new Color(r, g, b, a));
            drawPos++;
            zPos++;
        }
    }
}
