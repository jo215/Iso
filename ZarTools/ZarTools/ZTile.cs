using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using System.Drawing;
using System.Windows.Media;

namespace ZarTools
{
    /// <summary>
    /// Represents a Tile.
    /// </summary>
    public class ZTile
    {
	    //	Tile properties
        public string RelativePath { get; private set; }
        public string Name { get; private set; }
        public string Tileset { get; set; }
        public int[] BoundingBox { get; private set; }				//	Bounding box - height, width, depth in some order / | \
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Width2 { get; private set; }
        public int Height2 { get; private set; }
        public TileType TileType { get; private set; }
        public TileMaterial Material { get; private set; }
        public List<TileFlag> Flags { get; private set; }

        private readonly List<Texture2D> _textures;
        private readonly List<ImageSource> _images;
        public ImageSource Image
        {
            get
            {
                return GetImage();
            }
        }
        public List<Bitmap> Bitmaps { get; set; }

        private int _version;			                    //	Generally 9

	    //	Temporary Zar info
	    private readonly List<Zar> _zars;
	    //	Where the actual image info starts for when we are ready to grab Bitmaps
        private int _zarStartPos;
        private int _subType;
        private System.Drawing.Color[] _palette;
        private int _defaultPaletteIndex;
	    //	Variables for extracting/drawing
        private readonly byte[] _buffer;
	    private int _zPos, _drawPos, _bufferPos;
        private Bitmap _bm;

