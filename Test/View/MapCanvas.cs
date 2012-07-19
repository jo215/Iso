using System;
using System.Drawing;
using System.Threading;
using ISOTools;
using ZarTools;
using System.Windows.Threading;
using Editor.Model;
using System.Collections.Generic;
using System.Linq;

namespace Editor.View
{
    public class MapCanvas
    {
        public MapDefinition Map { get; set; }
        public System.Windows.Controls.Image ImageControl { get; set; }
        public bool ShowGrid { get; set; }

        Bitmap canvas, tempCanvas, greenGrid, redGrid;
        Dispatcher Dispatcher;
        Graphics g, g2;
        Isometry iso;
        string baseTileDir;
        volatile bool backgroundBusy;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="map"></param>
        public MapCanvas(MapDefinition map, Dispatcher dispatcher, string baseTileDirectory, Isometry iso)
        {
            this.Map = map;
            Dispatcher = dispatcher;
            ShowGrid = false;
            this.baseTileDir = baseTileDirectory;
            //  Grid bitmaps
            greenGrid = (Bitmap)Bitmap.FromFile(baseTileDir + "walkable.png");
            greenGrid.SetResolution(96, 96);
            redGrid = new Bitmap(baseTileDir + "notWalkable.png");
            redGrid.SetResolution(96, 96);
            //  Main canvas updated from UI thread
            canvas = new Bitmap(map.Width * map.TileWidth, map.Height * map.TileHeight / 2);
            g = Graphics.FromImage(canvas);
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            //  Canvas for background thread
            tempCanvas = new Bitmap(map.Width * map.TileWidth, map.Height * map.TileHeight / 2);
            //  Iso helper
            this.iso = iso;
            //  Render entire map
            ThreadPool.QueueUserWorkItem(RenderMap, null);
        }

        /// <summary>
        /// Renders the entire map (should be called on a separate thread.)
        /// </summary>
        /// <param name="state"></param>
        public void RenderMap(object state)
        {
            if (backgroundBusy)
                return;
            backgroundBusy = true;
            if (tempCanvas.Width != Map.Width * Map.TileWidth || tempCanvas.Height != Map.Height * Map.TileHeight / 2)
            {
                tempCanvas.Dispose();
                tempCanvas = new Bitmap(Map.Width * Map.TileWidth, Map.Height * Map.TileHeight / 2);
            }
            //  We have to reset the Graphics since we are multithreaded

            g2 = Graphics.FromImage(tempCanvas);
            g2.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g2.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            //  Clear bitmap
            g2.Clear(Color.Black);
            //  Plot tiles
            for (int row = 0; row < Map.Cells.GetLength(1); row++)
                for (int col = 0; col < Map.Cells.GetLength(0); col++)
                    for (int layer = 0; layer < Map.Cells[0, 0].Length; layer++)
                        drawSingleTile(layer, row, col, g2);
            //  Use the UI Dispatcher thread to swap the bitmaps
            swapGridsSafely(tempCanvas);
            backgroundBusy = false;
        }

        /// <summary>
        /// Draws a single tile.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        private void drawSingleTile(int layer, int row, int col, Graphics gr)
        {

            if (layer == 2 && ShowGrid)
            {
                drawGridTile(row, col, gr);
            }
            //  Check for null (no tile)
            if (Map.Cells[col, row][layer] != null)
            {
                //  Base drawing point for a 1x1 tile
                Point drawPoint = iso.TilePlotter(new Point(col, row));

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
                    if (Map.Cells[col, row][layer].Name.Contains("se") || Map.Cells[col, row][layer].Name.Contains("nw"))
                    {

                    }
                    else
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
            
        }

        /// <summary>
        /// Draws the grid at the given location.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="gr"></param>
        private void drawGridTile(int row, int col, Graphics gr)
        {
            Point drawPoint = iso.TilePlotter(new Point(col, row));
            drawPoint.X++;
            drawPoint.Y++;

            if (Map.Cells[col, row].IsWalkable())
                lock (greenGrid)
                {
                    gr.DrawImage(greenGrid, drawPoint);
                }
            else
                lock (redGrid)
                {
                    gr.DrawImage(redGrid, drawPoint);
                }
        }

        /// <summary>
        /// Re-renders a collection of specific map cells.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        public void adaptiveTileRefresh(IOrderedEnumerable<Point> locations)
        {
            foreach (Point location in locations)
            {
                if (Map.IsOnGrid(location))
                {
                    //  Draw all layers
                    for (int layer = 0; layer < Map.Cells[0, 0].Length; layer++)
                        //  Check for null (no tile)
                        drawSingleTile(layer, location.Y, location.X, g);
                }
            }
            //  Tell the owning Image to update
            if (ImageControl != null)
                ImageControl.Source = BitmapTools.ToBitmapSource(canvas);
            //  We can run out of memory fast if we don't collect garbage here
            GC.Collect(2);
        }

        /// <summary>
        /// Re-renders a single map cell.
        /// </summary>
        /// <param name="location"></param>
        public void adaptiveTileRefresh(Point location)
        {
            List<Point> cells = new List<Point>();
            cells.Add(location);
            adaptiveTileRefresh(cells.OrderBy(x => x.Y));
        }

        /// <summary>
        /// Makes sure map canvas updates are updated on the UI thread.
        /// </summary>
        /// <param name="bitmapToSwap"></param>
        private void swapGridsSafely(Bitmap bitmapToSwap)
        {
            if (Dispatcher.CheckAccess())
            {
                if (ImageControl != null)
                    ImageControl.Source = BitmapTools.ToBitmapSource(bitmapToSwap);
                if (canvas != bitmapToSwap)
                {
                    //  We're changing the canvas bitmap so need to change the graphics context
                    tempCanvas = canvas;
                    canvas = bitmapToSwap;
                    g = g2;
                }
                //  We can run out of memory fast if we don't collect garbage here
                GC.Collect(2);
            }
            else
                Dispatcher.BeginInvoke(new Action<Bitmap>(swapGridsSafely), bitmapToSwap);
        }
    }
}
