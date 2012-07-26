using System.Drawing;
using System;

namespace ISOTools
{
    /// <summary>
    /// The styles of isometric map used.
    /// </summary>
    public enum IsometricStyle
    {
        Slide, Staggered, Diamond
    }

    /// <summary>
    /// The eight compass directions.
    /// </summary>
    public enum CompassDirection
    {
        NorthWest = 0, North = 1, NorthEast = 2, East = 3, SouthEast = 4, South = 5, SouthWest = 6, West = 7
    }

    enum MouseMappings
    {
        NW, NE, SW, SE, Center
    }

    /// <summary>
    /// Provides tools for working with isometric projections.
    /// </summary>
    public class Isometry
    {
        public Point ScreenAnchor { get; set; }

        public IsometricStyle Style { get; set; }
        readonly int _tileWidth;
        readonly int _tileHeight;
        static MouseMappings[,] _lookupTable;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Isometry(IsometricStyle isoStyle, string mouseBitmap)
        {
            Style = isoStyle;
            var mouseMap = new Bitmap(mouseBitmap);
            _tileHeight = mouseMap.Height;
            _tileWidth = mouseMap.Width;
            ScreenAnchor = new Point(0, 0);

            //  Create the lookup table from the mouseMap bitmap
            _lookupTable = new MouseMappings[mouseMap.Width, mouseMap.Height];

            var nw = mouseMap.GetPixel(0, 0);
            var ne = mouseMap.GetPixel(mouseMap.Width - 1, 0);
            var sw = mouseMap.GetPixel(0, mouseMap.Height - 1);
            var se = mouseMap.GetPixel(mouseMap.Width - 1, mouseMap.Height - 1);

            for (var x = 0; x < mouseMap.Width; x++)
                for (var y = 0; y < mouseMap.Height; y++)
                {
                    var toTest = mouseMap.GetPixel(x, y);
                    if (toTest == nw)
                        _lookupTable[x, y] = MouseMappings.NW;
                    else if (toTest == ne)
                        _lookupTable[x, y] = MouseMappings.NE;
                    else if (toTest == se)
                        _lookupTable[x, y] = MouseMappings.SE;
                    else if (toTest == sw)
                        _lookupTable[x, y] = MouseMappings.SW;
                    else
                        _lookupTable[x, y] = MouseMappings.Center;
                }
            mouseMap.Dispose();
        }

        /// <summary>
        /// Returns the direction right of current.
        /// </summary>
        /// <param name="currentFacing"></param>
        /// <returns></returns>
        public CompassDirection TurnRight(CompassDirection currentFacing)
        {
            var current = (int)currentFacing + 1;
            if (current == 8)
                current = 0;
            return (CompassDirection)current;
        }

        /// <summary>
        /// Returns the direction left of current.
        /// </summary>
        /// <param name="currentFacing"></param>
        /// <returns></returns>
        public CompassDirection TurnLeft(CompassDirection currentFacing)
        {
            var current = (int)currentFacing - 1;
            if (current == -1)
                current = 7;
            return (CompassDirection)current;
        }

        /// <summary>
        /// Gets the tile at the given world-space pixel.
        /// </summary>
        /// <param name="worldPixel"></param>
        /// <returns></returns>
        public Point MouseMapper(System.Windows.Point worldPixel)
        {
            return MouseMapper(new Point((int)worldPixel.X, (int)worldPixel.Y));
        }

        public Point MouseMapper(Point worldPixel)
        {
            //  #1  Account for screen anchor
            var p = new Point(ScreenAnchor.X, ScreenAnchor.Y);
                     
            //  #2  upper left of tile map is at [0,0]
            var plot = TilePlotter(new Point(0, 0));
            worldPixel.X -= plot.X;
            worldPixel.Y -= plot.Y;

            //  #3  Determine MouseMap coordinates
            //  Find coarse co-ordinates
            var coarse = new Point {X = worldPixel.X/_tileWidth, Y = worldPixel.Y/_tileHeight};
            //  Find fine coordinates
            var fine = new Point {X = worldPixel.X%_tileWidth, Y = worldPixel.Y%_tileHeight};
            //  Adjust for negative fine coordinates
            if (fine.X < 0)
            {
                fine.X += _tileWidth;
                coarse.X--;
            }
            if (fine.Y < 0)
            {
                fine.Y += _tileHeight;
                coarse.Y--;
            }

            //  #4  Perform coarse tile walk
            
            while (coarse.Y < 0)
            {
                //  North movement
                p = TileWalker(p, CompassDirection.North);
                coarse.Y++;
            }
            while (coarse.Y > 0)
            {
                //  South movement
                p = TileWalker(p, CompassDirection.South);
                coarse.Y--;
            }
            while (coarse.X < 0)
            {
                //  West movement
                p = TileWalker(p, CompassDirection.West);
                coarse.X++;
            }
            while (coarse.X > 0)
            {
                //  East movement
                p = TileWalker(p, CompassDirection.East);
                coarse.X--;
            }

            //  #5  Lookup from MouseMap
            switch (_lookupTable[fine.X, fine.Y])
            {
                case MouseMappings.NE:
                    p = TileWalker(p, CompassDirection.NorthEast);
                    break;
                case MouseMappings.NW:
                    p = TileWalker(p, CompassDirection.NorthWest);
                    break;
                case MouseMappings.SE:
                    p = TileWalker(p, CompassDirection.SouthEast);
                    break;
                case MouseMappings.SW:
                    p = TileWalker(p, CompassDirection.SouthWest);
                    break;
            }

            return p;
        }