	    /**
	     * Constructor.
	     */
	    public ZTile(String filename) {
		    _zars = new List<Zar>();
		    _textures = new List<Texture2D>();
		    Flags = new List<TileFlag>();
            Bitmaps = new List<Bitmap>();
            _images = new List<ImageSource>();
		    //	Find the file - create an input stream, cache the bytes
            try
            {
                var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                var br = new BinaryReader(fs);
                var fi = new FileInfo(filename);
                var fileSize = fi.Length;
                _buffer = br.ReadBytes((int)fileSize);
                Name = fi.Name.ToLower().Substring(0, fi.Name.Length - 4);
                _bufferPos = 0;
                RelativePath = fi.FullName.ToLower();
                if (RelativePath.IndexOf("tiles", StringComparison.Ordinal) >= 0)
                    RelativePath = RelativePath.Substring(RelativePath.IndexOf("tiles", StringComparison.Ordinal));
                ReadTileHeader();
                while (_bufferPos < _buffer.Length && (_buffer.Length - _bufferPos > 1200))
                {	//	This is a guess
                    ReadZarHeader();
                    ReadPalette();
                    ReadRleBlocks();
                    _zarStartPos = _bufferPos;
                }
                MakeBitmaps();
                fs.Close();
                fs.Dispose();
                br.Close();
                br.Dispose();

                _palette = null;
                _buffer = null;
                _zars = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
	    }

        /// <summary>
        /// To string method - returns the name of this tile.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Draws the Tile as a Texture2D.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public void DrawTexture(SpriteBatch spriteBatch, Vector2 position, Microsoft.Xna.Framework.Color color)
        {
            //  If we only have the Bitmaps then create the Textures
            if (_textures.Count == 0)
                foreach (var t in Bitmaps)
                {
                    _textures.Add(BitmapTools.ToTexture2D(spriteBatch.GraphicsDevice, t));

                    //Bitmaps[i] = null;
                }
            //Bitmaps = null;
            spriteBatch.Draw(_textures[0], position, color);
        }

        /// <summary>
        /// Returns the tile as an ImageSource for integration with WPF
        /// </summary>
        /// <returns></returns>
        private ImageSource GetImage()
        {
            //  Create the images if necessary
            if (_images.Count == 0)
            {
                foreach (var t in Bitmaps)
                {
                    _images.Add(BitmapTools.ToImageSource(t));
                    //Bitmaps[i] = null;
                }
                //Bitmaps = null;
            }
            if (_images.Count == 0)
                Console.WriteLine(Name + " has 0 images.");
            return _images[0];
        }

	    /**
	     * Converts each of this tiles zar's to a Bitmap.
	     */
	    private void MakeBitmaps()
	    {
	        foreach (var t in _zars)
	        {
	            _bm = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
	            var currentZar = t;
	            //	Sequentially decode the RLE blocks
	            _zPos = 0;
	            _drawPos = 0;
	            var rleBlocks = currentZar.RleBlocks;
	            //	Draw
	            while ( _zPos < rleBlocks.Count) {
	                //	2-bit Command / 6-bit blockLength
                    var command = rleBlocks[_zPos] & 3;
                    var blockLength = rleBlocks[_zPos] >> 2;
	                _zPos ++;
	                //	Carry out RLE command
                    for (var i = 0; i < blockLength; i++)
                    {
	                    switch (command) {
	                        case 0 :	//	Skip the next blockLength pixels
	                            _drawPos ++;
	                            break;
	                        case 1 :	//	Pixel RGB from palette, alpha = 255
	                            FastSetPixel(_palette[rleBlocks[_zPos]].R, _palette[rleBlocks[_zPos]].G, _palette[rleBlocks[_zPos]].B, 255);
	                            break;
	                        case 2 :	//	Pixel RGB from palette, alpha value (pair - so extra zPos increment)
	                            FastSetPixel(_palette[rleBlocks[_zPos]].R, _palette[rleBlocks[_zPos]].G, _palette[rleBlocks[_zPos]].B, rleBlocks[_zPos + 1] );
	                            _zPos ++;
	                            break;
	                        case 3 :	//	Pixel RGB from palette's default color, alpha values
	                            FastSetPixel(_palette[_defaultPaletteIndex].R, _palette[_defaultPaletteIndex].G, _palette[_defaultPaletteIndex].B, rleBlocks[_zPos]);
	                            break;
	                    }
	                }
	            }
	            //	Tile fully drawn - add it to list
	            Bitmaps.Add(_bm);
	            //bm.Save(new FileStream("c:\\geezer.png", FileMode.CreateNew, FileAccess.Write), ImageFormat.Png);
	        }
	    }

        /// <summary>
        /// Plots a single pixel.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private void FastSetPixel(int r, int g, int b, int a)
        {
            _bm.SetPixel(_drawPos % Width, _drawPos / Width, System.Drawing.Color.FromArgb(a, r, g, b));
            _drawPos++;
            _zPos++;
        }

	    /**
	     * Reads the compressed image data.
	     */
	    private void ReadRleBlocks() {
		    var rleSize = GetInt();
		    //	Read the RLE blocks
		    _zars.Add(new Zar(new List<int>(), Width, Height, _subType, _defaultPaletteIndex, 0, 0, null));
		    for (var j = 0; j < rleSize; j ++)
			    _zars[_zars.Count - 1].RleBlocks.Add(GetByte());
	    }

	    /**
	     * Reads the palette information.
	     */
	    private void ReadPalette() {
		    //	palette present flag
		    int palPresent = GetByte();
		    if (palPresent == 0)
			    Console.WriteLine("Zar must include palette; exiting.");
		    //	read 1 palette
		    long paletteSize = GetInt();
		    if (paletteSize > 256)
			    Console.WriteLine("Wrong number of colors in palette.");
		    _palette = new System.Drawing.Color[(int)paletteSize];
		    for (var j = 0; j < paletteSize ; j ++ ) {
			    //	Palette stored as 4 byte BGRA
			    int b = GetByte();
                int g = GetByte();
                int r = GetByte();
                int a = GetByte();
			    _palette[j] = System.Drawing.Color.FromArgb(a, r, g, b);
		    }
		    //	Read default color
		    _defaultPaletteIndex = 0;
		    if (_subType == 4)
                _defaultPaletteIndex = GetByte();
	    }

	    /**
	     * Reads zar-specific information.
	     */
	    private void ReadZarHeader() {
		    _bufferPos = _zarStartPos;
		    //	zar subtype 3 or 4 - if 4 then default color included
		    _subType = GetByte();
		    while( _subType < 51 || _subType > 52)
                _subType = GetByte();
			_subType = _subType - 48;	//	3 or 4
		    //	miss +7	dummy variable
            GetByte();
		    //	width + height
		    long zWidth = GetInt();
            long zHeight = GetInt();
		    Width2 = (int) zWidth;
            Height2 = (int) zHeight;
	    }

	    /**
	     * Reads tile property information.
	     */
	    private void ReadTileHeader() {
		    _bufferPos = 7;		//	<tile>0
		    //	version
		    _version = GetByte() - 48;
		    //	skip unknown byte probably 0
		    _bufferPos++;
		    if (_version == 1) _bufferPos++;
		    BoundingBox = new int[3];
		    BoundingBox[0] = GetByte();
		    BoundingBox[1] = GetByte();
		    BoundingBox[2] = GetByte();
		    //	tile specific info
		    GetInt(); //    unknown 1
		    GetInt(); //    unknown 2
		    Width =  GetInt();
		    Height = GetInt();
		    int type = GetByte();
		    if (type <= 6)
			    TileType = (TileType) type;
		    else
			    Console.WriteLine(Name + " --> unknown TileType: " + type);
		    int mat = GetByte();
		    if (mat <= 7)
			    Material = (TileMaterial) mat;
		    else
			    Console.WriteLine(Name + " --> unknown TileMaterial: " + mat);
		    //	tile flags
            if (_version == 7 || _version == 8)
                GetByte();
		    int flag = GetByte();
            //Console.WriteLine("FLAG: " + Name + flag);
		    if ((flag & 1) == 1) Flags.Add(TileFlag.Ethereal);
		    if ((flag & 2) == 2) Flags.Add(TileFlag.HurtsNPC);
		    if ((flag & 4) == 4) Flags.Add(TileFlag.Window);
		    if ((flag & 8) == 8) Flags.Add(TileFlag.NoShadow);
		    if ((flag & 16) == 16) Flags.Add(TileFlag.Climbable);
		    if ((flag & 32) == 32) Flags.Add(TileFlag.NoPop);
		    if ((flag & 64) == 64) Flags.Add(TileFlag.Exit);
		    if ((flag & 128) == 128) Flags.Add(TileFlag.Invisible);
		    //	unknown tile data
		    _zarStartPos = _bufferPos + 17;
	    }
	
        //  Helper methods.
        private byte GetByte()
        {
            var result = _buffer[_bufferPos];
            _bufferPos++;
            return result;
        }

        private int GetInt()
        {
            var result = BitConverter.ToInt32(_buffer, _bufferPos);
            _bufferPos += 4;
            return result;
        }
    }
}
