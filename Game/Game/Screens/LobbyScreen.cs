using System;
using System.Linq;
using IsoGame.Screens.Base;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using IsoGame.Events;
using IsoGame.Misc;
using Awesomium.Core;
using System.Collections.Generic;
using Lidgren.Network;
using IsoGame.Network;
using IsoGame.State;
using System.IO;
using IsoTools;

namespace IsoGame.Screens
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class LobbyScreen : GameScreen
    {
        readonly EventManager _eventManager;
        AwesomiumUIManager _ui;
        ContentManager _content;
        private NetworkAgent _agent;
        private GameState _gameState;
        private ClientGame _game;
        readonly List<KeyValuePair<PlayerState, string>> _receivedChat;
        byte playersInMap;
        float _pauseAlpha;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LobbyScreen(EventManager eventManager)
        {
            _receivedChat = new List<KeyValuePair<PlayerState, string>>();
            _eventManager = eventManager;
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            UsesGameStateSound = false;
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (_content == null)
                _content = new ContentManager(ScreenManager.Game.Services, "Content");

            //  Add all components local to this screen

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
            _eventManager.QueueEvent(new GameEvent(EventType.GetReady));
            _game = (ClientGame)ScreenManager.Game;
            _agent = _game.Agent;
            _gameState = _game.GameState;
            _ui = _game.UI;
            _ui.LoadFile("Content\\html\\lobby.html");
            _ui.WebView.LoadCompleted += WebViewLoadCompleted;
        }

        /// <summary>
        /// UI initialisation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebViewLoadCompleted(Object sender,EventArgs e)
        {
            _ui.CreateObject("UIEventManager", "click", WebEventManager);
            _ui.WebView.ExecuteJavascript("disableSelection(document.body);");
            _ui.WebView.ExecuteJavascript("document.getElementById('input').focus();");
            
            PushPlayerUpdates();
            PushAvailableMaps();
            ScreenManager.Input.UiIsActive = true;
        }

        /// <summary>
        /// Displays available maps.
        /// </summary>
        private void PushAvailableMaps()
        {
            foreach (var f in new DirectoryInfo(_content.RootDirectory + "\\Maps").GetFiles().Where(f => f.Extension.ToLower().EndsWith("jim")))
            {
                _ui.PushData("", "addMap", new JSValue(f.Name));
            }
        }

        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            _content.Unload();
        }

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            _pauseAlpha = coveredByOtherScreen ? Math.Min(_pauseAlpha + 1f / 32, 1) : Math.Max(_pauseAlpha - 1f / 32, 0);

            if (IsActive)
            {
                //ScreenManager.Game.IsMouseVisible = false;
                
                //  Do game stuff here
                
                //  Check for messages
                HandleMessages();
            }
        }

        /// <summary>
        /// Deals with all incoming communication from server.
        /// </summary>
        private void HandleMessages()
        {

            var messages = _agent.CheckForMessages();
            foreach (var m in messages.Where(m => m.MessageType == NetIncomingMessageType.Data))
            {
                switch ((MessageType) m.ReadByte())
                {
                    case MessageType.PlayerID:
                        //  THIS player has been confirmed logged on
                        _game.PlayerID = m.ReadByte();
                        _gameState.Players.Add(new PlayerState(_game.PlayerName, _game.PlayerID));
                        //  Send our name info to server
                        _agent.WriteMessage((byte)MessageType.PlayerInfo);
                        _agent.WriteMessage(_game.PlayerID);
                        _agent.WriteMessage(_game.PlayerName);
                        _agent.SendMessage(_agent.Connections[0]);
                        PushPlayerUpdates();
                        //  If we're the master show the map selector
                        if (_game.PlayerID == 1)
                            _ui.WebView.ExecuteJavascript("document.getElementById('mapPanel').style.visibility='visible'");
                        break;

                    case MessageType.PlayerJoined:
                        //  A different player has logged on
                        _gameState.Players.Add(new PlayerState("", m.ReadByte()));
                        PushPlayerUpdates();
                        break;

                    case MessageType.PlayerInfo:
                        //  Update our local state info
                        _gameState.GetPlayerByID(m.ReadByte()).Name = m.ReadString();
                        PushPlayerUpdates();
                        break;

                    case MessageType.PlayerQuit:
                        _gameState.Players.Remove(_gameState.GetPlayerByID(m.ReadByte()));
                        PushPlayerUpdates();
                        break;

                    case MessageType.Chat:
                        //  Update chatbox
                        var sessionID = m.ReadByte();
                        _receivedChat.Add(new KeyValuePair<PlayerState, string>(_gameState.GetPlayerByID(sessionID), m.ReadString()));
                        PushChatUpdates();
                        break;

                    case MessageType.MapUpload:
                        var sender = m.ReadByte();
                        _gameState.Module = Module.OpenModule(StringCompressor.DecompressString(m.ReadString()),
                                                               _gameState.iso, true);
                        _receivedChat.Add(new KeyValuePair<PlayerState, string>(_gameState.GetPlayerByID(sender), "Sent a map which was received OK."));
                        PushChatUpdates();
                        _ui.WebView.ExecuteJavascript("document.getElementById('startButton').style.visibility='visible'");
                        break;

                    case MessageType.PlayerEnteredMap:
                        playersInMap++;
                        sender = m.ReadByte();
                        _receivedChat.Add(new KeyValuePair<PlayerState, string>(_gameState.GetPlayerByID(sender), "Entered the map."));
                        PushChatUpdates();
                        break;
                }
            }
        }

        private void PushChatUpdates()
        {
            _ui.PushData("", "clearElement", new JSValue("messages"));
            for (var i = _receivedChat.Count - 1; i > _receivedChat.Count - 26 && i >= 0; i--)
            {
                _ui.PushData("", "addMessage", new JSValue(_receivedChat[i].Key.Name + ": " + _receivedChat[i].Value), new JSValue(_receivedChat[i].Key.ColorText()));
            }
        }

        /// <summary>
        /// Pushes the player list to the UI.
        /// </summary>
        private void PushPlayerUpdates()
        {     
            var game = (ClientGame)ScreenManager.Game;
            _ui.PushData("", "clearElement", new JSValue("players"));
            foreach (var t in game.GameState.Players)
                _ui.PushData("", "addPlayer", new JSValue(t.SessionID), new JSValue(t.Name), new JSValue(t.ColorText()));
        }

        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when this screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            if (ControllingPlayer != null)
            {
                var playerIndex = (int)ControllingPlayer.Value;

                var gamePadState = input.CurrentGamePadStates[playerIndex];

                // The game pauses either if the user presses the pause button, or if
                // they unplug the active gamepad. This requires us to keep track of
                // whether a gamepad was ever plugged in, because we don't want to pause
                // on PC if they are playing with a keyboard and have no gamepad at all!
                var gamePadDisconnected = !gamePadState.IsConnected &&
                                          input.GamePadWasConnected[playerIndex];

                if (!input.IsPauseGame(ControllingPlayer) && !gamePadDisconnected) return;
            }

            ScreenManager.AddScreen(new PauseMenuScreen(_eventManager), ControllingPlayer);
            //  Otherwise send our game events corresponding to input received.

        }

        /// <summary>
        /// UI interaction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WebEventManager(object sender, JSCallbackEventArgs e)
        {

            switch (e.Arguments[0].ToString())
            {
                case "exit":
                    ScreenManager.AddScreen(new ExitScreen(_eventManager), ControllingPlayer);
                    break;

                case "send":
                    //  Guard against no connection 
                    if (_agent.Connections.Count > 0 && e.Arguments[1].ToString() != null && e.Arguments[1].ToString().Length > 0)
                    {
                        _agent.WriteMessage((byte)MessageType.Chat);
                        _agent.WriteMessage(_game.PlayerID);
                        _agent.WriteMessage(e.Arguments[1].ToString());
                        _agent.SendMessage(_agent.Connections[0]);
                        _ui.WebView.ExecuteJavascript("document.getElementById('input').focus();");
                    }
                    break;

                case "selectmap":
                    LoadAndSendMap(e.Arguments[1].ToString());
                    break;

                case "start":
                    if (_gameState.Module != null)
                    {
                        ScreenManager.Input.UiIsActive = false;
                        LoadingScreen.Load(ScreenManager, true, PlayerIndex.One, new GameplayScreen(_eventManager));
                    }
                    break;
            }
        }

        /// <summary>
        /// Loads the given map, zips it up and sends to server.
        /// </summary>
        /// <param name="fileName"></param>
        private void LoadAndSendMap(string fileName)
        {
            //  Don't use this cheat - receive everything from network!
            //_gameState.Map = MapDefinition.OpenMap(_content.RootDirectory + "\\Maps\\" + fileName, _gameState.iso, false);

            _receivedChat.Add(new KeyValuePair<PlayerState, string>(PlayerState.Server, "Beginning map upload...."));
            PushChatUpdates();
            using(var fs = new StreamReader(_content.RootDirectory + "\\Maps\\" + fileName))
            {
                var big = fs.ReadToEnd();
                var small = StringCompressor.CompressString(big);
                //  Send the small map to the server
                _agent.WriteMessage((byte)MessageType.MapUpload);
                _agent.WriteMessage(_game.PlayerID);
                _agent.WriteMessage(small);
                _agent.SendMessage(_agent.Connections[0]);
            }

        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            //  Draw gameplay specific components
            _ui.Draw(gameTime);

            //  If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition <= 0 && _pauseAlpha <= 0) return;
            var alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, _pauseAlpha / 2);

            ScreenManager.FadeBackBufferToBlack(alpha);
        }
    }
}