        /// <summary>
        /// Returns a location a given direction and distance away from the start location.
        /// </summary>
        /// <param name="startTile"></param>
        /// <param name="dir"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Point TileWalker(Point startTile, CompassDirection dir, int distance)
        {
            var p = startTile;
            for (var i = 0; i < distance; i++)
                p = TileWalker(p, dir);
            return p;
        }

        /// <summary>
        /// Returns the tile location adjacent to the passed tile location in the given direction.
        /// </summary>
        /// <param name="startTile"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Point TileWalker(Point startTile, CompassDirection dir)
        {
            var p = new Point(startTile.X, startTile.Y);
            switch (Style)
            {
                case IsometricStyle.Diamond:
                    switch (dir)
                    {
                        case CompassDirection.North:
                            p.X--;
                            p.Y--;
                            break;
                        case CompassDirection.NorthEast:
                            p.Y--;
                            break;
                        case CompassDirection.East:
                            p.X++;
                            p.Y--;
                            break;
                        case CompassDirection.SouthEast:
                            p.X++;
                            break;
                        case CompassDirection.South:
                            p.X++;
                            p.Y++;
                            break;
                        case CompassDirection.SouthWest:
                            p.Y++;
                            break;
                        case CompassDirection.West:
                            p.X--;
                            p.Y++;
                            break;
                        case CompassDirection.NorthWest:
                            p.X--;
                            break;
                    }
                    break;

                case IsometricStyle.Slide:
                    switch (dir)
                    {
                        case CompassDirection.North:
                            p.X++;
                            p.Y -= 2;
                            break;
                        case CompassDirection.NorthEast:
                            p.X++;
                            p.Y--;
                            break;
                        case CompassDirection.East:
                            p.X++;
                            break;
                        case CompassDirection.SouthEast:
                            p.Y++;
                            break;
                        case CompassDirection.South:
                            p.X--;
                            p.Y += 2;
                            break;
                        case CompassDirection.SouthWest:
                            p.X--;
                            p.Y++;
                            break;
                        case CompassDirection.West:
                            p.X--;
                            break;
                        case CompassDirection.NorthWest:
                            p.Y--;
                            break;
                    }
                    break;
                case IsometricStyle.Staggered:
                    switch (dir)
                    {
                        case CompassDirection.North:
                            p.Y -= 2;
                            break;
                        case CompassDirection.NorthEast:
                            p.X += (startTile.Y & 1);
                            p.Y--;
                            break;
                        case CompassDirection.East:
                            p.X++;
                            break;
                        case CompassDirection.SouthEast:
                            p.Y++;
                            p.X += (startTile.Y & 1);
                            break;
                        case CompassDirection.South:
                            p.Y += 2;
                            break;
                        case CompassDirection.SouthWest:
                            p.Y++;
                            p.X += ((startTile.Y & 1) - 1);
                            break;
                        case CompassDirection.West:
                            p.X++;
                            break;
                        case CompassDirection.NorthWest:
                            p.Y--;
                            p.X += ((startTile.Y & 1 )- 1);
                            break;
                    }
                    break;
            }
            return p;
        }

        /// <summary>
        /// Returns the world space coordinates for a given tile location.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public Point TilePlotter(Point tile)
        {
            var p = new Point();
            switch (Style)
            {
                case IsometricStyle.Slide:
                    p.X = tile.X * _tileWidth + tile.Y * _tileWidth / 2;
                    p.Y = tile.Y * _tileHeight / 2;
                    break;
                case IsometricStyle.Staggered:
                    p.X = tile.X * _tileWidth + (tile.Y & 1) * (_tileWidth / 2);
                    p.Y = tile.Y * (_tileHeight / 2);
                    break;
                case IsometricStyle.Diamond:
                    p.X = (tile.X - tile.Y) * _tileWidth / 2;
                    p.Y = (tile.X + tile.Y) * _tileHeight / 2;
                    break;
            }
            return p;
        }

        /// <summary>
        /// Returns the world space coordinates for a given tile location.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public Microsoft.Xna.Framework.Point TilePlotter(Microsoft.Xna.Framework.Point tile)
        {
            var p = TilePlotter(new Point(tile.X, tile.Y));
            return new Microsoft.Xna.Framework.Point(p.X, p.Y);
        }

        /// <summary>
        /// Converts staggered co-ordinates to diamond coordinates.
        /// </summary>
        /// <param name="staggeredX"></param>
        /// <param name="staggeredY"></param>
        /// <returns></returns>
        public Microsoft.Xna.Framework.Point StaggeredToDiamond(int staggeredX, int staggeredY)
        {

            int diamondX = (int) Math.Floor((staggeredY / 2.0) + (staggeredY % 2.0) + staggeredX);
            int diamondY = (int) Math.Floor((staggeredY / 2.0) - staggeredX);

            //int DiamondX = (staggeredY >> 1) + staggeredY & 1 + staggeredX;
            //int DiamondY = (staggeredY >> 1) - staggeredX;
            return new Microsoft.Xna.Framework.Point(diamondX, diamondY);
        }

        /// <summary>
        /// Converts diamond coordinates to staggered coordinates.
        /// </summary>
        /// <param name="diamondX"></param>
        /// <param name="diamondY"></param>
        /// <returns></returns>
        public Microsoft.Xna.Framework.Point DiamondToStaggered(int diamondX, int diamondY)
        {
            int StaggeredX = (diamondX - diamondY) >> 1;
            int StaggeredY = (diamondX + diamondY);
            return new Microsoft.Xna.Framework.Point(StaggeredX, StaggeredY);
        }
    }
}
