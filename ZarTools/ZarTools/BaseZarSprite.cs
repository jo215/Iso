using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace ZarTools
{
    /// <summary>
    /// A .zar-based sprite encapsulated with its animations.
    /// </summary>
    internal class BaseZarSprite
    {
        public bool Debug { get; set; }

        //  A dictionary of all loaded and decoded sprites
        private static Dictionary<string, BaseZarSprite> _sprites;

        //  Instance info
        private readonly GraphicsDevice _device;
        public string FileName { get; private set; }
        internal long FileSize { get; private set; }
        public Vector2 Center { get; private set; }
        internal Dictionary<string, AnimationSequence> Sequences { get; private set; }
        internal AnimationCollection[] Collections { get; private set; }
        internal int Layers;

        //  Temporary variables
        private static byte[] _buffer;
        private static int _bufferPos;
        private static byte[] _result;
        private static int[] _unsignedResult;
        private int _zarPos;
        private static Color[,] _palette;

        /// <summary>
        /// Private constructor. 
        /// </summary>
        /// <param name="device"> </param>
        /// <param name="fileName"></param>
        private BaseZarSprite(GraphicsDevice device, String fileName)
        {
            FileName = fileName;
            _device = device;
            if (Debug)
                Console.Write("Loading Zar definition: " + fileName + "...");
            MapFileToBuffer();
            var seqCount = ReadFileHeader();
            ReadSequences(seqCount);
            ReadCollections();
            var imgType = ReadAnimHeader();
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
        internal void ReadAllAnimations()
        {
            for (var i = 0; i < Collections.Length; i++)
                ReadAnimation(i);

        }

        /// <summary>
        /// Decodes a specific animation.
        /// </summary>
        /// <param name="n"></param>
        internal void ReadAnimation(int n)
        {

            var nextPos = FileSize;
            if (n < Collections.Length - 1)
                nextPos = Collections[n + 1].FileOffset;
            var startPos = Collections[n].FileOffset + 16;
            ReadCompressedBlock(startPos, nextPos);
            ReadCompressedPalettes();
            Collections[n].Palette = _palette;
            for (var k = 0; k < Collections[n].FrameCount * Collections[n].DirCount * 4; k++)
            {
                ReadZar(n);
            }
            
            ZarConverter.MakeBims(_device, Collections[n]);
            if (Debug)
                Console.WriteLine("Loaded sprite " + n);
            //	Release memory
            _palette = null;
            Collections[n].Zars.Clear();
            _result = null;
            _unsignedResult = null;
        }

        /// <summary>
        /// Reads an individual .zar image.
        /// </summary>
        /// <param name="i"></param>
        private void ReadZar(int i)
        {
            switch (_unsignedResult[_zarPos])
            {
			    case 0:		//	An empty frame
				    if (Layers == 4) Collections[i].Zars.Add(new Zar(new List<int>(), 0, 0, 0, 0, 0, 0, null));
				    _zarPos ++;
				    break;
			    case 1 :		//	Standard frame definition
				    _zarPos ++;
                    var xOff = _unsignedResult[_zarPos];
                    var yOff = _unsignedResult[_zarPos + 4];
				    _zarPos += 8;
				    //	zar subtype 3 or 4 - if 4 then default color included
                    var subType = 0;
				    if(_unsignedResult[_zarPos + 6] < 51 || _unsignedResult[_zarPos + 6] > 52)
					    Console.WriteLine("Unknown zar subtype " + _unsignedResult[_zarPos + 6]);

				    else
					    subType = _unsignedResult[_zarPos + 6] - 48;	//	3 or 4
				    //	miss zarPos + 7	(unknown variable)
				    //	width + height
                    var zWidth = _unsignedResult[_zarPos + 8];
                    var zHeight = _unsignedResult[_zarPos + 12];
				    //	palette present flag
                    var palPresent = _unsignedResult[_zarPos + 16];
				    if (palPresent != 0)
					    Console.WriteLine("Zar must not include palette!");
				    //	How many RLE blocks encode this image
                    var rleSize = ToInt(_result, _zarPos + 17);
				    //	Read the RLE blocks
				    Collections[i].Zars.Add(new Zar(new List<int>(), zWidth, zHeight, subType, 0, xOff, yOff, null));
                    for (var j = 0; j < rleSize; j++)
                    {
					    Collections[i].Zars[Collections[i].Zars.Count - 1].RleBlocks.Add(_unsignedResult[_zarPos + 21 + j]);
				    }
				    _zarPos = _zarPos + rleSize + 21;
				    break;
			    default :		//	issue found
				    Console.WriteLine("Unknown frame type : " + _unsignedResult[_zarPos]);
				    _zarPos++;
				    break;
		    }
        }

        /// <summary>
        /// Reads palette information from inflated array.
        /// </summary>
        private void ReadCompressedPalettes()
        {
            _zarPos = 0;
            var paletteSize = ToInt(_result, _zarPos);
            if (paletteSize > 256)
                Console.WriteLine("Wrong number of colors in palette");
            _palette = new Color[paletteSize, Layers];
            for (var i = 0; i < Layers; i++)
            {
                _zarPos += 4;
                for (var j = 0; j < paletteSize; j++)
                {
                    //	Palette stored as 4 byte BGRA
                    _palette[j,i] = new Color(_unsignedResult[_zarPos + 2], _unsignedResult[_zarPos + 1], _unsignedResult[_zarPos], _unsignedResult[_zarPos + 3]);
                    _zarPos += 4;
                }
            }
        }

        /// <summary>
        ///  Reads 4 bytes from an array and returns the integer equivelent (Little-Endian)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int ToInt(IList<byte> bytes, int offset)
        {
            var value = 0;
            for (var i = 0; i < 4; i++)
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
        private void ReadCompressedBlock(int startPos, long nextPos)
        {
            _bufferPos = startPos;
            var plainSize = GetInt();
            var inf = new Inflater(false);
            var input = nextPos - startPos > _buffer.Length - _bufferPos ? new byte[_buffer.Length - _bufferPos] : new byte[nextPos-startPos];
            _result = new byte[plainSize];
            GetBytes(input);
            inf.SetInput(input);
            _unsignedResult = new int[inf.Inflate(_result)];
            for (var l = 0; l < _unsignedResult.Length; l++)
                _unsignedResult[l] = _result[l];
        }

        private void GetBytes(IList<byte> input)
        {
            for (var i = 0; i < input.Count; i++)
            {
                input[i] = GetByte();
            }
        }

        /// <summary>
        /// Reads the animation header information.
        /// </summary>
        /// <returns></returns>
        private int ReadAnimHeader()
        {
            _bufferPos += 14;
            var imgChar = (int)GetByte();
            GetByte();
            switch (imgChar)
            {
                case 50:
                    if (Debug)
                        Console.WriteLine("Type 2 images (compressed).");
                    break;
                case 49:
                    Console.WriteLine("Type 1 images (uncompressed). Palette save code required.");
                    break;
                default:
                    Console.WriteLine("Unknown image types.");
                    break;
            }
            return imgChar - 48;
        }

        /// <summary>
        /// Reads the animation collection information.
        /// </summary>
        private void ReadCollections()
        {
            int animCount = GetShort();
            Layers = 4;
            //  Exceptions for single-layer zars
            if (FileName.Contains("Enclave") ||FileName.Contains("Assault") || FileName.Contains("Omega") || FileName.Contains("Deathclaw")
                || FileName.Contains("Goliath") || FileName.Contains("WBOS"))
                Layers = 1;
            if (Debug)
                Console.WriteLine("Found: " + animCount + " collections");
            Collections = new AnimationCollection[animCount];
            _bufferPos += 2;
            for (var k = 0; k < animCount; k++)
            {
                _bufferPos += 12;
                var fileOffset = GetInt();
                var nameLen = GetInt();
                var colName = "";
                for (var j = 0; j < nameLen; j++)
                    colName += (char)GetByte();
                var frameCount = GetInt();
                var dirCount = GetInt();
                if (Debug)
                    Console.WriteLine("Collection: " + k + " " + colName + " - Frame count = " + frameCount + " directions = " + dirCount);
                var total = frameCount * dirCount;
                var frameRect = new Rectangle[total];
                for (var a = 0; a < frameCount; a++)
                    for (var b = 0; b < dirCount; b++)
                        frameRect[frameCount * b + a] = new Rectangle(GetInt(), GetInt(), GetInt(), GetInt());
                var collectionRect = frameRect[0];
                for (var a = 1; a < frameCount * dirCount; a++)
                {
                    if (frameRect[a].X < collectionRect.X) collectionRect.X = frameRect[a].X;
                    if (frameRect[a].Y < collectionRect.Y) collectionRect.Y = frameRect[a].Y;
                    if (frameRect[a].Width > collectionRect.Width) collectionRect.Width = frameRect[a].Width;
                    if (frameRect[a].Height > collectionRect.Height) collectionRect.Height = frameRect[a].Height;
                }
                Collections[k] = new AnimationCollection(fileOffset, colName, frameCount, dirCount, frameRect, null, collectionRect, this);
            }
        }

        /// <summary>
        /// Reads the individual animation sequences.
        /// </summary>
        /// <param name="seqCount"></param>
        private void ReadSequences(int seqCount)
        {
            Sequences = new Dictionary<string, AnimationSequence>();
            for (var i = 0; i < seqCount; i++)
            {
                var numSeq = GetInt();
                var frames = new List<int>();
                var events = new Dictionary<int, List<object>>();
                var j = 0;
                var total = 0;
                events.Add(total, new List<object>());
                while (j < numSeq)
                {
                    int item = GetShort();
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
                                events[total].Add(new TimeSpan(0, 0, 0, 0, GetShort()));
                                j++;
                                break;
                            case -4:    //  Sequence loop marker
                                events[total].Add("sprloop");
                                break;
                            case -5:    //  Jump? event
                                events[total].Add("sprjump" + GetShort());
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
                                events[total].Add(new Vector3(GetShort(), GetShort(), GetShort()));
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
                _bufferPos += (numSeq * 4);
                //  sequence name
                var nameLen = GetInt();
                var name = "";
                for (var k = 0; k < nameLen; k++)
                    name += (char)GetByte();
                //  The collection this sequence relies on
                var collection = GetShort();
                //  Create the sequence object
                var framesArray = frames.ToArray();
                Sequences.Add(name, new AnimationSequence(name, framesArray, events, collection));
                if (Debug)
                    Console.WriteLine("Sequence: " + i + " " + name + " - " + numSeq + " items in this sequence, from collection: " + collection);
            }
        }

        /// <summary>
        /// Reads standard header info.
        /// </summary>
        /// <returns></returns>
        private int ReadFileHeader()
        {
            PositionBuffer(14);
            Center = new Vector2(GetInt(), GetInt());
            //  Unknown short & byte
            GetShort();
            GetByte();
            //  Sequence count
            var seqCount = GetInt();
            if (Debug)
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
            if (_sprites == null)
                _sprites = new Dictionary<string, BaseZarSprite>();

            if (!_sprites.ContainsKey(fileName))
                _sprites.Add(fileName, new BaseZarSprite(device, fileName));

            return _sprites[fileName];
        }

        /// <summary>
        /// Maps the file to memory.
        /// </summary>
        private void MapFileToBuffer()
        {
            try
            {
                var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                var br = new BinaryReader(fs);
                FileSize = new FileInfo(FileName).Length;
                _buffer = br.ReadBytes((int)FileSize);
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
        private static byte GetByte()
        {
            var result = _buffer[_bufferPos];
            _bufferPos++;
            return result;
        }

        private static short GetShort()
        {
            var result = BitConverter.ToInt16(_buffer, _bufferPos);
            _bufferPos += 2;
            return result;
        }

        private static int GetInt()
        {
            var result = BitConverter.ToInt32(_buffer, _bufferPos);
            _bufferPos += 4;
            return result;
        }

        private static void PositionBuffer(int p)
        {
            _bufferPos = p;
        }

    }
}
