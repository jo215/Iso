using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ZarTools;
using ISOTools;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections;
using Microsoft.Win32;
using System.Threading;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string baseContentDirectory = "D:\\workspace\\BaseGame\\";
        private CharacterEditor _characterEditor;

        ViewModel viewModel { get; set; }

        Isometry iso;

        Predicate<object> filterFX;

        Random random;

        Point mouseDragStartPoint, scrollStartOffset;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            //  Window initialisation
            SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
            
            //  Isometry helper
            iso = new Isometry(IsometricStyle.Staggered, baseContentDirectory + "tiles\\mousemap.png");

            //  Start the ViewModel
            viewModel = new ViewModel(this.Dispatcher, baseContentDirectory, iso);
            
            //  Text filter function for tile picker
            this.filterFX = (p) => p.ToString().Contains(tileFilterTextBox.Text);

            InitializeComponent();

            //  Pass an ImageControl reference to the MapCanvas (violating MVVM)
            viewModel.MapCanvas.ImageControl = mapCanvasImage;

            cellOverlayImage.IsHitTestVisible = false;

            //  Tabs for each tileset
            foreach (string s in viewModel.TileSets.Keys)
            {
                TabItem tab = new TabItem();
                tab.Header = s;
                //  Listview of filtered tiles in this tileset
                ListView lv = new ListView();
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
                lv.ItemsSource = viewModel.TileSetViews[s];
                viewModel.TileSetViews[s].Filter = this.filterFX;
                //  Action
                lv.SelectionChanged += tilePicker_SelectionChanged;
                tab.Content = lv;
                //  Add the tab to the TabControl
                tilePickerTabControl.Items.Add(tab);
            }
            random = new Random();

            _characterEditor = new CharacterEditor(viewModel);
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
        private void tilePicker_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //  Get the picked tile
            ZTile zt = ((ZTile)((ListView)sender).SelectedItem);
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
            foreach (TileFlag flag in zt.Flags)
                tileFlagsListBox.Items.Add(Enum.GetName(typeof(TileFlag), flag));
            tileFlagsListBox.Items.Refresh();
        }

        /// <summary>
        /// Called when keys pressed over tile picker filter box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tileFilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                changeFilter();
        }

        /// <summary>
        /// Called when the 'Clear Filter' button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            tileFilterTextBox.Text = "";
            changeFilter();
        }

        /// <summary>
        /// Re-runs the tile picker filter on the current tab.
        /// </summary>
        private void changeFilter()
        {
            TabItem tab = tilePickerTabControl.SelectedItem as TabItem;
            ListCollectionView lcv = viewModel.TileSetViews[tab.Header.ToString()];
            //  Re-run the filter on the current tab
            if (lcv.Count == 0)
                lcv.Filter = null;
            else
                lcv.Filter = filterFX;
        }

        /// <summary>
        /// Called when a new tab is selected on the tile picker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tilePickerTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            changeFilter();
        }

        private void doFilterButton_Click(object sender, RoutedEventArgs e)
        {
            changeFilter();
        }

        /// <summary>
        /// Map-drag functionality - Mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //  Only startdragging if over the scrollviewer content
            if (((Image)mapScrollViewer.Content).IsMouseOver == true)
            {
                mouseDragStartPoint = e.GetPosition(mapScrollViewer);
                scrollStartOffset.X = mapScrollViewer.HorizontalOffset;
                scrollStartOffset.Y = mapScrollViewer.VerticalOffset;

                // Update the cursor if scrolling is possible 
                mapScrollViewer.Cursor = (mapScrollViewer.ExtentWidth > mapScrollViewer.ViewportWidth) ||
                    (mapScrollViewer.ExtentHeight > mapScrollViewer.ViewportHeight) ?
                    Cursors.ScrollAll : Cursors.Arrow;

                mapScrollViewer.CaptureMouse();
                base.OnPreviewMouseDown(e);
            }
        }

        /// <summary>
        /// Map-drag functionality - Mouse move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mapScrollViewer.IsMouseCaptured)
            {
                // Get the new mouse position. 
                Point mousePos = e.GetPosition(mapScrollViewer);

                // Determine the new amount to scroll. 
                Point delta = new Point(
                    (mousePos.X > this.mouseDragStartPoint.X) ?
                    -(mousePos.X - this.mouseDragStartPoint.X) :
                    (this.mouseDragStartPoint.X - mousePos.X),
                    (mousePos.Y > this.mouseDragStartPoint.Y) ?
                    -(mousePos.Y - this.mouseDragStartPoint.Y) :
                    (this.mouseDragStartPoint.Y - mousePos.Y));

                // Scroll to the new position. 
                mapScrollViewer.ScrollToHorizontalOffset(this.scrollStartOffset.X + delta.X);
                mapScrollViewer.ScrollToVerticalOffset(this.scrollStartOffset.Y + delta.Y);
            }
            base.OnPreviewMouseMove(e);
        }

        /// <summary>
        /// Map drag functionality - Mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(viewModel.MapCanvas.RenderMap, null);
            if (mapScrollViewer.IsMouseCaptured)
            {
                mapScrollViewer.Cursor = Cursors.Arrow;
                mapScrollViewer.ReleaseMouseCapture();
            }
            base.OnPreviewMouseUp(e);
        }

        /// <summary>
        /// Called when the main map grid is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            //  Paint a new tile if over map canvas
            if (((Image)mapScrollViewer.Content).IsMouseOver == true)
            {
                //  If multiple tiles selected, choose one at random; return if no tiles selected
                IList selectedTiles = ((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItems;
                if (selectedTiles.Count == 0)
                    return;
                ZTile tileToPlace = (ZTile)selectedTiles[random.Next(0, selectedTiles.Count)];
                //  Get the grid location, return if off-grid
                viewModel.PaintCell(iso.MouseMapper(e.GetPosition(mapCanvasImage)), tileToPlace);
            }
        }

        /// <summary>
        /// Called when the mouse moved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            //  Chek if over map
            if (((Image)mapScrollViewer.Content).IsMouseOver == true)
            {
                //  Get the grid location of mouse
                System.Drawing.Point mouseAt = iso.MouseMapper(e.GetPosition(mapCanvasImage));
                if (viewModel.Map.IsOnGrid(mouseAt))
                {
                    //  Get the position to draw the overlay at
                    mouseAt = iso.TilePlotter(mouseAt);
                    cellOverlayImage.Visibility = Visibility.Visible;
                    //  Get the tile to overlay
                    ZTile zt = (ZTile)((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItem;
                    //  Correct for height/width of tile
                    int x = 0, y = 0;
                    if (zt != null)
                    {
                        if (zt.Height > viewModel.Map.TileHeight)
                            y -= (zt.Height - viewModel.Map.TileHeight);
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
                        onMouseLeftButtonDown(this, e);
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
        private void mapScrollViewer_KeyUp(object sender, KeyEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(viewModel.MapCanvas.RenderMap, null);
            base.OnKeyUp(e);
        }

        #region Commands

        //  New Map
        private void NewMap(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBoxResult result = 
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
            NewMapSettingsDialog dlg = new NewMapSettingsDialog();
            dlg.Owner = this;
            
            if (dlg.ShowDialog() == true)
                viewModel.NewMap(int.Parse(dlg.widthBox.Text), int.Parse(dlg.heightBox.Text)); 
        }

        //  Save map as
        private void SaveMapAs(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".jim";
            dlg.Filter = "Isometric map documents (.jim)|*.jim";
            bool? result = dlg.ShowDialog();
            if (result == true)
                viewModel.Map.SaveMap(dlg.FileName);
        }

        //  Open map
        private void OpenMap(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".jim";
            dlg.Filter = "Isometric map documents (.jim)|*.jim";
            bool? result = dlg.ShowDialog();
            if (result == true)
                viewModel.OpenMap(dlg.FileName, iso);
        }

        //  Undo
        private void Undo(object sender, ExecutedRoutedEventArgs e)
        {
            viewModel.Undo();
        }

        //  Redo
        private void Redo(object sender, ExecutedRoutedEventArgs e)
        {
            viewModel.Redo();
        }

        //  Select nothing
        private void SelectNothing(object sender, ExecutedRoutedEventArgs e)
        {
            ((ListView)((TabItem)tilePickerTabControl.SelectedItem).Content).SelectedItems.Clear();
        }

        //  Show grid
        private void ShowGrid(object sender, ExecutedRoutedEventArgs e)
        {
            viewModel.ShowGrid();
            
        }
        //  Refresh map
        private void Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(viewModel.MapCanvas.RenderMap, null);
        }

        //  Change edit layer
        private void ChangeLayer(object sender, ExecutedRoutedEventArgs e)
        {
            viewModel.SwitchEditLayer();
        }

        //  Edit characters
        private void EditCharacters(object sender, ExecutedRoutedEventArgs e)
        {
            _characterEditor.Visibility = _characterEditor.IsVisible ? Visibility.Hidden : Visibility.Visible;
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