using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Editor.Model
{
    public class Roster
    {
        public List<Unit> Units { get; set; }
 
        public Roster()
        {
            Units = new List<Unit>();
        }

        /// <summary>
        /// Indexer method.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Unit this[int i]
        {
            get
            {
                return Units[i];
            }
            set
            {
                Units[i] = value;
            }
        }

        /// <summary>
        /// Returns the total number of units in the MapCell.
        /// </summary>
        public int Length
        {
            get { return Units.Count; }
        }
    }
}
