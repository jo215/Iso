using System.Collections.ObjectModel;
using Editor.Model;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Editor.View
{
    public class FactionList
    {
        public string Name { get; set; }
        public ObservableCollection<Unit> Units { get;  set; }

        /// <summary>
        /// Constructors.
        /// </summary>
        /// <param name="name"></param>
        public FactionList(string name)
        {
            Name = name;
            Units = new ObservableCollection<Unit>();
        }

        public FactionList(string name, ObservableCollection<Unit> units)
        {
            Name = name;
            Units = units;
        }
    }
}
