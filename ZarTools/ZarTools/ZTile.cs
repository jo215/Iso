using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;

namespace ZarTools
{
    /// <summary>
    /// The base type of a tile.
    /// </summary>
    public enum TileType {
        Wall, Floor, Object, Stairs, Roof, Unknown
    }

    /// <summary>
    /// The base material of a tile.
    /// </summary>
    public enum TileMaterial {
        Stone, Gravel, Metal, Wood, Water, Snow, Ladder, Unknown
    }

    /// <summary>
    /// Flags pertaining to tiles.
    /// </summary>
    public enum TileFlag {
        Ethereal, HurtsNPC, Window, NoShadow, Climbable, NoPop, Exit, Invisible
    }

    /// <summary>
    /// Represents a Tile.
    /// </summary>
    public class ZTile
    {
	    //	Tile properties
        public string RelativePath { get; private set; }
        public string Name { get; private set; }
        public string Tileset { get; private set; }
        public int[] BoundingBox { get; private set; }				//	Bounding box - height, width, depth in some order
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Width2 { get; private set; }
        public int Height2 { get; private set; }
        public TileType TileType { get; private set; }
        public TileMaterial Material { get; private set; }
        public List<TileFlag> Flags { get; private set; }

        private List<Texture2D> textures;
        private List<ImageSource> images;
        public ImageSource Image
        {
            get
            {
                return GetImage();
            }
        }
        public List<Bitmap> Bitmaps { get; set; }

        private int version;			                    //	Generally 9
        private int unknown1;
        private int unknown2;
        private int ticks;

	    //	Temporary Zar info
	    private List<Zar> zars;
	    //	Where the actual image info starts for when we are ready to grab Bitmaps
        private int zarStartPos;
        private int subType;
        private System.Drawing.Color[] palette;
        private int defaultPaletteIndex;
	    //	Variables for extracting/drawing
        private byte[] buffer;
	    private int zPos, drawPos, bufferPos;
        private Bitmap bm;

