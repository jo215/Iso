using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editor.Model;
using System.Windows.Media;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewModel"></param>
        public CharacterEditor(ViewModel viewModel)
        {
           
            DataContext = this;
            Test = "dhsfuifbvosfbo";
            WindowStyle = WindowStyle.ToolWindow;
            ViewModel = viewModel;
            //  Faction unit lists
            Factions = ViewModel.Factions;
            Factions[1].Units.Add(new Unit(1, BodyType.Omega, WeaponType.SMG, Stance.Stand, 4, 4, 4, -1, -1, "James"));
            Factions[2].Units.Add(new Unit(2, BodyType.TribalFemale, WeaponType.Club, Stance.Stand, 4, 4, 4, -1, -1, "John"));
            SelectedUnit = Factions[1].Units[0];
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
            faction.Units.Add(new Unit(1, BodyType.TribalMale, WeaponType.None, Stance.Stand, 1, 1, 1, -1, -1, "Unnamed"));
        }

        //  Deletes the selected unit
        private void DeleteUnit(object sender, ExecutedRoutedEventArgs e)
        {
            var unit = (Unit)(VisualUpwardSearch((DependencyObject)e.OriginalSource)).DataContext;
            foreach (var f in Factions)
            {
                f.Units.Remove(unit);
            }
        }

        //  Deletes all units belonging to the selected player
        private void DeleteAllUnits(object sender, ExecutedRoutedEventArgs e)
        {
            var faction = GetFactionByName((string)e.Parameter);
            faction.Units.Clear();
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
            image1.Source = SelectedUnit.Image;
            TextBlockMouseDown(sender, e);
        }

        //  Change selected unit image
        private void UnitPictureChanged(object sender, SelectionChangedEventArgs e)
        {
            image1.Source = SelectedUnit.Image;
            ViewModel.MapCanvas.RenderMap(null);
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
        private void TextBlockMouseDown(object sender, MouseButtonEventArgs e)
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

        private void PosValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.MapCanvas.RenderMap(null);
        }


    }

    /// <summary>
    /// Command declarations.
    /// </summary>
    public static class UnitCommand
    {
        public static readonly RoutedUICommand NewUnit = new RoutedUICommand("New unit", "NewUnit", typeof(CharacterEditor));
        public static readonly RoutedUICommand DeleteAllUnits = new RoutedUICommand("Delete all units", "DeleteAllUnits", typeof(CharacterEditor));
        public static readonly RoutedUICommand DeleteUnit = new RoutedUICommand("Delete unit", "DeleteUnit", typeof(CharacterEditor));

    }
}
