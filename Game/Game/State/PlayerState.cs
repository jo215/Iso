using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;

namespace IsoGame.State
{
    public class PlayerState
    {
        public string Name { get; set; }
        public byte SessionID { get; private set; }
        public NetConnection NetConnection { get; set; }
        
        public static PlayerState Server = new PlayerState("SERVER", 0);
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sessionID"></param>
        /// <param name="ip"></param>
        public PlayerState(string name, byte sessionID, NetConnection ip)
        {
            Name = name;
            SessionID = sessionID;
            NetConnection = ip;
        }
        public PlayerState(string name, byte sessionID)
        {
            Name = name;
            SessionID = sessionID;
        }
        public PlayerState(byte sessionID, string name)
        {
            Name = name;
            SessionID = sessionID;
        }

        public string ColorText()
        {
            switch (SessionID)
            { 
                case 0:
                    return "white";
                case 1:
                    return "lightblue";
                case 2:
                    return "red";
                case 3:
                    return "green";
                case 4:
                    return "yellow";
                case 5:
                    return "cyan";
                case 6:
                    return "magenta";
                case 7:
                    return "orange";
                case 8:
                    return "purple";
            }
            return "white";
        }

        public Color ColorValue()
        {
            switch (SessionID)
            {
                case 0:
                    return Color.White;
                case 1:
                    return Color.LightBlue;
                case 2:
                    return Color.Red;
                case 3:
                    return Color.Green;
                case 4:
                    return Color.Yellow;
                case 5:
                    return Color.Cyan;
                case 6:
                    return Color.Magenta;
                case 7:
                    return Color.Orange;
                case 8:
                    return Color.Purple;
            }
            return Color.White;
        }
    }
}
