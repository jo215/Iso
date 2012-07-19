using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZarTools;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Windows.Data;
using Editor.Model;
using System.Windows.Controls;
using System.Drawing;
using ISOTools;
using System.Linq;
namespace Editor.View
{
    public class ViewModel
    {
        public Dictionary<string, ObservableCollection<ZTile>> TileSets { get; set; }
        public Dictionary<string, ListCollectionView> TileSetViews { get; set; }

        public MapDefinition Map { get; set; }
        public MapCanvas MapCanvas { get; set; }

        DoubleLinkedListNode<MapDefinition> currentUndoNode;

        Dispatcher UIDispatcher;
        string baseContentDirectory;
        string tempTileSet;
        Isometry iso;
        bool useAltEditLayer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ViewModel(Dispatcher UIDispatcher, string baseContentDir, Isometry iso)
        {
            //  Reference to the UIDispatcher for thread-safe collection manipulation
            this.UIDispatcher = UIDispatcher;
            this.baseContentDirectory = baseContentDir;
            this.iso = iso;
            useAltEditLayer = false;
            TileSets = new Dictionary<string, ObservableCollection<ZTile>>();
            TileSetViews = new Dictionary<string, ListCollectionView>();
           
            Map = new MapDefinition(new ZTile(baseContentDir + "tiles\\Generic Tiles\\Generic Floors\\DirtSand\\Waste_Floor_Gravel_SandDirtCentre_F_1_NE.til"), iso, baseContentDir);
            //Map = MapDefinition.OpenMap("C:\\Users\\samcruise\\Documents\\jim2.jim", iso);
            
            MapCanvas = new MapCanvas(Map, UIDispatcher, baseContentDir + "tiles\\", iso);
            //  Create the available TileSets and collection views
            foreach (DirectoryInfo di in new DirectoryInfo(baseContentDir + "tiles\\").GetDirectories())
            {
                TileSets.Add(di.Name, new ObservableCollection<ZTile>());
                TileSetViews.Add(di.Name, new ListCollectionView(TileSets[di.Name]));
            }

            //  Start a new thread to load in the tile images
            ThreadPool.QueueUserWorkItem(GetWholeTileSet, baseContentDir + "tiles\\");
        }

        /// <summary>
        /// Changes a cell's contents, correcting for layer.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="tileToPlace"></param>
        public void PaintCell(Point gridPosition, ZTile tileToPlace)
        {
            if (!Map.IsOnGrid(gridPosition))
                return;
            //  Set correct layer
            int layer = 1;
            if (!useAltEditLayer)
            {
                layer = 0;
                if (tileToPlace.TileType != TileType.Floor)
                {
                    layer = 3;
                    //  Layers 2 & 4 are generally used for wall filler tiles atm
                    if (tileToPlace.Name.Contains("_ct_") || tileToPlace.Name.Contains("_c_") || tileToPlace.Name.Contains("_d_"))
                        layer = 4;
                    if (tileToPlace.Name.Contains("_b_") || tileToPlace.Name.Contains("b&d"))
                        layer = 2;
                }
            }
            //  Check we're not swapping for the same thing
            if (Map.Cells[gridPosition.X, gridPosition.Y][layer] != tileToPlace)
            {
                //  Update the undo buffer with the old info
                DoubleLinkedListNode<MapDefinition> newNode =
                    new DoubleLinkedListNode<MapDefinition>(Map.Clone(), currentUndoNode);
                if (currentUndoNode != null)
                    currentUndoNode.Next = newNode;
                currentUndoNode = newNode;
                Map.undoInfo.Clear();
                Map.undoInfo.UnionWith(Map.LegallyPlaceTile(gridPosition, layer, tileToPlace));
                MapCanvas.adaptiveTileRefresh(Map.undoInfo.OrderBy(x => x.Y));
            }
        }

