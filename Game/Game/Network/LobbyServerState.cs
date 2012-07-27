using IsoGame.Misc;
using IsoGame.State;
using Lidgren.Network;
using System;
using Core;

namespace IsoGame.Network
{
    public class LobbyServerState : IServerState
    {
        readonly Server _server;
        readonly GameState _gameState;
        readonly NetworkAgent _agent;
        internal byte NextPlayerID;
        byte playersInMap;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LobbyServerState(Server server)
        {
            _server = server;
            _gameState = server.GameState;
            _agent = server.Agent;
        }

        /// <summary>
        /// Actions to execute each loop.
        /// </summary>
        public void Execute()
        {

            foreach (var m in _agent.CheckForMessages())
            {
                //  Check for any Logon / Logoff messages
                switch (m.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        {
                            m.Position = 0;
                            var status = (NetConnectionStatus)m.ReadByte();

                            if (status == NetConnectionStatus.Connected)  
                                LogonClient(m.SenderConnection);

                            if (status == NetConnectionStatus.Disconnected || status == NetConnectionStatus.Disconnecting)
                                LogoffClient(m.SenderConnection);
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        switch ((MessageType)m.ReadByte())
                        {
                            case MessageType.Chat:
                                SendChatToAll(m);
                                break;
                            case MessageType.PlayerInfo:
                                UpdatePlayerInfo(m);
                                break;
                            case MessageType.MapUpload:
                                ReceiveAndSendMap(m);
                                break;
                            case MessageType.PlayerEnteredMap:
                                PlayerEnteredMap(m);
                                break;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a player leaves the lobby and enters the map.
        /// </summary>
        /// <param name="m"></param>
        private void PlayerEnteredMap(NetIncomingMessage m)
        {
            var id = m.ReadByte();
            foreach (var player in _gameState.Players)
            {
                _agent.WriteMessage((byte)MessageType.PlayerEnteredMap);
                _agent.WriteMessage(id);
                _agent.SendMessage(player.NetConnection);
            }
            playersInMap++;
            if (playersInMap == _gameState.Players.Count)
                _server.CurrentServerState = new GameServerState(_server);
        }

        /// <summary>
        /// Receives a map from a client and sends it to all clients.
        /// </summary>
        /// <param name="m"></param>
        private void ReceiveAndSendMap(NetIncomingMessage m)
        {
            var id = m.ReadByte();
            var small = m.ReadString();
            var big = StringCompressor.DecompressString(small);
            _gameState.Module = Module.OpenModule(big, _gameState.iso, true);

            //  Send map to all players
            foreach(var player in _gameState.Players)
            {
                _agent.WriteMessage((byte)MessageType.MapUpload);
                _agent.WriteMessage(id);
                _agent.WriteMessage(small);
                _agent.SendMessage(player.NetConnection);
            }
        }

        /// <summary>
        /// Updates player info
        /// </summary>
        /// <param name="m"></param>
        private void UpdatePlayerInfo(NetIncomingMessage m)
        {
            var id = m.ReadByte();
            var name = m.ReadString();
            _gameState.GetPlayerByID(id).Name = name;

            //  Send all players notification
            foreach (var player in _gameState.Players)
            {
                _agent.WriteMessage((byte)MessageType.PlayerInfo);
                _agent.WriteMessage(id);
                _agent.WriteMessage(name);
                _agent.SendMessage(player.NetConnection);
            }
        }

        /// <summary>
        /// Chat with all connected clients.
        /// </summary>
        /// <param name="m"> </param>
        private void SendChatToAll(NetIncomingMessage m)
        {
            var session = m.ReadByte();
            var message = m.ReadString();
            //  Send all players notification
            foreach (var player in _gameState.Players)
            {
                _agent.WriteMessage((byte)MessageType.Chat);
                _agent.WriteMessage(session);
                _agent.WriteMessage(message);
                _agent.SendMessage(player.NetConnection, true);
            }
        }

        /// <summary>
        /// Logs off an existing client.
        /// </summary>
        private void LogoffClient(NetConnection senderConnection)
        {
            //  Send all other players notification
            foreach (var nc in _gameState.Players)
            {
                _agent.WriteMessage((byte)MessageType.PlayerQuit);
                _agent.WriteMessage(nc.SessionID);
                _agent.SendMessage(nc.NetConnection, true);
            }
            //  Remove player from master GameState & IP dictionary
            _gameState.Players.Remove(_server.IPDictionary[senderConnection]);
            _server.IPDictionary.Remove(senderConnection);
        }

        /// <summary>
        /// Attempts to log on a new client.
        /// </summary>
        private void LogonClient(NetConnection senderConnection)
        {
            //  Send new player their ID
            _agent.WriteMessage((byte)MessageType.PlayerID);
            _agent.WriteMessage(++NextPlayerID);
            _agent.SendMessage(senderConnection, true);

            foreach (var nc in _gameState.Players)
            {
                //  Send all other players notification of the new player
                _agent.WriteMessage((byte)MessageType.PlayerJoined);
                _agent.WriteMessage(NextPlayerID);
                _agent.SendMessage(nc.NetConnection, true);
                //  Send the new player details of all other players
                _agent.WriteMessage((byte)MessageType.PlayerJoined);
                _agent.WriteMessage(nc.SessionID);
                _agent.SendMessage(senderConnection, true);
                //  And their name information
                _agent.WriteMessage((byte)MessageType.PlayerInfo);
                _agent.WriteMessage(nc.SessionID);
                _agent.WriteMessage(nc.Name);
                _agent.SendMessage(senderConnection);
            }

            //  Add player to master GameState
            var player = new PlayerState("", NextPlayerID, senderConnection);
            _gameState.Players.Add(player);
            _server.IPDictionary.Add(senderConnection, player);
            _server.SomebodyOnceConnected = true; 
        }
    }
}
