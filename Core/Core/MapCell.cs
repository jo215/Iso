using System;
using System.Collections.Generic;
using System.Linq;
using ZarTools;
using System.Drawing;
using Core.AStar;

namespace Core
{
    /// <summary>
    /// An individual Map cell (location)
    /// </summary>
    public class MapCell : IHasNeighbours<MapCell>
    {
        private readonly ZTile[] _layers = new ZTile[5];
        public MapCell ParentCell { get; set; }
        //  Remember to update Clone() before adding any more instance variables!
        internal Point TempPoint;
        
        //  These are set on creation
        public Point MapCoordinate { get; set; }
        public MapDefinition Map { get; set; }

        //  Denotes if a character is occupying this cell.
        public bool IsOccupied { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="layer0"></param>
        /// <param name="layer1"></param>
        /// <param name="layer2"></param>
        /// <param name="layer3"></param>
        /// <param name="layer4"></param>
        public MapCell(MapDefinition map, Point coords, ZTile layer0, ZTile layer1, ZTile layer2, ZTile layer3, ZTile layer4)
        {
            Map = map;
            MapCoordinate = coords;
            _layers[0] = layer0;
            _layers[1] = layer1;
            _layers[2] = layer2;
            _layers[3] = layer3;
            _layers[4] = layer4;
            IsOccupied = false;
        }
        public MapCell(MapDefinition map, Point coords, ZTile floor) : this(map, coords, floor, null, null, null, null) { }
        public MapCell(MapDefinition map, Point coords) : this(map, coords, null, null, null, null, null) { }
        public MapCell(MapDefinition map, Point coords, IList<ZTile> layers) : this(map, coords, layers[0], layers[1], layers[2], layers[3], layers[4]) { }

        /// <summary>
        /// Returns true if this cell can be entered by characters, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsWalkable()
        {
            if (IsOccupied)
                return false;
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
            var newCell = new MapCell(Map, MapCoordinate, _layers.ToList()) {ParentCell = ParentCell};
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
                var p = ParentCell.MapCoordinate;
                file.Write(p.X + "," + p.Y);
            }
            else
            {
                file.Write("-1,-1");
            }
            file.Write("\n");
        }

        internal static MapCell FromStream(System.IO.TextReader file, List<ZTile> requiredTiles, MapDefinition map, Point coords)
        {
            var readLine = file.ReadLine();
            if (readLine != null)
            {
                var attributes = readLine.Split(',');
                var newCell = new MapCell(map, coords);
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

        /// <summary>
        /// Returns the list of MapCells which surround this one.
        /// </summary>
        public IEnumerable<MapCell> Neighbours
        {
            get {
                List<MapCell> neighbours = new List<MapCell>();
                foreach (CompassDirection dir in Enum.GetValues(typeof(CompassDirection)))
                {
                    Point p = Map.Iso.TileWalker(MapCoordinate, dir);
                    if (Map.IsOnGrid(p))
                    {
                        neighbours.Add(Map.Cells[p.X, p.Y]);
                    }
                    
                }
                return neighbours;
            }
        }

        /// <summary>
        /// Returns the exact distance between 2 neighbours.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(MapCell n, MapCell goal)
        {
            if (!goal.IsWalkable())
                return double.MaxValue;

            //  Make sure we convert to diamond style coodinates
            Microsoft.Xna.Framework.Point nCoord, goalCoord;
            if (n.Map.Iso.Style == IsometricStyle.Staggered)
            {
                nCoord = n.Map.Iso.StaggeredToDiamond(n.MapCoordinate.X, n.MapCoordinate.Y);
                goalCoord = n.Map.Iso.StaggeredToDiamond(goal.MapCoordinate.X, goal.MapCoordinate.Y);
            }
            else
            {
                nCoord = new Microsoft.Xna.Framework.Point(n.MapCoordinate.X, n.MapCoordinate.Y);
                goalCoord = new Microsoft.Xna.Framework.Point(goal.MapCoordinate.X, goal.MapCoordinate.Y);
            }
            var h_diagonal = Math.Min(Math.Abs(nCoord.X - goalCoord.X), Math.Abs(nCoord.Y - goalCoord.Y));

            if (h_diagonal > 0)
                return 1.4121;
            else
                return 1;
        }

        /// <summary>
        /// A 8-way diagonal movement heuristic for A*
        /// </summary>
        /// <param name="n"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static double DiagonalHeuristic(MapCell n, MapCell goal)
        {
            //  Movement costs for straight / diagonal
            var D = 1;
            var D2 = 1.4121;

            //  Make sure we convert to diamond style coodinates
            Microsoft.Xna.Framework.Point nCoord, goalCoord;
            if (n.Map.Iso.Style == IsometricStyle.Staggered)
            {
                nCoord = n.Map.Iso.StaggeredToDiamond(n.MapCoordinate.X, n.MapCoordinate.Y);
                goalCoord = n.Map.Iso.StaggeredToDiamond(goal.MapCoordinate.X, goal.MapCoordinate.Y);
            }
            else
            {
                nCoord = new Microsoft.Xna.Framework.Point(n.MapCoordinate.X, n.MapCoordinate.Y);
                goalCoord = new Microsoft.Xna.Framework.Point(goal.MapCoordinate.X, goal.MapCoordinate.Y);
            }

            var h_diagonal = Math.Min(Math.Abs(nCoord.X - goalCoord.X), Math.Abs(nCoord.Y - goalCoord.Y));
            var h_straight = (Math.Abs(nCoord.X - goalCoord.X) + Math.Abs(nCoord.Y - goalCoord.Y));

            var hn = D2 * h_diagonal + D * (h_straight - 2*h_diagonal);

            return hn;
        }
    }
}
