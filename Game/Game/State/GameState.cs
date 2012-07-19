using System.Collections.Generic;
using System.Drawing;
using Editor.Model;
using System.Linq;
using ISOTools;

namespace IsoGame.State
{
    public class GameState
    {
        public List<PlayerState> Players { get; private set; }
        public int ActivePlayer { get; set; }
        public List<Unit> Units { get; private set; } 
        public MapDefinition Map { get; set; }
        public Isometry iso;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameState(string mouseMapPath)
        {
            Players = new List<PlayerState>();
            Units = new List<Unit>();
            iso = new Isometry(IsometricStyle.Staggered, mouseMapPath);
        }

        public PlayerState GetPlayerByID(byte id)
        {
            return Players.FirstOrDefault(p => p.SessionID == id);
        }
    }
}
