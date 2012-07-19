using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
namespace ZarTools
{
    /// <summary>
    /// A .zar-based sprite encapsulated with its animations.
    /// </summary>
    internal class BaseZarSprite
    {
        bool debug = false;

        //  A dictionary of all loaded and decoded sprites
        private static Dictionary<string, BaseZarSprite> sprites;

        //  Instance info
        private GraphicsDevice device;
        public string FileName { get; private set; }
        internal long FileSize { get; private set; }
        public Vector2 Center { get; private set; }
        internal Dictionary<string, AnimationSequence> Sequences { get; private set; }
        internal AnimationCollection[] Collections { get; private set; }
        internal int layers;

        //  Temporary variables
        private static byte[] buffer;
        private static int bufferPos;
        private static byte[] result;
        private static int[] unsignedResult;
        private int zarPos;
        private static Color[,] palette;

        /// <summary>
        /// Private constructor. 
        /// </summary>
        /// <param name="fileName"></param>
        private BaseZarSprite(GraphicsDevice device, String fileName)
        {
            this.FileName = fileName;
            this.device = device;
            if (debug)
                Console.Write("Loading Zar definition: " + fileName + "...");
            mapFileToBuffer();
            int seqCount = readFileHeader();
            readSequences(seqCount);
            readCollections();
            int imgType = readAnimHeader();
            if (imgType != 2)
            {
                Console.WriteLine("Unknown type image!");
            }
            //  We can get rid of this and do lazily later
            //readAllAnimations();
        }

        /// <summary>
        /// Will decode all animations (time consuming).
        /// </summary>
        private void readAllAnimations()
        {
            for (int i = 0; i < Collections.Length; i++)
                readAnimation(i);

        }

        /// <summary>
        /// Decodes a specific animation.
        /// </summary>
        /// <param name="i"></param>
        internal void readAnimation(int n)
        {

            long nextPos = FileSize;
            if (n < Collections.Length - 1)
                nextPos = Collections[n + 1].fileOffset;
            int startPos = Collections[n].fileOffset + 16;
            readCompressedBlock(startPos, nextPos);
            readCompressedPalettes();
            Collections[n].palette = palette;
            for (int k = 0; k < Collections[n].frameCount * Collections[n].dirCount * 4; k++)
            {
                readZar(n, k);
            }
            
            ZarConverter.makeBims(device, Collections[n]);
            if (debug)
                Console.WriteLine("Loaded sprite " + n);
            //	Release memory
            palette = null;
            Collections[n].zars.Clear();
            result = null;
            unsignedResult = null;
        }

        /// <summary>
        /// Reads an individual .zar image.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="k"></param>
        private void readZar(int i, int k)
        {
            switch (unsignedResult[zarPos])
            {
			    case 0:		//	An empty frame
				    if (layers == 4) Collections[i].zars.Add(new Zar(new List<int>(), 0, 0, 0, 0, 0, 0, null));
				    zarPos ++;
				    break;
			    case 1 :		//	Standard frame definition
				    zarPos ++;
				    int xOff = (int)unsignedResult[zarPos];
				    int yOff = (int)unsignedResult[zarPos + 4];
				    zarPos += 8;
				    //	zar subtype 3 or 4 - if 4 then default color included
				    int subType = 0;
				    if(unsignedResult[zarPos + 6] < 51 || unsignedResult[zarPos + 6] > 52)
					    Console.WriteLine("Unknown zar subtype " + unsignedResult[zarPos + 6]);

				    else
					    subType = (int)unsignedResult[zarPos + 6] - 48;	//	3 or 4
				    //	miss zarPos + 7	(unknown variable)
				    //	width + height
				    int zWidth = (int)unsignedResult[zarPos + 8];
				    int zHeight = (int)unsignedResult[zarPos + 12];
				    //	palette present flag
				    int palPresent = (int)unsignedResult[zarPos + 16];
				    if (palPresent != 0)
					    Console.WriteLine("Zar must not include palette!");
				    //	How many RLE blocks encode this image
				    int RLEsize = toInt(result, zarPos + 17);
				    //	Read the RLE blocks
				    Collections[i].zars.Add(new Zar(new List<int>(), zWidth, zHeight, subType, 0, xOff, yOff, null));
				    for (int j = 0; j < RLEsize; j ++) {
					    Collections[i].zars[Collections[i].zars.Count - 1].RLEblocks.Add(unsignedResult[zarPos + 21 + j]);
				    }
				    zarPos = zarPos + RLEsize + 21;
				    break;
			    default :		//	issue found
				    Console.WriteLine("Unknown frame type : " + unsignedResult[zarPos]);
				    zarPos++;
				    break;
		    }
        }

