using System.Drawing;

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
        North = 0, NorthEast = 1, East = 2, SouthEast = 3, South = 4, SouthWest = 5, West = 6, NorthWest = 7
    }

    enum MM
    {
        NW, NE, SW, SE, Center
    }

    /// <summary>
    /// Provides tools for working with isometric projections.
    /// </summary>
    public class Isometry
    {
        public Point ScreenAnchor { get; set; }

        public IsometricStyle style { get; set; }
        static int tileWidth;
        static int tileHeight;
        static MM[,] lookupTable;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Isometry(IsometricStyle isoStyle, string mouseBitmap)
        {
            style = isoStyle;
            Bitmap mouseMap = new Bitmap(mouseBitmap);
            tileHeight = mouseMap.Height;
            tileWidth = mouseMap.Width;
            ScreenAnchor = new Point(0, 0);

            //  Create the lookup table from the mouseMap bitmap
            lookupTable = new MM[mouseMap.Width, mouseMap.Height];

            Color NW = mouseMap.GetPixel(0, 0);
            Color NE = mouseMap.GetPixel(mouseMap.Width - 1, 0);
            Color SW = mouseMap.GetPixel(0, mouseMap.Height - 1);
            Color SE = mouseMap.GetPixel(mouseMap.Width - 1, mouseMap.Height - 1);
            
            for (int x = 0; x < mouseMap.Width; x++)
                for (int y = 0; y < mouseMap.Height; y++)
                {
                    Color toTest = mouseMap.GetPixel(x, y);
                    if (toTest == NW)
                        lookupTable[x, y] = MM.NW;
                    else if (toTest == NE)
                        lookupTable[x, y] = MM.NE;
                    else if (toTest == SE)
                        lookupTable[x, y] = MM.SE;
                    else if (toTest == SW)
                        lookupTable[x, y] = MM.SW;
                    else
                        lookupTable[x, y] = MM.Center;
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
            int current = (int)currentFacing + 1;
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
            int current = (int)currentFacing - 1;
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
            Point p = new Point(ScreenAnchor.X, ScreenAnchor.Y);
                     
            //  #2  upper left of tile map is at [0,0]

            Point plot = TilePlotter(new Point(0, 0));
            worldPixel.X -= plot.X;
            worldPixel.Y -= plot.Y;

            //  #3  Determine MouseMap coordinates

            //  Find coarse co-ordinates
            Point coarse = new Point();
            coarse.X = worldPixel.X / tileWidth;
            coarse.Y = worldPixel.Y / tileHeight;
            //  Find fine coordinates
            Point fine = new Point();
            fine.X = worldPixel.X % tileWidth;
            fine.Y = worldPixel.Y % tileHeight;
            //  Adjust for negative fine coordinates
            if (fine.X < 0)
            {
                fine.X += tileWidth;
                coarse.X--;
            }
            if (fine.Y < 0)
            {
                fine.Y += tileHeight;
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
            switch (lookupTable[fine.X, fine.Y])
            {
                case MM.NE:
                    p = TileWalker(p, CompassDirection.NorthEast);
                    break;
                case MM.NW:
                    p = TileWalker(p, CompassDirection.NorthWest);
                    break;
                case MM.SE:
                    p = TileWalker(p, CompassDirection.SouthEast);
                    break;
                case MM.SW:
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
            Point p = startTile;
            for (int i = 0; i < distance; i++)
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
            Point p = new Point(startTile.X, startTile.Y);
            switch (style)
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
            Point p = new Point();
            switch (style)
            {
                case IsometricStyle.Slide:
                    p.X = tile.X * tileWidth + tile.Y * tileWidth / 2;
                    p.Y = tile.Y * tileHeight / 2;
                    break;
                case IsometricStyle.Staggered:
                    p.X = tile.X * tileWidth + (tile.Y & 1) * (tileWidth / 2);
                    p.Y = tile.Y * (tileHeight / 2);
                    break;
                case IsometricStyle.Diamond:
                    p.X = (tile.X - tile.Y) * tileWidth / 2;
                    p.Y = (tile.X + tile.Y) * tileHeight / 2;
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
            Point p = TilePlotter(new Point(tile.X, tile.Y));
            return new Microsoft.Xna.Framework.Point(p.X, p.Y);
        }
    }
}
