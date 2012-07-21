using System;
using System.Collections.Generic;
using System.Linq;
using ZarTools;
using System.Drawing;

namespace Editor.Model
{
    /// <summary>
    /// An individual Map cell (location)
    /// </summary>
    public class MapCell
    {
        private readonly ZTile[] _layers = new ZTile[5];
        public MapCell ParentCell { get; set; }
        //  Remember to update Clone() before adding any more instance variables!
        internal Point TempPoint;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="layer0"></param>
        /// <param name="layer1"></param>
        /// <param name="layer2"></param>
        /// <param name="layer3"></param>
        /// <param name="layer4"></param>
        public MapCell(ZTile layer0, ZTile layer1, ZTile layer2, ZTile layer3, ZTile layer4)
        {
            _layers[0] = layer0;
            _layers[1] = layer1;
            _layers[2] = layer2;
            _layers[3] = layer3;
            _layers[4] = layer4;
        }
        public MapCell(ZTile floor) : this(floor, null, null, null, null) { }
        public MapCell() : this(null, null, null, null, null) { }
        public MapCell(IList<ZTile> layers) : this(layers[0], layers[1], layers[2], layers[3], layers[4]) { }

        /// <summary>
        /// Returns true if this cell can be entered by characters, flase otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsWalkable()
        {
            for (var layer = 2; layer < Length; layer++)
            {
                if (this[layer] != null)
                    if (this[layer].TileType == TileType.Object || this[layer].TileType == TileType.Wall)
                        if (!this[layer].Flags.Contains(TileFlag.Ethereal) || !this[layer].Flags.Contains(TileFlag.Climbable))
                            return false;
            }
            if (ParentCell != null && ParentCell != this)
                return ParentCell.IsWalkable();
            return true;
        }

        public ZTile[] AllTiles()
        {
            return _layers;
        }

        /// <summary>
        /// Indexer method.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public ZTile this[int i]
        {
            get
            {
                return _layers[i];
            }
            set
            {
                _layers[i] = value;
            }
        }
        
        /// <summary>
        /// Returns the total number of layers in the MapCell.
        /// </summary>
        public int Length
        {
            get { return _layers.Length; }
        }

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        public MapCell Clone()
        {
            var newCell = new MapCell(_layers.ToList()) {ParentCell = ParentCell};
            return newCell;
        }

        /// <summary>
        /// Writes out a description of this cell from file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="map"> </param>
        /// <param name="tileDictionary"> </param>
        internal void ToStream(System.IO.TextWriter file, MapDefinition map, IList<string> tileDictionary)
        {
            foreach (var t in _layers)
            {
                if (t == null)
                {
                    file.Write("-1,");
                }
                else
                {
                    file.Write(tileDictionary.IndexOf(t.RelativePath) + ",");
                }
            }
            if (ParentCell != null)
            {
                var p = map.GetGridRef(ParentCell);
                file.Write(p.X + "," + p.Y);
            }
            else
            {
                file.Write("-1,-1");
            }
            file.Write("\n");
        }

        internal static MapCell FromStream(System.IO.TextReader file, List<ZTile> requiredTiles)
        {
            var readLine = file.ReadLine();
            if (readLine != null)
            {
                var attributes = readLine.Split(',');
                var newCell = new MapCell();
                for (var i = 0; i < 5; i++)
                {
                    if (int.Parse(attributes[i]) != -1)
                    {
                        newCell[i] = requiredTiles[int.Parse(attributes[i])];
                    }
                }

                newCell.TempPoint = new Point(int.Parse(attributes[5]), int.Parse(attributes[6]));

                return newCell;
            }
            Console.WriteLine("MapCell: file was null.");
            return null;
        }
    }
}