        /// <summary>
        /// Reads palette information from inflated array.
        /// </summary>
        private void readCompressedPalettes()
        {
            zarPos = 0;
            int paletteSize = toInt(result, zarPos);
            if (paletteSize > 256)
                Console.WriteLine("Wrong number of colors in palette");
            palette = new Color[paletteSize, layers];
            for (int i = 0; i < layers; i++)
            {
                zarPos += 4;
                for (int j = 0; j < paletteSize; j++)
                {
                    //	Palette stored as 4 byte BGRA
                    palette[j,i] = new Color(unsignedResult[zarPos + 2], unsignedResult[zarPos + 1], unsignedResult[zarPos], unsignedResult[zarPos + 3]);
                    zarPos += 4;
                }
            }
        }

        /// <summary>
        ///  Reads 4 bytes from an array and returns the integer equivelent (Little-Endian)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int toInt(byte[] bytes, int offset)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value += bytes[i + offset] << (8 * i);
            }
            return value;
        }

        /// <summary>
        /// Uncompresses data from file to 2 arrays.
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="nextPos"></param>
        private void readCompressedBlock(int startPos, long nextPos)
        {
            bufferPos = startPos;
            int plainSize = getInt();
            Inflater inf = new Inflater(false);
            byte[] input;
            if (nextPos - startPos > buffer.Length - bufferPos)
                input = new byte[buffer.Length - bufferPos];
            else
                input = new byte[nextPos-startPos];
            result = new byte[plainSize];
            getBytes(input);
            inf.SetInput(input);
            unsignedResult = new int[inf.Inflate(result)];
            for (int l = 0; l < unsignedResult.Length; l++)
                unsignedResult[l] = (int) result[l];
        }

        private void getBytes(byte[] input)
        {
            for (int i = 0; i < input.Length; i ++)
            {
                input[i] = getByte();
            }
        }

        /// <summary>
        /// Reads the animation header information.
        /// </summary>
        /// <returns></returns>
        private int readAnimHeader()
        {
            bufferPos += 14;
            char imgChar = (char)getByte();
            getByte();
            if (imgChar == 50)
                if (debug)
                    Console.WriteLine("Type 2 images (compressed).");
            else if (imgChar == 49)
                Console.WriteLine("Type 1 images (uncompressed). Palette save code required.");
            else
                Console.WriteLine("Unknown image types.");
            return imgChar - 48;
        }

        /// <summary>
        /// Reads the animation collection information.
        /// </summary>
        private int readCollections()
        {
            int animCount = getShort();
            layers = 4;
            //  Exceptions for single-layer zars
            if (FileName.Contains("Enclave") ||FileName.Contains("Assault") || FileName.Contains("Omega") || FileName.Contains("Deathclaw")
                || FileName.Contains("Goliath") || FileName.Contains("WBOS"))
                layers = 1;
            if (debug)
                Console.WriteLine("Found: " + animCount + " collections");
            Collections = new AnimationCollection[animCount];
            bufferPos += 2;
            for (int k = 0; k < animCount; k++)
            {
                bufferPos += 12;
                int fileOffset = getInt();
                int nameLen = getInt();
                string colName = "";
                for (int j = 0; j < nameLen; j++)
                    colName += (char)getByte();
                int frameCount = getInt();
                int dirCount = getInt();
                if (debug)
                    Console.WriteLine("Collection: " + k + " " + colName + " - Frame count = " + frameCount + " directions = " + dirCount);
                int total = frameCount * dirCount;
                Rectangle[] frameRect = new Rectangle[total];
                for (int a = 0; a < frameCount; a ++)
                    for (int b = 0; b < dirCount; b++)
                        frameRect[frameCount * b + a] = new Rectangle(getInt(), getInt(), getInt(), getInt());
                Rectangle collectionRect = frameRect[0];
                for (int a = 1; a < frameCount * dirCount; a++)
                {
                    if (frameRect[a].X < collectionRect.X) collectionRect.X = frameRect[a].X;
                    if (frameRect[a].Y < collectionRect.Y) collectionRect.Y = frameRect[a].Y;
                    if (frameRect[a].Width > collectionRect.Width) collectionRect.Width = frameRect[a].Width;
                    if (frameRect[a].Height > collectionRect.Height) collectionRect.Height = frameRect[a].Height;
                }
                Collections[k] = new AnimationCollection(fileOffset, colName, frameCount, dirCount, frameRect, null, collectionRect, this);
            }
            return animCount;
        }

        /// <summary>
        /// Reads the individual animation sequences.
        /// </summary>
        /// <param name="seqCount"></param>
        private void readSequences(int seqCount)
        {
            Sequences = new Dictionary<string, AnimationSequence>();
            for (int i = 0; i < seqCount; i++)
            {
                int numSeq = getInt();
                List<int> frames = new List<int>();
                Dictionary<int, List<Object>> events = new Dictionary<int, List<object>>();
                int item;
                int j = 0;
                int total = 0;
                events.Add(total, new List<object>());
                while (j < numSeq)
                {
                    item = getShort();
                    if (item >= 0)
                    {
                        //  An animation frame
                        frames.Add(item);
                        total++;
                        events.Add(total, new List<object>());
                    }
                    else
                    {
                        //  An event
                        switch (item)
                        {
                            case -3:    //  Timing
                                events[total].Add(new TimeSpan(0, 0, 0, 0, getShort()));
                                j++;
                                break;
                            case -4:    //  Sequence loop marker
                                events[total].Add("sprloop");
                                break;
                            case -5:    //  Jump? event
                                events[total].Add("sprjump" + getShort());
                                j++;
                                break;
                            case -6:    //  Another sprite overlays this one
                                events[total].Add("sproverlay");
                                break;
                            case -40:   //  Step left
                                events[total].Add("step");
                                break;
                            case -41:   //  Step right
                                events[total].Add("step");
                                break;
                            case -42:   //  Hit by a weapon
                                events[total].Add("hit");
                                break;
                            case -43:   //  Fired a weapon
                                events[total].Add(new Vector3(getShort(), getShort(), getShort()));
                                j += 3;
                                break;
                            case -44:   //  Sound effect
                                events[total].Add("sound");
                                break;
                            case -45:   //  Pick up an object
                                events[total].Add("pickup");
                                break;
                            default:    //  Unknown event
                                Console.WriteLine("Unknown animation event");
                                break;
                        }
                    }
                    j++;
                }
                //  an unknown var
                bufferPos += (numSeq * 4);
                //  sequence name
                int nameLen = getInt();
                string name = "";
                for (int k = 0; k < nameLen; k++)
                    name += (char)getByte();
                //  The collection this sequence relies on
                short collection = getShort();
                //  Create the sequence object
                int[] framesArray = frames.ToArray();
                Sequences.Add(name, new AnimationSequence(name, framesArray, events, collection));
                if (debug)
                    Console.WriteLine("Sequence: " + i + " " + name + " - " + numSeq + " items in this sequence, from collection: " + collection);
            }
        }

        /// <summary>
        /// Reads standard header info.
        /// </summary>
        /// <returns></returns>
        private int readFileHeader()
        {
            positionBuffer(14);
            this.Center = new Vector2(getInt(), getInt());
            //  Unknown short & byte
            getShort();
            getByte();
            //  Sequence count
            int seqCount = getInt();
            if (debug)
                Console.WriteLine("Found " + seqCount + " sequences.");
            return seqCount;
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <param name="device"> </param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static BaseZarSprite GetInstance(GraphicsDevice device, String fileName)
        {
            if (sprites == null)
                sprites = new Dictionary<string, BaseZarSprite>();

            if (!sprites.ContainsKey(fileName))
                sprites.Add(fileName, new BaseZarSprite(device, fileName));

            return sprites[fileName];
        }


        private void mapFileToBuffer()
        {
            try
            {
                FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                FileSize = new FileInfo(FileName).Length;
                buffer = br.ReadBytes((int)FileSize);
                fs.Close();
                fs.Dispose();
                br.Close();
                br.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Couldn't load the file.");
            }
        }

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
