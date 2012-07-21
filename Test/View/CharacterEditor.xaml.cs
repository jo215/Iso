using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editor.Model;
using Xceed.Wpf.Toolkit;
namespace Editor.View
{
    /// <summary>
    /// Interaction logic for CharacterEditor.xaml
    /// </summary>
    public partial class CharacterEditor 
    {
        public ViewModel ViewModel { get; set; }

        static public ObservableCollection<FactionList> Factions { get; set; }

        public Unit selectedUnit { get; set; }
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
            Factions = new ObservableCollection<FactionList> { new FactionList("Player 1"), new FactionList("Player 2") };
            Factions[0].Units.Add(new Unit(0, BodyType.Omega, WeaponType.SMG, Stance.Stand, 4, 4, 4, 3, 3, "Donny the Bull"));
            selectedUnit = Factions[0].Units[0];
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
            faction.Units.Add(new Unit(1, BodyType.Omega, WeaponType.SMG, Stance.Stand, 4, 4, 4, 3, 3, "Donny the Bull"));
        }

        private FactionList GetFactionByName(string name)
        {
            return Factions.FirstOrDefault(fl => fl.Name.Equals(name));
        }

        #endregion

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedUnit = (Unit) ((TextBlock) e.Source).DataContext;
            selUnit.DataContext = selectedUnit;
        }

    }

    /// <summary>
    /// Command declarations.
    /// </summary>
    public static class UnitCommand
    {
        public static readonly RoutedUICommand NewUnit = new RoutedUICommand("New unit", "NewUnit", typeof(CharacterEditor));
    }
}
