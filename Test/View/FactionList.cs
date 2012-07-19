﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Editor.Model;

namespace Editor.View
{
    public class FactionList
    {
        public string Name { get; set; }

        public ObservableCollection<Unit> Units { get; private set; }

        public FactionList()
        {
            Units = new ObservableCollection<Unit>();
        }

        public FactionList(string name, ObservableCollection<Unit> units)
        {
            Name = name;
            Units = units;
        }
    }
}
