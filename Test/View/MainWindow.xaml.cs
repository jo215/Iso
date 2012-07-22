using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ZarTools;
using ISOTools;
using System.Collections;
using Microsoft.Win32;
using System.Threading;
using Editor.Model;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string BaseContentDirectory = "D:\\workspace\\BaseGame\\";
        private readonly CharacterEditor _characterEditor;

        ViewModel ViewModel { get; set; }

        readonly Isometry _iso;

        Predicate<object> _filterFX;

        readonly Random _random;

        Point _mouseDragStartPoint, _scrollStartOffset;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            //  Window initialisation
            SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
            
            //  Isometry helper
            _iso = new Isometry(IsometricStyle.Staggered, BaseContentDirectory + "tiles\\mousemap.png");

            //  Start the ViewModel
            ViewModel = new ViewModel(Dispatcher, BaseContentDirectory, _iso);
            
            //  Text filter function for tile picker
            _filterFX = (p) => p.ToString().Contains(tileFilterTextBox.Text);

            InitializeComponent();

            //  Pass an ImageControl reference to the MapCanvas (violating MVVM)
            ViewModel.MapCanvas.ImageControl = mapCanvasImage;

            cellOverlayImage.IsHitTestVisible = false;

            //  Tabs for each tileset
            foreach (var s in ViewModel.TileSets.Keys)
            {
                var tab = new TabItem {Header = s};
                //  Listview of filtered tiles in this tileset
                var lv = new ListView();
                //  Style & template from XAML
                tab.Style = (Style)FindResource("TilePickerTabItemStyle");
                lv.Style = (Style)FindResource("TilePickerListViewStyle");
                lv.ItemTemplate = (DataTemplate)FindResource("TilePickerListData");
                //  Use a WrapPanel for content
                lv.ItemsPanel =
                    new ItemsPanelTemplate(
                        new FrameworkElementFactory(
                            typeof(WrapPanel)));
                //  Set the ListView content & filter function
                lv.ItemsSource = ViewModel.TileSetViews[s];
                ViewModel.TileSetViews[s].Filter = _filterFX;
                //  Action
                lv.SelectionChanged += TilePickerSelectionChanged;
                tab.Content = lv;
                //  Add the tab to the TabControl
                tilePickerTabControl.Items.Add(tab);
            }
            _random = new Random();

            _characterEditor = new CharacterEditor(ViewModel);
            _characterEditor.Show();
            _characterEditor.Topmost = true;
        }

        /// <summary>
        /// Called when the window is closed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            //  Get rid of character editor window
            _characterEditor.Visibility = Visibility.Collapsed;
            _characterEditor.Close();
        }

        /// <summary>
        /// Called when a new tile is Picked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TilePickerSelectionChanged(object sender, RoutedEventArgs e)
        {
            //  Get the picked tile
            var zt = ((ZTile)((ListView)sender).SelectedItem);
            if (zt == null)
            {
                cellOverlayImage.Source = null;
                return;
            }
            cellOverlayImage.Source = zt.Image;
            //  Set the image & tile info text
            selectedTileImage.Source = zt.Image;
            selectedTileLabel.Content = zt.Name;
            tileDimensionsLabel.Content = zt.Width + " x " + zt.Height + " px";
            tileBoundingBoxLabel.Content = "B. Box: " + zt.BoundingBox[0] + "," + zt.BoundingBox[1] + "," + zt.BoundingBox[2];

            //  Show associated material / type / flags in listbox
            tileFlagsListBox.Items.Clear();
            tileFlagsListBox.Items.Add(Enum.GetName(typeof(TileMaterial), zt.Material));
            tileFlagsListBox.Items.Add(Enum.GetName(typeof(TileType), zt.TileType));
            foreach (var flag in zt.Flags)
                tileFlagsListBox.Items.Add(Enum.GetName(typeof(TileFlag), flag));
            tileFlagsListBox.Items.Refresh();
        }

        /// <summary>
        /// Called when keys pressed over tile picker filter box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TileFilterTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ChangeFilter();
        }

        /// <summary>
        /// Called when the 'Clear Filter' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearFilterButtonClick(object sender, RoutedEventArgs e)
        {
            tileFilterTextBox.Text = "";
            ChangeFilter();
        }

        /// <summary>
        /// Re-runs the tile picker filter on the current tab.
        /// </summary>
        private void ChangeFilter()
        {
            var tab = tilePickerTabControl.SelectedItem as TabItem;
            var lcv = ViewModel.TileSetViews[tab.Header.ToString()];
            //  Re-run the filter on the current tab
            lcv.Filter = lcv.Count == 0 ? null : _filterFX;
        }

        /// <summary>
        /// Called when a new tab is selected on the tile picker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TilePickerTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeFilter();
        }

        /// <summary>
        /// Called to change the filter settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoFilterButtonClick(object sender, RoutedEventArgs e)
        {
            ChangeFilter();
        }

        /// <summary>
        /// Map-drag functionality - Mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //  Only startdragging if over the scrollviewer content
            if (!((Image) mapScrollViewer.Content).IsMouseOver) return;
            _mouseDragStartPoint = e.GetPosition(mapScrollViewer);
            _scrollStartOffset.X = mapScrollViewer.HorizontalOffset;
            _scrollStartOffset.Y = mapScrollViewer.VerticalOffset;

            // Update the cursor if scrolling is possible 
            mapScrollViewer.Cursor = (mapScrollViewer.ExtentWidth > mapScrollViewer.ViewportWidth) ||
                                     (mapScrollViewer.ExtentHeight > mapScrollViewer.ViewportHeight) ?
                                     Cursors.ScrollAll : Cursors.Arrow;

            mapScrollViewer.CaptureMouse();
            base.OnPreviewMouseDown(e);
        }

        /// <summary>
        /// Map-drag functionality - Mouse move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mapScrollViewer.IsMouseCaptured)
            {
                // Get the new mouse position. 
                var mousePos = e.GetPosition(mapScrollViewer);

                // Determine the new amount to scroll. 
                var delta = new Point(
                    (mousePos.X > _mouseDragStartPoint.X) ?
                    -(mousePos.X - _mouseDragStartPoint.X) :
                    (_mouseDragStartPoint.X - mousePos.X),
                    (mousePos.Y > _mouseDragStartPoint.Y) ?
                    -(mousePos.Y - _mouseDragStartPoint.Y) :
                    (_mouseDragStartPoint.Y - mousePos.Y));

                // Scroll to the new position. 
                mapScrollViewer.ScrollToHorizontalOffset(_scrollStartOffset.X + delta.X);
                mapScrollViewer.ScrollToVerticalOffset(_scrollStartOffset.Y + delta.Y);
            }
            base.OnPreviewMouseMove(e);
        }

        /// <summary>
        /// Map drag functionality - Mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (mapScrollViewer.IsMouseCaptured)
            {
                mapScrollViewer.Cursor = Cursors.Arrow;
                mapScrollViewer.ReleaseMouseCapture();
            }
            base.OnPreviewMouseUp(e);
            if (e.ChangedButton == MouseButton.Right) return;
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }

        /// <summary>
        /// Called when the main map grid is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            //  Paint a new tile if over map canvas
            if (!((Image) mapScrollViewer.Content).IsMouseOver) return;
            //  If multiple tiles selected, choose one at random; return if no tiles selected
            var selectedTiles = ((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItems;
            if (selectedTiles.Count == 0)
                return;
            var tileToPlace = (ZTile)selectedTiles[_random.Next(0, selectedTiles.Count)];
            //  Get the grid location, return if off-grid
            ViewModel.PaintCell(_iso.MouseMapper(e.GetPosition(mapCanvasImage)), tileToPlace);
        }

        /// <summary>
        /// Called when the mouse moved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            //  Check if over map
            if (((Image)mapScrollViewer.Content).IsMouseOver)
            {
                //  Get the grid location of mouse
                System.Drawing.Point mouseAt = _iso.MouseMapper(e.GetPosition(mapCanvasImage));
                if (ViewModel.Map.IsOnGrid(mouseAt))
                {
                    //  Get the position to draw the overlay at
                    mouseAt = _iso.TilePlotter(mouseAt);
                    cellOverlayImage.Visibility = Visibility.Visible;
                    //  Get the tile to overlay
                    var zt = (ZTile)((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItem;
                    //  Correct for height/width of tile
                    int x = 0, y = 0;
                    if (zt != null)
                    {
                        if (zt.Height > ViewModel.Map.TileHeight)
                            y -= (zt.Height - ViewModel.Map.TileHeight);
                        x += (6 - zt.BoundingBox[2]) * 6;

                        if (zt.Name.Contains("_b_") || zt.Name.Contains("b&d"))
                        {
                            if (zt.Name.Contains("se") || zt.Name.Contains("nw"))
                            {

                            }
                            else
                            {
                                x -= 12;
                                y -= 6;
                            }
                        }
                    }
                    Canvas.SetLeft(cellOverlayImage, mouseAt.X - mapScrollViewer.HorizontalOffset + x);
                    Canvas.SetTop(cellOverlayImage, mouseAt.Y - mapScrollViewer.VerticalOffset + y);
                    //  We also call draw tiles if mouse pressed
                    if (e.LeftButton == MouseButtonState.Pressed)
                        OnMouseLeftButtonDown(this, e);
                    return;
                }
            }
            //  Hide the overlay
            cellOverlayImage.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Called when a key is released over the map scroll viewer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapScrollViewerKeyUp(object sender, KeyEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
            base.OnKeyUp(e);
        }

        #region Commands

        //  New Map
        private void NewMap(object sender, ExecutedRoutedEventArgs e)
        {
            var result = 
                MessageBox.Show("Do you want to save your changes?", "New Map",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.Cancel:
                    return;
                case MessageBoxResult.None:
                    return;
                case MessageBoxResult.Yes:
                    SaveMapAs(null, null);
                    break;
            }
            var dlg = new NewMapSettingsDialog {Owner = this};

            if (dlg.ShowDialog() == true)
                ViewModel.NewMap(int.Parse(dlg.widthBox.Text), int.Parse(dlg.heightBox.Text)); 
        }

        //  Save map as
        private void SaveMapAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new SaveFileDialog {DefaultExt = ".jim", Filter = "Isometric map documents (.jim)|*.jim"};
            var result = dlg.ShowDialog();
            if (result == true)
                ViewModel.Map.SaveMap(dlg.FileName);
        }

        //  Open map
        private void OpenMap(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {DefaultExt = ".jim", Filter = "Isometric map documents (.jim)|*.jim"};
            var result = dlg.ShowDialog();
            if (result == true)
                ViewModel.OpenMap(dlg.FileName, _iso);
        }

        //  Undo
        private void Undo(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Undo();
        }

        //  Redo
        private void Redo(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Redo();
        }

        //  Select nothing
        private void SelectNothing(object sender, ExecutedRoutedEventArgs e)
        {
            ((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItems.Clear();
        }

        //  Show grid
        private void ShowGrid(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.ShowGrid();
            
        }
        //  Refresh map
        private void Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }

        //  Change edit layer
        private void ChangeLayer(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.SwitchEditLayer();
        }

        //  Edit characters
        private void EditCharacters(object sender, ExecutedRoutedEventArgs e)
        {
            _characterEditor.Visibility = _characterEditor.IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        //  Drag & Drop
        private void mapCanvasImage_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
               
            }
        }

        private void mapCanvasImage_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
                //  Get the grid location of mouse
                System.Drawing.Point mouseAt = _iso.MouseMapper(e.GetPosition(mapCanvasImage));
                if (ViewModel.Map.IsOnGrid(mouseAt))
                {
                    //  Get the position to draw the overlay at
                    mouseAt = _iso.TilePlotter(mouseAt);
                    Unit u = e.Data.GetData("myFormat") as Unit;
                    cellOverlayImage.Source = u.Image;
                    cellOverlayImage.Visibility = Visibility.Visible;
                    Canvas.SetLeft(cellOverlayImage, (mouseAt.X - mapScrollViewer.HorizontalOffset) + (u.Image.PixelWidth/2) );
                    Canvas.SetTop(cellOverlayImage, (mouseAt.Y - mapScrollViewer.VerticalOffset) - u.Image.PixelHeight  + ViewModel.Map.TileHeight);
                }
            }
        }

        private void mapCanvasImage_DragLeave(object sender, DragEventArgs e)
        {
            cellOverlayImage.Source = null;
            cellOverlayImage.Visibility = Visibility.Hidden;
        }

        //  Drag & Drop
        private void mapCanvasImage_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("myFormat"))
            {
                Unit u = e.Data.GetData("myFormat") as Unit;
                System.Drawing.Point old = new System.Drawing.Point(u.X, u.Y);
                //  Get the grid location of mouse
                System.Drawing.Point mouseAt = _iso.MouseMapper(e.GetPosition(mapCanvasImage));
                u.X = (short) mouseAt.X;
                u.Y = (short) mouseAt.Y;
                
                ViewModel.MapCanvas.AdaptiveTileRefresh(mouseAt);
                ViewModel.MapCanvas.AdaptiveTileRefresh(old);
                ViewModel.MapCanvas.AdaptiveTileRefresh(_iso.TileWalker(old, CompassDirection.North));
                ViewModel.MapCanvas.AdaptiveTileRefresh(_iso.TileWalker(old, CompassDirection.North, 2));
                ViewModel.MapCanvas.AdaptiveTileRefresh(_iso.TileWalker(old, CompassDirection.NorthEast));

                cellOverlayImage.Source = null;
                cellOverlayImage.Visibility = Visibility.Hidden;
            }
        }

        #endregion








    }

    /// <summary>
    /// Command declarations.
    /// </summary>
    public static class Command
    {
        public static readonly RoutedUICommand SelectNothing = new RoutedUICommand("Select none", "SelectNothing", typeof(MainWindow));
        public static readonly RoutedUICommand ShowGrid = new RoutedUICommand("Show grid", "ShowGrid", typeof(MainWindow));
        public static readonly RoutedUICommand ChangeLayer = new RoutedUICommand("Change edit layer", "ChangeLayer", typeof(MainWindow));
        public static readonly RoutedUICommand EditCharacters = new RoutedUICommand("Edit characters", "EditCharacters", typeof(MainWindow));
    }
}