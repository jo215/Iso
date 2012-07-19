using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;

namespace IsoGame.Network
{
    public enum AgentRole
    {
        Client, Server
    }

    public class NetworkAgent
    {
        private NetPeer _peer;
        private readonly NetPeerConfiguration _config;
        private readonly AgentRole _role;

        private NetOutgoingMessage _outgoingMessage;
        private List<NetIncomingMessage> _incomingMessages;

        public List<NetConnection> Connections
        {
            get { return _peer.Connections; }
        }

        /// <summary>
        /// Customize appIdentifier. Note: Client and server appIdentifier must be the same.
        /// </summary>
        public NetworkAgent(AgentRole role, string tag)
        {
            _role = role;
            _config = new NetPeerConfiguration(tag);

            Initialize();
        }

        /// <summary>
        /// Initialises the agent based on its role.
        /// </summary>
        private void Initialize()
        {
            if (_role == AgentRole.Server)
            {
                _config.MaximumConnections = 8;
                _config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
                _config.Port = 11000;
                //Casts the NetPeer to a NetServer
                _peer = new NetServer(_config);
            }
            if (_role == AgentRole.Client)
            {
                _config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
                //Casts the NetPeer to a NetClient
                _peer = new NetClient(_config);
            }
            _incomingMessages = new List<NetIncomingMessage>();
            _outgoingMessage = _peer.CreateMessage();

            _peer.Start();

        }

        /// <summary>
        /// Connects to a server. Throws an exception if you attempt to call Connect as a Server.
        /// </summary>
        public void Connect(string ip, int port)
        {
            if (_role == AgentRole.Client)
            {
                _peer.Connect(ip, port);
            }
            else
            {
                throw new SystemException("Attempted to connect as server. Only clients should connect.");
            }
        }

        /// <summary>
        /// Reads every message in the queue and returns a list of data messages.
        /// Other message types just write a Console note.
        /// This should be called every update by the Game Screen
        /// The Game Screen should implement the actual handling of messages.
        /// </summary>
        /// <returns></returns>
        public List<NetIncomingMessage> CheckForMessages()
        {
            _incomingMessages.Clear();
            NetIncomingMessage incomingMessage;
            var output = "";

            while ((incomingMessage = _peer.ReadMessage()) != null)
            {
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.DiscoveryRequest:
                        _peer.SendDiscoveryResponse(null, incomingMessage.SenderEndpoint);
                        break;
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        if (_role == AgentRole.Server)
                            output += incomingMessage.ReadString() + "\n";
                        break;
                    case NetIncomingMessageType.StatusChanged:

                        if (_role == AgentRole.Server)
                        {
                            output += "Status Message: " + incomingMessage.ReadString() + "\n";
                            //  Server gets notified of logon / logoff as well as standard data messages
                            _incomingMessages.Add(incomingMessage);
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        _incomingMessages.Add(incomingMessage);
                        break;
                }
            }
            if (_role == AgentRole.Server)
            {
                var textOut = new StreamWriter(new FileStream("log.txt", FileMode.Append, FileAccess.Write));
                textOut.Write(output);
                textOut.Close();
            }
            return _incomingMessages;
        }

        /// <summary>
        /// Sends off _outgoingMessage and then clears it for the next send.
        /// Defaults to ReliableSequenced for safer but much slower transmission.
        /// </summary>
        public void SendMessage(NetConnection recipient)
        {
            SendMessage(recipient, true);
        }

        public void SendMessage(NetConnection recipient, bool isGuaranteed)
        {
            var method = isGuaranteed ? NetDeliveryMethod.ReliableOrdered : NetDeliveryMethod.UnreliableSequenced;
            _peer.SendMessage(_outgoingMessage, recipient, method);
            _outgoingMessage = _peer.CreateMessage();
        }

        /// <summary>
        /// Write bool to message
        /// </summary>
        public void WriteMessage(bool message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Write byte to message
        /// </summary>
        public void WriteMessage(byte message)
        {
            _outgoingMessage.Write(message);
            
        }

        /// <summary>
        /// Write Int16 to message
        /// </summary>
        public void WriteMessage(Int16 message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Write Int32 to message
        /// </summary>
        public void WriteMessage(Int32 message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Write Int64 to message
        /// </summary>
        public void WriteMessage(Int64 message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Write float to message
        /// </summary>
        public void WriteMessage(float message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Write double to message
        /// </summary>
        public void WriteMessage(double message)
        {
            _outgoingMessage.Write(message);
        }

        public void WriteMessage(string message)
        {
            _outgoingMessage.Write(message);
        }

        /// <summary>
        /// Closes the NetPeer
        /// </summary>
        public void Shutdown()
        {
            _peer.Shutdown("Closing connection.");
        }
    }
}
