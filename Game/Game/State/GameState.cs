using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IsoTools;

namespace IsoGame.State
{
    public class GameState
    {
        public List<PlayerState> Players { get; private set; }
        public int ActivePlayer { get; set; }
        public Module Module { get; set; }
        public Isometry iso;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameState(string mouseMapPath)
        {
            Players = new List<PlayerState>();
            iso = new Isometry(IsometricStyle.Staggered, mouseMapPath);
        }

        public PlayerState GetPlayerByID(byte id)
        {
            return Players.FirstOrDefault(p => p.SessionID == id);
        }
    }
}
