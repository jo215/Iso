using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZarTools
{
    /// <summary>
    /// Converts an AnimationCollection's .zar files to Texture2Ds.
    /// </summary>
    internal class ZarConverter
    {
        private static Color[,] _palette;
        private static Zar _currentZar;
        private static int _drawPos;
        private static int _zPos;
        private static int _totalFrames;
        private static int _layerWidth, _xOff, _yOff;
        private static SpriteBatch _sb;
        private static Texture2D _pixel;

        /// <summary>
        /// Decodes a .zar file into a useable Texture2D
        /// </summary>
        /// <param name="device"></param>
        /// <param name="col"></param>
        public static void MakeBims(GraphicsDevice device, AnimationCollection col)
        {
            if (_sb == null)
                _sb = new SpriteBatch(device);

            if (_pixel == null)
            {
                _pixel = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
                _pixel.SetData(new[] { Color.White });
            }

		    //	Number of images to convert/create
		    _totalFrames = col.DirCount * col.FrameCount;

            //	Loop through each zar
		    for (var zar = 0 ; zar < _totalFrames ; zar ++ ) {
			    // Check zar not empty
			    if (zar >= col.Zars.Count)
                    break;
			    //	Final image composite of all 4 layers - work out how big a rectangle we need 
		        var bitmapSize = new Rectangle();
                for (var layer = 0; layer < col.Sprite.Layers; layer++)
                {
                    bitmapSize = Rectangle.Union(bitmapSize,
                                    new Rectangle(0, 0, col.Zars[zar + (layer*_totalFrames)].Width,
                                                  col.Zars[zar + (layer*_totalFrames)].Height));
                }
		        var bigBim = new RenderTarget2D(device, bitmapSize.Width + 5 , bitmapSize.Height + 5 );
                device.SetRenderTarget(bigBim);
                device.Clear(Color.Transparent);
                
                //	Draw each layer 
                _sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
			    for (var layer = 0; layer < col.Sprite.Layers; layer ++ ) {
				    // Check zar not empty
				    if (col.Zars[zar + (layer * _totalFrames)] == null) continue;
				    //	Get the correct zar
				    _currentZar = col.Zars[zar + (layer * _totalFrames)];
				    _layerWidth = _currentZar.Width == 0 ? 1 : _currentZar.Width;
				    _xOff = _currentZar.XOffset;
				    _yOff = _currentZar.YOffset;
				    //	Get correct palette - should normally be collection's default palette
				    _palette = col.Palette;
				    var defaultColor = _currentZar.DefaultColor;
				    //	Sequentially decode the RLE blocks
				    _zPos = 0;
				    _drawPos = 0;
				    var rleBlocks = _currentZar.RleBlocks;
				    while ( _zPos < rleBlocks.Count ) {
					    //	2-bit Command / 6-bit blockLength
					    var command = rleBlocks[_zPos] & 3;
					    var blockLength = rleBlocks[_zPos] >> 2;
					    _zPos ++;
					    //	Carry out RLE command
					    for (var i = 0; i < blockLength; i ++ ) {
						    switch (command) {
							    case 0 :	//	Skip the next blockLength pixels
								    _drawPos ++;
							    break;
							    case 1 :	//	Pixel RGB from palette, alpha = 255
								    FastSetPixel(_palette[rleBlocks[_zPos],layer].R, _palette[rleBlocks[_zPos],layer].G, _palette[rleBlocks[_zPos],layer].B, 255);
							    break;
							    case 2 :	//	Pixel RGB from palette, alpha value (pair - so extra zPos increment)
								    FastSetPixel(_palette[rleBlocks[_zPos],layer].R, _palette[rleBlocks[_zPos],layer].G, _palette[rleBlocks[_zPos],layer].B, rleBlocks[_zPos + 1]);
								    _zPos ++;
							    break;
							    case 3 :	//	Pixel RGB from palette's default color, alpha values
								    FastSetPixel(_palette[defaultColor,layer].R, _palette[defaultColor,layer].G, _palette[defaultColor,layer].B, rleBlocks[_zPos]);
							    break;
						    }
					    }
				    }
			    }
                _sb.End();
			    //	All 4 layers are drawn - add to collection
			    col.Textures[zar] = bigBim;
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
        private static void FastSetPixel(int r, int g, int b, int a)
        {
            _sb.Draw(_pixel, new Vector2(_drawPos % _layerWidth + _xOff, _drawPos / _layerWidth + _yOff), new Color(r, g, b, a));
            _drawPos++;
            _zPos++;
        }
    }
}
