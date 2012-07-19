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
        static string BaseContentDir;
        internal HashSet<Point> undoInfo { get; set; }

        //  Remember to update Clone() before adding any more instance variables!

        /// <summary>
        /// Constructor.
        /// </summary>
        public MapDefinition(ZTile defaultFloorTile, Isometry iso, string baseContentDir)
        {
            BaseContentDir = baseContentDir;
            Iso = iso;
            DefaultFloorTile = defaultFloorTile;

            TileHeight = 37;
            TileWidth = 73;
            
            undoInfo = new HashSet<Point>();
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
                for (int x = 0; x < Cells.GetLength(0); x++)
                    for (int y = 0; y < Cells.GetLength(1); y++)
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
            if (p.X < 0 || p.X >= Cells.GetLength(0) || p.Y < 0 || p.Y >= Cells.GetLength(1))
                return false;
            return true;
        }

        /// <summary>
        /// Saves this map.
        /// </summary>
        public void SaveMap(string filePath)
        {
            if (filePath == null)
                return;
            using (TextWriter file = new StreamWriter(filePath))
            {
                //  Map info
                file.WriteLine("<Map>");
                file.WriteLine(Enum.GetName(typeof(IsometricStyle), Iso.style));
                file.WriteLine(Width);
                file.WriteLine(Height);
                file.WriteLine(TileWidth);
                file.WriteLine(TileHeight);
                file.WriteLine("</Map>");
                //  Set of all used tiles
                file.WriteLine("<TileDictionary>");
                HashSet<string> tileDictionary = new HashSet<string>();

                foreach (MapCell cell in Cells)
                {
                    foreach (ZTile tile in cell.AllTiles())
                    {
                        if (tile != null)
                            tileDictionary.Add(tile.RelativePath);
                    }
                }
                List<string> finalList = tileDictionary.ToList();
                for (int i = 0; i < finalList.Count; i++)
                {
                    file.WriteLine(finalList[i]);
                }
                file.WriteLine("</TileDictionary>");
                //  Map cell info
                file.WriteLine("<Cells>");
                for (int x = 0; x < Cells.GetLength(0); x++)
                    for (int y = 0; y < Cells.GetLength(1); y++)
                        Cells[x, y].ToStream(file, this, finalList);
                file.WriteLine("</Cells>");
            }
        }

        /// <summary>
        /// Opens and returns a map from file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static MapDefinition OpenMap(string filePath, Isometry Iso, bool asString)
        {
            if (filePath == null)
                return new MapDefinition(null, Iso, BaseContentDir) ;
            MapDefinition newMap = new MapDefinition(null, Iso, BaseContentDir);

            TextReader file;
            if (asString)
                file = new StringReader(filePath);
            else
                file = new StreamReader(filePath);
            
                file.ReadLine(); // <Map>
                newMap.Iso.style = (IsometricStyle)Enum.Parse(typeof(IsometricStyle), file.ReadLine());
                newMap.ClearAllCells(int.Parse(file.ReadLine()), int.Parse(file.ReadLine()));
                newMap.TileWidth = int.Parse(file.ReadLine());
                newMap.TileHeight = int.Parse(file.ReadLine());
                file.ReadLine(); // </Map>
                file.ReadLine(); // <TileDictionary>
                List<ZTile> requiredTiles = new List<ZTile>();
                string line = file.ReadLine();
                while (!line.Equals("</TileDictionary>"))
                {
                    requiredTiles.Add(new ZTile("D:\\workspace\\BaseGame\\" + line));
                    
                    line = file.ReadLine();
                }
                Console.WriteLine(requiredTiles[0].Bitmaps.Count());
                file.ReadLine(); // <Cells>
                for (int x = 0; x < newMap.Cells.GetLength(0); x++)
                    for (int y = 0; y < newMap.Cells.GetLength(1); y++)
                    {
                        newMap.Cells[x, y] = MapCell.FromStream(file, requiredTiles);
                    }
                file.ReadLine(); // </Cells>
                //  Remake Parent links
                for (int x = 0; x < newMap.Cells.GetLength(0); x++)
                    for (int y = 0; y < newMap.Cells.GetLength(1); y++)
                    {
                        if (newMap.Cells[x, y].tempPoint.X != -1)
                        {
                            newMap.Cells[x, y].ParentCell = newMap.Cells[newMap.Cells[x, y].tempPoint.X, newMap.Cells[x, y].tempPoint.Y];
                        }
                    }

            file.Close();
            return newMap;
        }

        /// <summary>
        /// Returns a list of cells which contain a portion of the given tile.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        internal HashSet<Point> getFootprint(Point gridPosition, ZTile tile)
        {
            HashSet<Point> children = new HashSet<Point>();
            children.Add(gridPosition);
            if (tile == null)
                return children;
            MapCell parent = Cells[gridPosition.X, gridPosition.Y];

            int ne = tile.BoundingBox[0] / 6;
            int nw = tile.BoundingBox[2] / 6;
            //  Adjust for tile-fraction overlaps
            if (tile.BoundingBox[0] % 6 > 0)
            {
                ne++;
            }
            if (tile.BoundingBox[2] % 6 > 0)
            {
                nw++;
            }
            Point startPosition = gridPosition;
            for (int i = 0; i < ne; i++)
            {
                for (int j = 0; j < nw; j++)
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
            var cloned = new MapDefinition(DefaultFloorTile, Iso, BaseContentDir) {Cells = new MapCell[Width,Height]};
            for (var x = 0; x < Cells.GetLength(0); x++)
                for (var y = 0; y < Cells.GetLength(1); y++)
                    cloned.Cells[x, y] = Cells[x, y].Clone();
            foreach (var p in undoInfo)
            {
                cloned.undoInfo.Add(p);
            }
            return cloned;
        }
    }
}
