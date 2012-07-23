using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IsoGame.State;

namespace IsoGame.Network
{
    public class GameServerState : IServerState
    {
        Server _server;
        NetworkAgent _agent;
        GameState _gameState;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="server"></param>
        public GameServerState(Server server)
        {
            _server = server;
            _agent = server.Agent;
            _gameState = server.GameState;
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Execute()
        {

            
        }
    }
}