	    /**
	     * Constructor.
	     */
	    public ZTile(String filename) {
		    zars = new List<Zar>();
		    textures = new List<Texture2D>();
		    Flags = new List<TileFlag>();
            Bitmaps = new List<Bitmap>();
            images = new List<ImageSource>();
		    //	Find the file - create an input stream, cache the bytes
            try
            {
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                FileInfo fi = new FileInfo(filename);
                long FileSize = fi.Length;

                buffer = br.ReadBytes((int)FileSize);
                Name = fi.Name.ToLower().Substring(0, fi.Name.Length - 4);
                bufferPos = 0;
                RelativePath = fi.FullName.ToLower();
                if (RelativePath.IndexOf("tiles") >= 0)
                    RelativePath = RelativePath.Substring(RelativePath.IndexOf("tiles"));
                readTileHeader();
                while (bufferPos < buffer.Length && (buffer.Length - bufferPos > 1200))
                {	//	This is a guess
                    readZarHeader();
                    readPalette();
                    readRLEBlocks();
                    zarStartPos = bufferPos;
                }
                makeBitmaps();
                fs.Close();
                fs.Dispose();
                br.Close();
                br.Dispose();

                palette = null;
                buffer = null;
                zars = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
	    }

        
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Draws the Tile as a Texture2D.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="vector2"></param>
        /// <param name="color"></param>
        public void DrawTexture(SpriteBatch spriteBatch, Vector2 position, Microsoft.Xna.Framework.Color color)
        {
            //  If we only have the Bitmaps then create the Textures
            if (textures.Count == 0)
                for (int i = 0; i < Bitmaps.Count; i++)
                {
                    textures.Add(BitmapTools.ToTexture2D(spriteBatch.GraphicsDevice, Bitmaps[i]));

                    //Bitmaps[i] = null;
                }
            //Bitmaps = null;
            spriteBatch.Draw(textures[0], position, color);
        }

        /// <summary>
        /// Returns the tile as an ImageSource for integration with WPF
        /// </summary>
        /// <returns></returns>
        private ImageSource GetImage()
        {
            //  Create the images if necessary
            if (images.Count == 0)
            {
                for (int i = 0; i < Bitmaps.Count; i++)
                {
                    images.Add(BitmapTools.ToImageSource(Bitmaps[i]));
                    //Bitmaps[i] = null;
                }
                //Bitmaps = null;
            }
            if (images.Count == 0)
                Console.WriteLine(Name + " has 0 images.");
            return images[0];
        }

	    /**
	     * Converts each of this tiles zar's to a Bitmap.
	     */
	    private void makeBitmaps() {
		    for(int j = 0; j < zars.Count; j++) {
                bm = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			    Zar currentZar = zars[j];
			    //	Sequentially decode the RLE blocks
			    zPos = 0;
			    drawPos = 0;
			    List<int> RLEblocks = currentZar.RLEblocks;
                //	Draw
			    while ( zPos < RLEblocks.Count) {
				    //	2-bit Command / 6-bit blockLength
				    int command = RLEblocks[zPos] & 3 ;
				    int blockLength = RLEblocks[zPos] >> 2;
				    zPos ++;
				    //	Carry out RLE command
				    for ( int i = 0; i < blockLength; i ++ ) {
					    switch (command) {
						    case 0 :	//	Skip the next blockLength pixels
							    drawPos ++;
						    break;
						    case 1 :	//	Pixel RGB from palette, alpha = 255
							    fastSetPixel(palette[RLEblocks[zPos]].R, palette[RLEblocks[zPos]].G, palette[RLEblocks[zPos]].B, 255);
						    break;
						    case 2 :	//	Pixel RGB from palette, alpha value (pair - so extra zPos increment)
							    fastSetPixel(palette[RLEblocks[zPos]].R, palette[RLEblocks[zPos]].G, palette[RLEblocks[zPos]].B, (int)RLEblocks[zPos + 1] );
							    zPos ++;
						    break;
						    case 3 :	//	Pixel RGB from palette's default color, alpha values
							    fastSetPixel(palette[defaultPaletteIndex].R, palette[defaultPaletteIndex].G, palette[defaultPaletteIndex].B, (int)RLEblocks[zPos]);
						    break;
					    }
				    }
			    }
			    //	Tile fully drawn - add it to list
                Bitmaps.Add(bm);
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
        private void fastSetPixel(int r, int g, int b, int a)
        {
            bm.SetPixel(drawPos % Width, drawPos / Width, System.Drawing.Color.FromArgb(a, r, g, b));
            drawPos++;
            zPos++;
        }

	    /**
	     * Reads the compressed image data.
	     */
	    private void readRLEBlocks() {
		    int RLEsize = (int) getInt();
		    //	Read the RLE blocks
		    zars.Add(new Zar(new List<int>(), Width, Height, subType, defaultPaletteIndex, 0, 0, null));
		    for (int j = 0; j < RLEsize; j ++)
			    zars[zars.Count - 1].RLEblocks.Add(getByte());
	    }

	    /**
	     * Reads the palette information.
	     */
	    private void readPalette() {
		    //	palette present flag
		    int palPresent = getByte();
		    if (palPresent == 0)
			    Console.WriteLine("Zar must include palette; exiting.");
		    //	read 1 palette
		    long paletteSize = getInt();
		    if (paletteSize > 256)
			    Console.WriteLine("Wrong number of colors in palette.");
		    palette = new System.Drawing.Color[(int)paletteSize];
		    for ( int j = 0; j < paletteSize ; j ++ ) {
			    //	Palette stored as 4 byte BGRA
			    int b = getByte();
                int g = getByte();
                int r = getByte();
                int a = getByte();
			    palette[j] = System.Drawing.Color.FromArgb(a, r, g, b);
		    }
		    //	Read default color
		    defaultPaletteIndex = 0;
		    if (subType == 4)
                defaultPaletteIndex = getByte();
	    }

	    /**
	     * Reads zar-specific information.
	     */
	    private void readZarHeader() {
		    bufferPos = zarStartPos;
		    //	zar subtype 3 or 4 - if 4 then default color included
		    subType = getByte();
		    while( subType < 51 || subType > 52)
                subType = getByte();
			subType = subType - 48;	//	3 or 4
		    //	miss +7	dummy variable
            getByte();
		    //	width + height
		    long zWidth = getInt();
            long zHeight = getInt();
		    Width2 = (int) zWidth;
            Height2 = (int) zHeight;
	    }

	    /**
	     * Reads tile property information.
	     */
	    private void readTileHeader() {
		    bufferPos = 7;		//	<tile>0
		    //	version
		    version = getByte() - 48;
		    //	skip unknown byte probably 0
		    bufferPos++;
		    if (version == 1) bufferPos++;
		    BoundingBox = new int[3];
		    BoundingBox[0] = getByte();
		    BoundingBox[1] = getByte();
		    BoundingBox[2] = getByte();
		    //	tile specific info
		    this.unknown1 = (int) getInt();
		    this.unknown2 = (int) getInt();
		    Width = (int) getInt();
		    Height = (int) getInt();
		    int type = getByte();
		    if (type <= 6)
			    this.TileType = (TileType) type;
		    else
			    Console.WriteLine(Name + " --> unknown TileType: " + type);
		    int mat = getByte();
		    if (mat <= 7)
			    this.Material = (TileMaterial) mat;
		    else
			    Console.WriteLine(Name + " --> unknown TileMaterial: " + mat);
		    //	tile flags
            if (version == 7 || version == 8)
                getByte();
		    int flag = getByte();
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
		    zarStartPos = bufferPos + 17;
	    }
	
	    /**
	     * To String
	     */
	    public String toString() { return this.Name; }

        //  Helper methods.
        private byte getByte()
        {
            byte result = buffer[bufferPos];
            bufferPos++;
            return result;
        }

        private short getShort()
        {
            short result = BitConverter.ToInt16(buffer, bufferPos);
            bufferPos += 2;
            return result;
        }

        private int getInt()
        {
            int result = BitConverter.ToInt32(buffer, bufferPos);
            bufferPos += 4;
            return result;
        }

        private void positionBuffer(int p)
        {
            bufferPos = p;
        }
    }
}
