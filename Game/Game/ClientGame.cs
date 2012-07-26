using System.IO;
using System.Linq;
using Editor.Model;
using IsoGame.Screens.Base;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using IsoGame.Screens;
using IsoGame.Events;
using IsoGame.Audio;
using IsoGame.State;
using IsoGame.Network;
using IsoGame.Misc;
using IsoGame.Processes;

namespace IsoGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ClientGame : Game
    {
        internal string Tag = "isogame";
        internal string ServerIP = "localhost";
        internal int ServerPort = 11000;
        internal NetworkAgent Agent;

        internal AwesomiumUIManager UI;

        readonly GraphicsDeviceManager _graphics;
        internal SpriteBatch SpriteBatch;

        public static EventManager _eventManager;
        public static ProcessManager _processManager;
        readonly ScreenManager _screenManager;
        readonly AudioManager _audioManager;
        
        internal GameState GameState;

        // ReSharper disable ConvertToConstant.Local
        private readonly bool _debug;
        // ReSharper restore ConvertToConstant.Local
        public byte PlayerID;
        public string PlayerName;
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientGame() 
        {
            _debug = true;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Most games will want to leave both these values set to true to ensure
            // smoother updates, but when you are doing performance work it can be
            // useful to set them to false in order to get more accurate measurements.
            //IsFixedTimeStep = false;
            //graphics.SynchronizeWithVerticalRetrace = false;

            //  Full screen mode @ desktop resolution
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferFormat = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

            IsMouseVisible = true;

            //  Hook up the event & process managers
            _eventManager = new EventManager(this);
            Components.Add(_eventManager);
            _processManager = new ProcessManager(this);
            Components.Add(_processManager);

            //  Starting screens
            _screenManager = new ScreenManager(this, _eventManager) { Debug = _debug };
            Components.Add(_screenManager);

            //  Start the threaded audio manager component
            _audioManager = new AudioManager(this, _eventManager);

            //  Create a new blank GameState
            GameState = new GameState(Content.RootDirectory + "\\Textures\\mousemap.png");
            Agent = new NetworkAgent(AgentRole.Client, Tag);

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            UI = new AwesomiumUIManager(this, "");
            UI.Initialize();

            if (_debug)
            {
                //  Start an connect to a server
                PlayerName = "Jimmy DEBUG";
                new Server(Tag, Content);
                Agent.Connect(ServerIP, ServerPort);
                var gotInfo = false;
                while (!gotInfo)
                {
                    //  Wait for response and update game state accordingly
                    var messages = Agent.CheckForMessages();
                    foreach (var m in messages.Where(m => m.MessageType == NetIncomingMessageType.Data))
                    {
                        switch ((MessageType) m.ReadByte())
                        {
                            case MessageType.PlayerID:
                                //  THIS player has been confirmed logged on
                                PlayerID = m.ReadByte();
                                GameState.Players.Add(new PlayerState(PlayerName, PlayerID));
                                //  Send our name info to server
                                Agent.WriteMessage((byte) MessageType.PlayerInfo);
                                Agent.WriteMessage(PlayerID);
                                Agent.WriteMessage(PlayerName);
                                Agent.SendMessage(Agent.Connections[0]);
                                gotInfo = true;
                                break;
                        }
                    }
                }
                //  Send the map
                using (var fs = new StreamReader(Content.RootDirectory + "\\Maps\\" + "TestMap1.jim"))
                {
                    var big = fs.ReadToEnd();
                    var small = StringCompressor.CompressString(big);
                    //  Send the small map to the server
                    Agent.WriteMessage((byte)MessageType.MapUpload);
                    Agent.WriteMessage(PlayerID);
                    Agent.WriteMessage(small);
                    Agent.SendMessage(Agent.Connections[0]);
                }
                //  Receive the map
                gotInfo = false;
                while (!gotInfo)
                {
                    //  Wait for response and update game state
                    var messages = Agent.CheckForMessages();
                    foreach (var m in messages.Where(m => m.MessageType == NetIncomingMessageType.Data))
                    {
                        switch ((MessageType) m.ReadByte())
                        {

                            case MessageType.MapUpload:
                                m.ReadByte();   //  sender - us!
                                GameState.Module = Module.OpenModule(
                                    StringCompressor.DecompressString(m.ReadString()),
                                    GameState.iso, true);
                                gotInfo = true;
                                break;
                        }
                    }
                }

                LoadingScreen.Load(_screenManager, true, PlayerIndex.One, new GameplayScreen(_eventManager));
            }
            else
            {
                _screenManager.AddScreen(new BackgroundScreen(), null);
                _screenManager.AddScreen(new MainMenuScreen(_eventManager), null);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            //  Close UI
            _screenManager.Input.UiIsActive = false;
            UI.WebView.Stop();
            UI.WebView.Close();
            UI.Dispose();
            //  Audio thread
            _audioManager.IsRunning = false;
            //  Close the socket connection
            Agent.WriteMessage((byte)MessageType.PlayerQuit);
            Agent.WriteMessage(PlayerID);
            Agent.SendMessage(Agent.Connections[0]);
            Agent.Shutdown();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            UI.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //  ui.Draw() is called from within individual Screens to ensure it's always drawn last

            base.Draw(gameTime);
        }
    }
}