        /// <summary>
        /// Undo functionality.
        /// </summary>
        public void Undo()
        {
            if (currentUndoNode == null)
                return;
            MapDefinition temp = Map;
            Map = currentUndoNode.Value;

            currentUndoNode = new DoubleLinkedListNode<MapDefinition>(temp, currentUndoNode.Previous, currentUndoNode.Next);
            if (currentUndoNode.Previous != null)
                currentUndoNode.Previous.Next = currentUndoNode;

            if (currentUndoNode.Previous != null)
                currentUndoNode = currentUndoNode.Previous;
            MapCanvas.Map = Map;
            MapCanvas.adaptiveTileRefresh(Map.undoInfo.Union(temp.undoInfo).OrderBy(x => x.Y).ThenBy(c => c.X));
        }

        /// <summary>
        /// Redo functionality.
        /// </summary>
        public void Redo()
        {
            if (currentUndoNode == null || currentUndoNode.Next == null)
                return;
            currentUndoNode = currentUndoNode.Next;
            MapDefinition temp = Map;
            Map = currentUndoNode.Value;

            currentUndoNode = new DoubleLinkedListNode<MapDefinition>(temp, currentUndoNode.Previous, currentUndoNode.Next);
            if (currentUndoNode.Previous != null)
                currentUndoNode.Previous.Next = currentUndoNode;
            MapCanvas.Map = Map;
            MapCanvas.adaptiveTileRefresh(Map.undoInfo.Union(temp.undoInfo).OrderBy(x => x.Y).ThenBy(c => c.X));
        }

        /// <summary>
        /// Recursively loads all the tile images from the given directory.
        /// </summary>
        /// <param name="state"></param>
        private void GetWholeTileSet(Object state)
        {
            DirectoryInfo di = new DirectoryInfo(state.ToString());
            List<ZTile> tempList =  new List<ZTile>();
            if (TileSets.ContainsKey(di.Name))
                tempTileSet = di.Name;

            foreach (FileInfo fi in di.GetFiles())
                if (fi.Extension.Equals(".til"))
                    tempList.Add(new ZTile(fi.FullName));

            if (tempList.Count > 0)
                AddTilesToCollection(tempList);

            foreach (DirectoryInfo di2 in di.GetDirectories())
                GetWholeTileSet(di2.FullName);
        }

        /// <summary>
        /// Allows adding to the ObservableCollection from any thread.
        /// </summary>
        /// <param name="newItems"></param>
        private void AddTileToCollection(ZTile newItem)
        {
            if (UIDispatcher.CheckAccess())
                TileSets[tempTileSet].Add(newItem);
            else
                UIDispatcher.BeginInvoke(new Action<ZTile>(AddTileToCollection), newItem);
        }

        /// <summary>
        /// Allows adding a list to the ObservableCollection from any thread.
        /// </summary>
        /// <param name="newItems"></param>
        private void AddTilesToCollection(List<ZTile> newItems)
        {
            if (UIDispatcher.CheckAccess())
                newItems.ForEach(x => TileSets[tempTileSet].Add(x));
            else
                UIDispatcher.BeginInvoke(new Action<List<ZTile>>(AddTilesToCollection), newItems);
        }

        /// <summary>
        /// Starts a new map.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal void NewMap(int width, int height)
        {
            currentUndoNode = null;
            Map.ClearAllCells(width, height);
            ThreadPool.QueueUserWorkItem(MapCanvas.RenderMap, null);
        }

        internal void OpenMap(string fileName, Isometry iso)
        {
            currentUndoNode = null;
            MapDefinition tempMap = MapDefinition.OpenMap(fileName, iso, false);
            Map = tempMap;
            MapCanvas.Map = tempMap;

            ThreadPool.QueueUserWorkItem(MapCanvas.RenderMap, null);
        }

        /// <summary>
        /// Shows / hides the walk grid.
        /// </summary>
        internal void ShowGrid()
        {
            MapCanvas.ShowGrid = !MapCanvas.ShowGrid;
            ThreadPool.QueueUserWorkItem(MapCanvas.RenderMap, null);
        }

        /// <summary>
        /// Switches to the 'spare' layer (1) for edits.
        /// </summary>
        internal void SwitchEditLayer()
        {
            useAltEditLayer = !useAltEditLayer;
        }
    }
}
