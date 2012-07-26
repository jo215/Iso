using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace IsoGame.Events
{
    /// <summary>
    /// Keeps a map of key presses to event names.
    /// </summary>
    public class Keymap
    {
        public Dictionary<Keys, EventType> Map;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Keymap()
        {
            Map = new Dictionary<Keys, EventType>();
            AddDefaultKeys();
        }

        /// <summary>
        /// Adds some quick default mappings.
        /// </summary>
        private void AddDefaultKeys()
        {
            Map.Add(Keys.W, EventType.MoveForward);
            Map.Add(Keys.S, EventType.MoveBack);
            Map.Add(Keys.A, EventType.MoveLeft);
            Map.Add(Keys.D, EventType.MoveRight);
        }
    }
}
