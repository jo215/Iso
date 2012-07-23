using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZarTools;
using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Windows.Data;
using Editor.Model;
using System.Drawing;
using ISOTools;
using System.Linq;

namespace Editor.View
{
    public class ViewModel
    {
        public Dictionary<string, ObservableCollection<ZTile>> TileSets { get; set; }
        public Dictionary<string, ListCollectionView> TileSetViews { get; set; }

        static public ObservableCollection<FactionList> Factions { get; set; }

        public MapDefinition Map { get; set; }
        public MapCanvas MapCanvas { get; set; }
 
        DoubleLinkedListNode<MapDefinition> _currentUndoNode;

        readonly Dispatcher _uiDispatcher;

        string _tempTileSet;

        bool _useAltEditLayer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ViewModel(Dispatcher uiDispatcher, string baseContentDir, Isometry iso)
        {
            //  Reference to the UIDispatcher for thread-safe collection manipulation
            _uiDispatcher = uiDispatcher;

            Factions = new ObservableCollection<FactionList> { new FactionList("AI"), new FactionList("Player 1"), new FactionList("Player 2") };
            
            //  Map setup
            Map = new MapDefinition(new ZTile(baseContentDir + "tiles\\Generic Tiles\\Generic Floors\\DirtSand\\Waste_Floor_Gravel_SandDirtCentre_F_1_NE.til"), iso, baseContentDir);
            MapCanvas = new MapCanvas(Map, uiDispatcher, baseContentDir + "tiles\\", iso, Factions);
            _useAltEditLayer = false;


            //  Create the available TileSets and collection views
            TileSets = new Dictionary<string, ObservableCollection<ZTile>>();
            TileSetViews = new Dictionary<string, ListCollectionView>();
            foreach (var di in new DirectoryInfo(baseContentDir + "tiles\\").GetDirectories())
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
            var layer = 1;
            if (!_useAltEditLayer)
            {
                layer = 0;

                if (tileToPlace.TileType != TileType.Floor)
                {
                    layer = 3;
                    //  Layers 2 & 4 are generally used for wall filler tiles atm
                    if (tileToPlace.Name.Contains("_ct_") || tileToPlace.Name.Contains("_c_") || tileToPlace.Name.Contains("_d_"))
                        layer = 4;
                    if (tileToPlace.Name.Contains("_b_") || tileToPlace.Name.Contains("b&d") )
                        layer = 2;
                }
                if (tileToPlace.Flags.Contains(TileFlag.Ethereal))
                    layer = 4;
            }
            //  Check we're not swapping for the same thing
            if (Map.Cells[gridPosition.X, gridPosition.Y][layer] == tileToPlace) return;

            //  Update the undo buffer with the old info
            var newNode = new DoubleLinkedListNode<MapDefinition>(Map.Clone(), _currentUndoNode);
            if (_currentUndoNode != null)
                _currentUndoNode.Next = newNode;
            _currentUndoNode = newNode;
            Map.UndoInfo.Clear();
            Map.UndoInfo.UnionWith(Map.LegallyPlaceTile(gridPosition, layer, tileToPlace));
            MapCanvas.AdaptiveTileRefresh(Map.UndoInfo.OrderBy(x => x.Y));
        }

        /// <summary>
        /// Undo functionality.
        /// </summary>
        public void Undo()
        {
            if (_currentUndoNode == null)
                return;
            var temp = Map;
            Map = _currentUndoNode.Value;

            _currentUndoNode = new DoubleLinkedListNode<MapDefinition>(temp, _currentUndoNode.Previous, _currentUndoNode.Next);
            if (_currentUndoNode.Previous != null)
                _currentUndoNode.Previous.Next = _currentUndoNode;

            if (_currentUndoNode.Previous != null)
                _currentUndoNode = _currentUndoNode.Previous;
            MapCanvas.Map = Map;
            MapCanvas.AdaptiveTileRefresh(Map.UndoInfo.Union(temp.UndoInfo).OrderBy(x => x.Y).ThenBy(c => c.X));
        }

        /// <summary>
        /// Redo functionality.
        /// </summary>
        public void Redo()
        {
            if (_currentUndoNode == null || _currentUndoNode.Next == null)
                return;
            _currentUndoNode = _currentUndoNode.Next;
            var temp = Map;
            Map = _currentUndoNode.Value;

            _currentUndoNode = new DoubleLinkedListNode<MapDefinition>(temp, _currentUndoNode.Previous, _currentUndoNode.Next);
            if (_currentUndoNode.Previous != null)
                _currentUndoNode.Previous.Next = _currentUndoNode;
            MapCanvas.Map = Map;
            MapCanvas.AdaptiveTileRefresh(Map.UndoInfo.Union(temp.UndoInfo).OrderBy(x => x.Y).ThenBy(c => c.X));
        }

        /// <summary>
        /// Recursively loads all the tile images from the given directory.
        /// </summary>
        /// <param name="state"></param>
        private void GetWholeTileSet(Object state)
        {
            var di = new DirectoryInfo(state.ToString());
            if (TileSets.ContainsKey(di.Name))
                _tempTileSet = di.Name;

            var tempList = (from fi in di.GetFiles() where fi.Extension.Equals(".til") select new ZTile(fi.FullName)).ToList();

            if (tempList.Count > 0)
                AddTilesToCollection(tempList);

            foreach (var di2 in di.GetDirectories())
                GetWholeTileSet(di2.FullName);
        }

        /// <summary>
        /// Allows adding a list to the ObservableCollection from any thread.
        /// </summary>
        /// <param name="newItems"></param>
        private void AddTilesToCollection(List<ZTile> newItems)
        {
            if (_uiDispatcher.CheckAccess())
                newItems.ForEach(x => TileSets[_tempTileSet].Add(x));
            else
                _uiDispatcher.BeginInvoke(new Action<List<ZTile>>(AddTilesToCollection), newItems);
        }

        /// <summary>
        /// Starts a new module.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal void NewModule(int width, int height)
        {
            _currentUndoNode = null;
            Map.ClearAllCells(width, height);
            foreach( FactionList fl in Factions)
                fl.Units.Clear();
            
            ThreadPool.QueueUserWorkItem(MapCanvas.RenderMap, null);
        }

        /// <summary>
        /// Opens module from file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="iso"></param>
        internal void OpenModule(string fileName, Isometry iso)
        {
            _currentUndoNode = null;
            var tempMod = Module.OpenModule(fileName, iso, false);
            Map = tempMod.Map;
            MapCanvas.Map = tempMod.Map;
            foreach (FactionList fl in Factions)
                fl.Units.Clear();
            foreach (Unit u in tempMod.Roster)
                Factions[u.OwnerID].Units.Add(u);
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
            _useAltEditLayer = !_useAltEditLayer;
        }
    }
}
