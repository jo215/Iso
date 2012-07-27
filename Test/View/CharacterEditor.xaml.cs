using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using ZarTools;
using IsoTools;

namespace Editor.View
{
    /// <summary>
    /// Interaction logic for CharacterEditor.xaml
    /// </summary>
    public partial class CharacterEditor 
    {
        public ViewModel ViewModel { get; set; }

        static public ObservableCollection<FactionList> Factions { get; set; }

        public Unit SelectedUnit { get; set; }
        public string Test { get; set; }
        private Dispatcher _uiDispatcher;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewModel"></param>
        public CharacterEditor(ViewModel viewModel, Dispatcher uiDispatcher, List<Unit> newRoster = null)
        {
            _uiDispatcher = uiDispatcher;
            DataContext = this;
            Test = "dhsfuifbvosfbo";
            WindowStyle = WindowStyle.ToolWindow;
            ViewModel = viewModel;
            //  Faction unit lists
            Factions = ViewModel.Factions;
            if (newRoster == null)
            {
                Factions[1].Units.Add(new Unit(1, BodyType.Enclave, WeaponType.SMG, Stance.Stand, 10, 10, 10, 10, 10, -1, -1, "James"));
                Factions[2].Units.Add(new Unit(2, BodyType.TribalFemale, WeaponType.Club, Stance.Stand, 10, 10, 10, 10, 10, -1, -1, "John"));
            }
            else
            {
                foreach (Unit u in newRoster)
                {
                    Factions[u.OwnerID].Units.Add(u);
                }
            }
            InitializeComponent();
            body.ItemsSource = Enum.GetValues(typeof (BodyType));
            weapon.ItemsSource = Enum.GetValues(typeof (WeaponType));
            
        }

        /// <summary>
        /// Hide (do not close) if the X button is clicked.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (Visibility != Visibility.Visible) return;
            Visibility = Visibility.Hidden;
            e.Cancel = true;
        }

        #region Commands

        //  Adds a new unit to the selected player's list
        private void NewUnit(object sender, ExecutedRoutedEventArgs e)
        {
            var faction = GetFactionByName((string) e.Parameter);
            var facId = faction.ID;
            faction.Units.Add(new Unit(facId, BodyType.TribalMale, WeaponType.None, Stance.Stand, 1, 1, 1, 1, 1, -1, -1, "Unnamed"));
        }

        //  Deletes the selected unit
        private void DeleteUnit(object sender, ExecutedRoutedEventArgs e)
        {
            var unit = (Unit)(VisualUpwardSearch((DependencyObject)e.OriginalSource)).DataContext;
            foreach (var f in Factions)
            {
                f.Units.Remove(unit);
            }
            if (SelectedUnit == unit)
            {
                SelectedUnit = null;
                selUnit.DataContext = SelectedUnit;
                image1.DataContext = SelectedUnit;
            }
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }

        //  Deletes all units belonging to the selected player
        private void DeleteAllUnits(object sender, ExecutedRoutedEventArgs e)
        {
            var faction = GetFactionByName((string)e.Parameter);
            faction.Units.Clear();
            SelectedUnit = null;
            selUnit.DataContext = SelectedUnit;
            image1.DataContext = SelectedUnit;
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }

        //  Gets a faction object by name
        private FactionList GetFactionByName(string name)
        {
            return Factions.FirstOrDefault(fl => fl.Name.Equals(name));
        }

        #endregion

        #region events

        //  Change selected unit
        private void UnitTextBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedUnit = (Unit) ((TextBlock) e.Source).DataContext;
            selUnit.DataContext = SelectedUnit;
            image1.DataContext = SelectedUnit;
            image1.Source = SelectedUnit.Image;
            TreeviewTextMouseDown(sender, e);
        }

        //  Change selected unit image
        private void ChangeUnitPicture(object sender, SelectionChangedEventArgs e)
        {
            image1.Source = SelectedUnit.Image;

            if (SelectedUnit.X >= 0 || SelectedUnit.Y >= 0)
                ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }
        
        //  Drag & Drop 
        private void image1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the mouse position
            startPoint = e.GetPosition(null);
        }

        //  Drag & Drop
        private void image1_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // Get the dragged ListViewItem
                var image = sender as Image;
                var unit = (Unit)image.DataContext;

                // Initialize the drag & drop operation
                DataObject dragData = new DataObject("myFormat", unit);
                DragDrop.DoDragDrop(image, dragData, DragDropEffects.Move);
            } 
        }

        #endregion

        /// <summary>
        /// Selects a treeview item before showing the context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeviewTextMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                //e.Handled = true;
            }
        }

        /// <summary>
        /// Finds a treeview parent item of a given dependency object.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        public Point startPoint { get; set; }

        private void CharacterPositionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ThreadPool.QueueUserWorkItem(ViewModel.MapCanvas.RenderMap, null);
        }

        internal void AddUnits(System.Collections.Generic.List<Unit> units)
        {
            Unit.Bitmaps = null;
            Unit.Images = null;
            foreach (Unit u in units)
            {
                Factions[u.OwnerID].Units.Add(u);
            }
            SelectedUnit = null;
            selUnit.DataContext = SelectedUnit;
            image1.DataContext = SelectedUnit;
        }
    }


}
