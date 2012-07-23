//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using IsoGame.Events;
using IsoGame.Misc;
using IsoGame.Network;
using IsoGame.Screens.Base;
using IsoGame.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace IsoGame.Screens
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        readonly EventManager _eventManager;
        private ClientGame _game;
        private GameState _gameState;
        private NetworkAgent _agent;
        private AwesomiumUIManager _ui;

        ContentManager _content;

        GraphicsDevice _device;
        private SpriteBatch _spriteBatch;
        private Scroller _scroller;

        // Measure the framerate.
        //  We're locked at 60fps but can unlock in game.cs for testing
        int _frameRate;
        int _frameCounter;
        TimeSpan _elapsedTime;

        Random _random;

        float _pauseAlpha;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen(EventManager eventManager)
        {
            _random = new Random(Environment.TickCount);
            _eventManager = eventManager;
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (_content == null)
                _content = new ContentManager(ScreenManager.Game.Services, "Content");
            _device = ScreenManager.GraphicsDevice;
            _spriteBatch = ScreenManager.SpriteBatch;
            _game = (ClientGame)ScreenManager.Game;
            _agent = _game.Agent;
            _gameState = _game.GameState;
            _ui = _game.UI;
            _scroller = new Scroller(_game, _gameState);
            //  Add all components local to this screen


            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
            _eventManager.QueueEvent(new GameEvent(EventType.GetReady));
            //  Tell the server we're here
            _game.Agent.WriteMessage((byte)MessageType.PlayerEnteredMap);
            _game.Agent.WriteMessage(_game.PlayerID);
            _game.Agent.SendMessage(_game.Agent.Connections[0]);
            ScreenManager.Input.UiIsActive = true;
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {

            //  Unload content
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
                //  Do game stuff here
                _scroller.Update(gameTime, ScreenManager.Input);
            }


            if (!ScreenManager.Debug) return;

            // Measure our framerate.
            _elapsedTime += gameTime.ElapsedGameTime;

            if (_elapsedTime <= TimeSpan.FromSeconds(1)) return;
            _elapsedTime -= TimeSpan.FromSeconds(1);
            _frameRate = _frameCounter;
            _frameCounter = 0;
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            var playerIndex = (int)ControllingPlayer.Value;

            var keyboardState = input.CurrentKeyboardStates[playerIndex];
            var gamePadState = input.CurrentGamePadStates[playerIndex];
            var mouseState = input.CurrentMouseState;
            var lastMouseState = input.LastMouseState;

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            var gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                if (ScreenManager.Debug == true)
                    ScreenManager.Game.Exit();
                ScreenManager.AddScreen(new PauseMenuScreen(_eventManager), ControllingPlayer);
                return;
            }
            //  Otherwise send our game events corresponding to input received.
            
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {     
            _device.Clear(Color.Black);
            //  If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || _pauseAlpha > 0)
            {
                var alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, _pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }

            //  Draw shit
            _scroller.Draw(gameTime);

            //  Framerate debug text
            if (!ScreenManager.Debug) return;


            _frameCounter++;
        }
    }
}
