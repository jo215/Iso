using System;
using System.Drawing;
using System.Threading;
using ISOTools;
using ZarTools;
using System.Windows.Threading;
using Editor.Model;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Editor.View
{
    public class MapCanvas
    {
        public MapDefinition Map { get; set; }
        ObservableCollection<FactionList> Factions;
        public System.Windows.Controls.Image ImageControl { get; set; }
        public bool ShowGrid { get; set; }

        Bitmap _canvas, _tempCanvas;
        readonly Bitmap _greenGrid, _redGrid;
        readonly Dispatcher _dispatcher;
        Graphics _g, _g2;
        readonly Isometry _iso;
        readonly string _baseTileDir;
        volatile bool _backgroundBusy;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="dispatcher"> </param>
        /// <param name="baseTileDirectory"> </param>
        /// <param name="iso"> </param>
        public MapCanvas(MapDefinition map, Dispatcher dispatcher, string baseTileDirectory, Isometry iso, ObservableCollection<FactionList> factions)
        {
            Map = map;
            Factions = factions;
            _dispatcher = dispatcher;
            ShowGrid = false;
            _baseTileDir = baseTileDirectory;
            //  Grid bitmaps
            _greenGrid = (Bitmap)Image.FromFile(_baseTileDir + "walkable.png");
            _greenGrid.SetResolution(96, 96);
            _redGrid = new Bitmap(_baseTileDir + "notWalkable.png");
            _redGrid.SetResolution(96, 96);
            //  Main canvas updated from UI thread
            _canvas = new Bitmap(map.Width * map.TileWidth, map.Height * map.TileHeight / 2);
            _g = Graphics.FromImage(_canvas);
            _g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            _g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            //  Canvas for background thread
            _tempCanvas = new Bitmap(map.Width * map.TileWidth, map.Height * map.TileHeight / 2);
            //  Iso helper
            _iso = iso;
            //  Render entire map
            ThreadPool.QueueUserWorkItem(RenderMap, null);
        }

        /// <summary>
        /// Renders the entire map (should be called on a separate thread.)
        /// </summary>
        /// <param name="state"></param>
        public void RenderMap(object state)
        {
            if (_backgroundBusy)
                return;
            _backgroundBusy = true;
            if (_tempCanvas.Width != Map.Width * Map.TileWidth || _tempCanvas.Height != Map.Height * Map.TileHeight / 2)
            {
                _tempCanvas.Dispose();
                _tempCanvas = new Bitmap(Map.Width * Map.TileWidth, Map.Height * Map.TileHeight / 2);
            }
            //  We have to reset the Graphics since we are multithreaded

            _g2 = Graphics.FromImage(_tempCanvas);
            _g2.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            _g2.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            //  Clear bitmap
            _g2.Clear(Color.Black);
            //  Plot tiles
            for (var row = 0; row < Map.Cells.GetLength(1); row++)
                for (var col = 0; col < Map.Cells.GetLength(0); col++)
                    for (var layer = 0; layer < Map.Cells[0, 0].Length; layer++)
                        DrawSingleTile(layer, row, col, _g2);
            //  Use the UI Dispatcher thread to swap the bitmaps
            SwapGridsSafely(_tempCanvas);
            _backgroundBusy = false;
        }

        /// <summary>
        /// Draws a single tile.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="gr"> </param>
        private void DrawSingleTile(int layer, int row, int col, Graphics gr)
        {

            if (layer == 2)
            {
                if (ShowGrid)
                {
                    DrawGridTile(row, col, gr);
                }
                DrawCharacters(gr, row, col);
            }
            //  Check for null (no tile)
            if (Map.Cells[col, row][layer] == null) return;
            //  Base drawing point for a 1x1 tile
            var drawPoint = _iso.TilePlotter(new Point(col, row));

            //  Correct for height of tile
            if (Map.Cells[col, row][layer].Height > Map.TileHeight)
            {
                drawPoint.Y -= (Map.Cells[col, row][layer].Height - Map.TileHeight);
            }

            //  And now for width
            drawPoint.X += (6 - Map.Cells[col, row][layer].BoundingBox[2]) * 6;

            //  we use layer 2 for wall filler which needs a different offset
            if (layer == 2)
            {
                if (!(Map.Cells[col, row][layer].Name.Contains("se") || Map.Cells[col, row][layer].Name.Contains("nw")))
                {
                    drawPoint.X -= 12;
                    drawPoint.Y -= 6;
                }
            }

            //  Lock the current ZTile
            lock (Map.Cells[col, row][layer])
            {
                gr.DrawImage(Map.Cells[col, row][layer].Bitmaps[0], drawPoint);

            }
        }

        private void DrawCharacters(Graphics gr, int row, int column)
        {
            List<Unit> units = new List<Unit>();
            foreach (FactionList f in Factions)
            {
                foreach (Unit u in f.Units)
                    units.Add(u);
            }
            foreach (Unit u in units.Where(x => x.X == column && x.Y == row))
            {
                var drawPoint = _iso.TilePlotter(new Point(u.X, u.Y));
                lock (u.Bitmap)
                {
                    drawPoint.X += (u.Bitmap.Width / 2);
                    drawPoint.Y -= u.Bitmap.Height - Map.TileHeight;
                    gr.DrawImage(u.Bitmap, drawPoint);
                }
            }
        }

        /// <summary>
        /// Draws the grid at the given location.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="gr"></param>
        private void DrawGridTile(int row, int col, Graphics gr)
        {
            var drawPoint = _iso.TilePlotter(new Point(col, row));
            drawPoint.X++;
            drawPoint.Y++;

            if (Map.Cells[col, row].IsWalkable())
                lock (_greenGrid)
                {
                    gr.DrawImage(_greenGrid, drawPoint);
                }
            else
                lock (_redGrid)
                {
                    gr.DrawImage(_redGrid, drawPoint);
                }
        }

        /// <summary>
        /// Re-renders a collection of specific map cells.
        /// </summary>
        /// <param name="locations"></param>
        public void AdaptiveTileRefresh(IOrderedEnumerable<Point> locations)
        {
            foreach (var location in locations.Where(location => Map.IsOnGrid(location)))
            {
                //  Draw all layers
                for (var layer = 0; layer < Map.Cells[0, 0].Length; layer++)
                    //  Check for null (no tile)
                    DrawSingleTile(layer, location.Y, location.X, _g);
            }
            //  Tell the owning Image to update
            if (ImageControl != null)
                ImageControl.Source = BitmapTools.ToBitmapSource(_canvas);
            //  We can run out of memory fast if we don't collect garbage here
            GC.Collect(2);
        }

        /// <summary>
        /// Re-renders a single map cell.
        /// </summary>
        /// <param name="location"></param>
        public void AdaptiveTileRefresh(Point location)
        {
            var cells = new List<Point> {location};
            AdaptiveTileRefresh(cells.OrderBy(x => x.Y));
        }

        /// <summary>
        /// Makes sure map canvas updates are updated on the UI thread.
        /// </summary>
        /// <param name="bitmapToSwap"></param>
        private void SwapGridsSafely(Bitmap bitmapToSwap)
        {
            if (_dispatcher.CheckAccess())
            {
                if (ImageControl != null)
                    ImageControl.Source = BitmapTools.ToBitmapSource(bitmapToSwap);
                if (_canvas != bitmapToSwap)
                {
                    //  We're changing the canvas bitmap so need to change the graphics context
                    _tempCanvas = _canvas;
                    _canvas = bitmapToSwap;
                    _g = _g2;
                }
                //  We can run out of memory fast if we don't collect garbage here
                GC.Collect(2);
            }
            else
                _dispatcher.BeginInvoke(new Action<Bitmap>(SwapGridsSafely), bitmapToSwap);
        }
    }
}
