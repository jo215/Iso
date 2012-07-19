using System;
using System.Collections.Generic;
using System.Threading;
using IsoGame.State;
using Lidgren.Network;
using Microsoft.Xna.Framework.Content;

namespace IsoGame.Network
{
    public class Server
    {

        internal string Tag;

        public volatile bool IsRunning;

        internal IServerState CurrentServerState;
        
        internal GameState GameState;

        internal NetworkAgent Agent;

        internal bool SomebodyOnceConnected;
        internal Dictionary<NetConnection, PlayerState> IPDictionary;
        internal ContentManager Content;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tag"></param>
        public Server(string tag, ContentManager content)
        {
            Tag = tag;
            Content = content;
            Initialise();
            var t = new Thread(Run);
            t.Start();
        }

        /// <summary>
        /// Runs the thread.
        /// </summary>
        private void Run()
        {
            
            while (IsRunning)
            {
                try
                {
                    CurrentServerState.Execute();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                if (GameState.Players.Count == 0 && SomebodyOnceConnected)
                    IsRunning = false;
            }

        }

        /// <summary>
        /// Initialises the socket.
        /// </summary>
        private void Initialise()
        {
            try
            {
                Agent = new NetworkAgent(AgentRole.Server, Tag);     
                GameState = new GameState(Content.RootDirectory + "\\Textures\\mousemap.png");
                CurrentServerState = new LobbyServerState(this);
                IPDictionary = new Dictionary<NetConnection, PlayerState>();
                SomebodyOnceConnected = false;
                IsRunning = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
