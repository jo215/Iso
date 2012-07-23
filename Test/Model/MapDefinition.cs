using ZarTools;
using System.Drawing;
using ISOTools;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Editor.Model
{
    /// <summary>
    /// An entire map.
    /// </summary>
    public class MapDefinition
    {
        public Isometry Iso { get; set; }
        public MapCell[,] Cells { get; set; }
        public ZTile DefaultFloorTile { get; private set; }
        public int Width { get { return Cells.GetLength(0); } }
        public int Height { get { return Cells.GetLength(1); } }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        static string _baseContentDir;
        internal HashSet<Point> UndoInfo { get; set; }

        //  Remember to update Clone() before adding any more instance variables!

        /// <summary>
        /// Constructor.
        /// </summary>
        public MapDefinition(ZTile defaultFloorTile, Isometry iso, string baseContentDir)
        {
            _baseContentDir = baseContentDir;
            Iso = iso;
            DefaultFloorTile = defaultFloorTile;

            TileHeight = 37;
            TileWidth = 73;
            
            UndoInfo = new HashSet<Point>();
            Cells = new MapCell[0, 0];
            ClearAllCells(26, 45);
        }

        /// <summary>
        /// Clears the map.
        /// </summary>
        public void ClearAllCells(int width, int height)
        {
            lock (Cells)
            {
                Cells = new MapCell[width, height];
                for (var x = 0; x < Cells.GetLength(0); x++)
                    for (var y = 0; y < Cells.GetLength(1); y++)
                        Cells[x, y] = new MapCell(DefaultFloorTile);
            }
        }

        /// <summary>
        /// Checks if a given grid position is valid.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsOnGrid(Point p)
        {
            return p.X >= 0 && p.X < Cells.GetLength(0) && p.Y >= 0 && p.Y < Cells.GetLength(1);
        }

        /// <summary>
        /// Appends this map to the given stream.
        /// </summary>
        /// <param name="stream"></param>
        public void AppendMap(TextWriter stream)
        {
            if (stream == null)
                return;
            //  Map info
            stream.WriteLine("<Map>");
            stream.WriteLine(Enum.GetName(typeof(IsometricStyle), Iso.Style));
            stream.WriteLine(Width);
            stream.WriteLine(Height);
            stream.WriteLine(TileWidth);
            stream.WriteLine(TileHeight);
            stream.WriteLine("</Map>");

            //  Set of all used tiles
            stream.WriteLine("<TileDictionary>");
            var tileDictionary = new HashSet<string>();

            foreach (var cell in Cells)
            {
                foreach (var tile in cell.AllTiles().Where(tile => tile != null))
                {
                    tileDictionary.Add(tile.RelativePath);
                }
            }
            var finalList = tileDictionary.ToList();
            foreach (var t in finalList)
            {
                stream.WriteLine(t);
            }
            stream.WriteLine("</TileDictionary>");

            //  Map cell info
            stream.WriteLine("<Cells>");
            for (var x = 0; x < Cells.GetLength(0); x++)
                for (var y = 0; y < Cells.GetLength(1); y++)
                    Cells[x, y].ToStream(stream, this, finalList);
            stream.WriteLine("</Cells>");
        }

        /// <summary>
        /// Saves this map to a new file.
        /// </summary>
        public void SaveMap(string filePath)
        {
            if (filePath == null)
                return;
            using (var stream = new StreamWriter(filePath))
            {
                AppendMap(stream);
            }
        }

        /// <summary>
        /// Reads a map from the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iso"></param>
        /// <returns></returns>
        public static MapDefinition ReadMap(TextReader stream, Isometry iso)
        {
            var newMap = new MapDefinition(null, iso, _baseContentDir);
            
            if (stream == null)
                return newMap;
            
            stream.ReadLine(); // <Map>
            newMap.Iso.Style = (IsometricStyle)Enum.Parse(typeof(IsometricStyle), stream.ReadLine());
            newMap.ClearAllCells(int.Parse(stream.ReadLine()), int.Parse(stream.ReadLine()));
            newMap.TileWidth = int.Parse(stream.ReadLine());
            newMap.TileHeight = int.Parse(stream.ReadLine());
            stream.ReadLine(); // </Map>

            stream.ReadLine(); // <TileDictionary>
            var requiredTiles = new List<ZTile>();
            var line = stream.ReadLine();
            while (!line.Equals("</TileDictionary>"))
            {
                requiredTiles.Add(new ZTile("D:\\workspace\\BaseGame\\" + line));

                line = stream.ReadLine();
            }
            Console.WriteLine(requiredTiles[0].Bitmaps.Count());

            stream.ReadLine(); // <Cells>
            for (var x = 0; x < newMap.Cells.GetLength(0); x++)
                for (var y = 0; y < newMap.Cells.GetLength(1); y++)
                {
                    newMap.Cells[x, y] = MapCell.FromStream(stream, requiredTiles);
                }
            stream.ReadLine(); // </Cells>

            //  Remake Parent links
            for (var x = 0; x < newMap.Cells.GetLength(0); x++)
                for (var y = 0; y < newMap.Cells.GetLength(1); y++)
                {
                    if (newMap.Cells[x, y].TempPoint.X != -1)
                    {
                        newMap.Cells[x, y].ParentCell = newMap.Cells[newMap.Cells[x, y].TempPoint.X, newMap.Cells[x, y].TempPoint.Y];
                    }
                }
            return newMap;
        }

        /// <summary>
        /// Opens and returns a map from file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="Iso"> </param>
        /// <param name="asString"> </param>
        /// <returns></returns>
        public static MapDefinition OpenMap(string filePath, Isometry Iso, bool asString)
        {
            if (filePath == null)
                return new MapDefinition(null, Iso, _baseContentDir) ;
            
            TextReader file;
            if (asString)
            {
                using (file = new StringReader(filePath))
                {
                    return ReadMap(file, Iso);
                }
            }
            else
            {
                using (file = new StreamReader(filePath))
                {
                    return ReadMap(file, Iso);
                }
            }
        }

        /// <summary>
        /// Returns a list of cells which contain a portion of the given tile.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        internal HashSet<Point> getFootprint(Point gridPosition, ZTile tile)
        {
            var children = new HashSet<Point> {gridPosition};
            if (tile == null)
                return children;

            var ne = tile.BoundingBox[0] / 6;
            var nw = tile.BoundingBox[2] / 6;
            //  Adjust for tile-fraction overlaps
            if (tile.BoundingBox[0] % 6 > 0)
            {
                ne++;
            }
            if (tile.BoundingBox[2] % 6 > 0)
            {
                nw++;
            }
            var startPosition = gridPosition;
            for (var i = 0; i < ne; i++)
            {
                for (var j = 0; j < nw; j++)
                {
                    if (IsOnGrid(gridPosition))
                        children.Add(gridPosition);
                    gridPosition = Iso.TileWalker(gridPosition, CompassDirection.NorthWest);   
                }
                gridPosition = Iso.TileWalker(startPosition, CompassDirection.NorthEast, i + 1);
            }
            return children;
        }

        /// <summary>
        /// Adds a new tile, taking care of regions and other objects.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layer"></param>
        /// <param name="tileToPlace"></param>
        /// <returns></returns>
        internal IOrderedEnumerable<Point> LegallyPlaceTile(Point gridPosition, int layer, ZTile tileToPlace)
        {
            //  Get the footprint of the new tile
            HashSet<Point> footPrint = getFootprint(gridPosition, tileToPlace);
            //  Step thru each cell in the footprint...
            foreach (Point p in footPrint.ToList())
            {
                //  If it has a link then find the parent, delete that and set its children to none
                if (Cells[p.X, p.Y].ParentCell != null)
                {
                    Point parent = GetGridRef(Cells[p.X, p.Y].ParentCell);

                    HashSet<Point> children = getFootprint(parent, Cells[p.X, p.Y].ParentCell[layer]);
                    Cells[p.X, p.Y].ParentCell[layer] = null;
                    foreach (Point q in children)
                    {
                        Cells[q.X, q.Y].ParentCell = null;
                    }    
                    footPrint.UnionWith(children);
                }
                //  If it has an object delete it and set that objects children to none
                if (Cells[p.X, p.Y][layer] != null)
                {
                    HashSet<Point> children = getFootprint(p, Cells[p.X, p.Y][layer]);
                    foreach (Point q in children)
                    {
                        Cells[q.X, q.Y].ParentCell = null;
                    }
                    Cells[p.X, p.Y][layer] = null;
                    footPrint.UnionWith(children);
                }
            }
            foreach (Point p in getFootprint(gridPosition, tileToPlace))
                //  Set each footprint cell's parent
                Cells[p.X, p.Y].ParentCell = Cells[gridPosition.X, gridPosition.Y];
            //  Set the 'actual' position
            Cells[gridPosition.X, gridPosition.Y][layer] = tileToPlace;
            Cells[gridPosition.X, gridPosition.Y].ParentCell = null; 
            //  Now add in extra cells to take into account height
            foreach (Point p in footPrint.ToList())
            {
                footPrint.Add(Iso.TileWalker(p, CompassDirection.North));
                footPrint.Add(Iso.TileWalker(p, CompassDirection.NorthWest));
                footPrint.Add(Iso.TileWalker(p, CompassDirection.NorthEast));
            }
            foreach (Point p in footPrint.ToList())
            {
                footPrint.Add(Iso.TileWalker(p, CompassDirection.North));
                footPrint.Add(Iso.TileWalker(p, CompassDirection.NorthWest));
            }
            return footPrint.ToList().OrderBy(x => x.Y).ThenBy(n => n.X);
        }

        /// <summary>
        /// Gets the location of the given Cell.
        /// </summary>
        /// <param name="mapCell"></param>
        /// <returns></returns>
        public Point GetGridRef(MapCell mapCell)
        {
            for (int x = 0; x < Cells.GetLength(0); x++)
                for (int y = 0; y < Cells.GetLength(1); y++)
                    if (Cells[x, y].Equals(mapCell))
                        return new Point(x, y);
            return new Point();
        }

        /// <summary>
        /// Returns a copy of this MapDefinition.
        /// </summary>
        /// <returns></returns>
        internal MapDefinition Clone()
        {
            var cloned = new MapDefinition(DefaultFloorTile, Iso, _baseContentDir) {Cells = new MapCell[Width,Height]};
            for (var x = 0; x < Cells.GetLength(0); x++)
                for (var y = 0; y < Cells.GetLength(1); y++)
                    cloned.Cells[x, y] = Cells[x, y].Clone();
            foreach (var p in UndoInfo)
            {
                cloned.UndoInfo.Add(p);
            }
            return cloned;
        }
    }
}
