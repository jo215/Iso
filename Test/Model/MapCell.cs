using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZarTools;
using System.Drawing;

namespace Editor.Model
{
    /// <summary>
    /// An individual Map cell (location)
    /// </summary>
    public class MapCell
    {
        private ZTile[] layers = new ZTile[5];
        public MapCell ParentCell { get; set; }
        //  Remember to update Clone() before adding any more instance variables!
        internal Point tempPoint;
        /// <summary>
        /// Constructors.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="floor2"></param>
        /// <param name="obj"></param>
        /// <param name="obj2"></param>
        /// <param name="wall"></param>
        public MapCell(ZTile layer0, ZTile layer1, ZTile layer2, ZTile layer3, ZTile layer4)
        {
            layers[0] = layer0;
            layers[1] = layer1;
            layers[2] = layer2;
            layers[3] = layer3;
            layers[4] = layer4;
        }
        public MapCell(ZTile floor) : this(floor, null, null, null, null) { }
        public MapCell() : this(null, null, null, null, null) { }
        public MapCell(List<ZTile> layers) : this(layers[0], layers[1], layers[2], layers[3], layers[4]) { }

        /// <summary>
        /// Returns true if this cell can be entered by characters, flase otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsWalkable()
        {
            for (int layer = 2; layer < Length; layer++)
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
            return layers;
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
                return layers[i];
            }
            set
            {
                layers[i] = value;
            }
        }
        
        /// <summary>
        /// Returns the total number of layers in the MapCell.
        /// </summary>
        public int Length
        {
            get { return layers.Length; }
        }

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns></returns>
        public MapCell Clone()
        {
            MapCell newCell = new MapCell(layers.ToList<ZTile>());
            newCell.ParentCell = this.ParentCell;
            return newCell;
        }

        /// <summary>
        /// Writes out a description of this cell from file.
        /// </summary>
        /// <param name="file"></param>
        internal void ToStream(System.IO.TextWriter file, MapDefinition map, IList<string> tileDictionary)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    file.Write("-1,");
                }
                else
                {
                    file.Write(tileDictionary.IndexOf(layers[i].RelativePath) + ",");
                }
            }
            if (ParentCell != null)
            {
                Point p = map.GetGridRef(ParentCell);
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
            string[] attributes = file.ReadLine().Split(',');
            MapCell newCell = new MapCell();
            for (int i = 0; i < 5; i++)
            {
                if (int.Parse(attributes[i]) != -1)
                {
                    newCell[i] = requiredTiles[int.Parse(attributes[i])];
                }
            }

            newCell.tempPoint = new Point(int.Parse(attributes[5]), int.Parse(attributes[6]));
            
            return newCell;
        }
    }
}
