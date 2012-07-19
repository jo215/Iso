//-----------------------------------------------------------------------------
// ScreenManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IsoGame.Events;
using IsoGame.Misc;

namespace IsoGame.Screens.Base
{
    /// <summary>
    /// The screen manager is a component which manages one or more GameScreen
    /// instances. It maintains a stack of screens, calls their Update and Draw
    /// methods at the appropriate times, and automatically routes input to the
    /// topmost active screen.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        public EventManager EventManager { get; private set; }
        public AwesomiumUIManager UI { get; set; }

        readonly List<GameScreen> _screens = new List<GameScreen>();
        readonly List<GameScreen> _screensToUpdate = new List<GameScreen>();

        public InputState Input;

        Texture2D _blankTexture;

        bool _isInitialized;

        public bool Debug { get; set; }
        public bool DebugCollisionShapes { get; set; }

        /// <summary>
        /// A default SpriteBatch shared by all the screens. This saves
        /// each screen having to bother creating their own local instance.
        /// </summary>
        public SpriteBatch SpriteBatch { get; private set; }


        /// <summary>
        /// A default font shared by all the screens. This saves
        /// each screen having to bother loading their own local copy.
        /// </summary>
        public SpriteFont Font { get; private set; }


        /// <summary>
        /// If true, the manager prints out a list of all the screens
        /// each time it is updated. This can be useful for making sure
        /// everything is being added and removed at the right times.
        /// </summary>
        public bool TraceEnabled { get; set; }

        /// <summary>
        /// Constructs a new screen manager component.
        /// </summary>
        public ScreenManager(Game game, EventManager eventManager)
            : base(game)
        {
            Input = new InputState(game);
            EventManager = eventManager;
        }


        /// <summary>
        /// Initializes the screen manager component.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _isInitialized = true;
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load content belonging to the screen manager.
            var content = Game.Content;

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = content.Load<SpriteFont>("Fonts/menufont");
            _blankTexture = content.Load<Texture2D>("Textures/Menu/blank");

            // Tell each of the screens to load their content.
            foreach (var screen in _screens)
            {
                screen.LoadContent();
            }
        }


        /// <summary>
        /// Unload your graphics content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Tell each of the screens to unload their content.
            foreach (var screen in _screens)
            {
                screen.UnloadContent();
            }
        }

        /// <summary>
        /// Allows each screen to run logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (UI == null)
                UI = ((ClientGame)Game).UI;
            // Read the keyboard and gamepad.
            Input.Update();

            // Make a copy of the master screen list, to avoid confusion if
            // the process of updating one screen adds or removes others.
            _screensToUpdate.Clear();

            foreach (var screen in _screens)
                _screensToUpdate.Add(screen);

            var otherScreenHasFocus = !Game.IsActive;
            var coveredByOtherScreen = false;

            // Loop as long as there are screens waiting to be updated.
            while (_screensToUpdate.Count > 0)
            {
                // Pop the topmost screen off the waiting list.
                var screen = _screensToUpdate[_screensToUpdate.Count - 1];

                _screensToUpdate.RemoveAt(_screensToUpdate.Count - 1);

                // Update the screen.
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState != ScreenState.TransitionOn && screen.ScreenState != ScreenState.Active)
                    continue;
                // If this is the first active screen we came across,
                // give it a chance to handle input.
                if (!otherScreenHasFocus)
                {
                    screen.HandleInput(Input);

                    otherScreenHasFocus = true;
                }

                // If this is an active non-popup, inform any subsequent
                // screens that they are covered by it.
                if (!screen.IsPopup)
                    coveredByOtherScreen = true;
            }

            // Print debug trace?
            if (TraceEnabled)
                TraceScreens();
        }


        /// <summary>
        /// Prints a list of all the screens, for debugging.
        /// </summary>
        void TraceScreens()
        {
            System.Diagnostics.Debug.WriteLine(string.Join(", ", _screens.Select(screen => screen.GetType().Name).ToArray()));
        }


        /// <summary>
        /// Tells each screen to draw itself.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            foreach (var screen in _screens.Where(screen => screen.ScreenState != ScreenState.Hidden))
            {
                screen.Draw(gameTime);
            }
        }

        /// <summary>
        /// Adds a new screen to the screen manager.
        /// </summary>
        public void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
        {
            screen.ControllingPlayer = controllingPlayer;
            screen.ScreenManager = this;
            screen.IsExiting = false;

            // If we have a graphics device, tell the screen to load content.
            if (_isInitialized)
            {
                screen.LoadContent();
            }

            _screens.Add(screen);
            if (screen.UsesGameStateSound)
                EventManager.QueueEvent(new GameEvent(EventType.MenuSelect));
        }


        /// <summary>
        /// Removes a screen from the screen manager. You should normally
        /// use GameScreen.ExitScreen instead of calling this directly, so
        /// the screen can gradually transition off rather than just being
        /// instantly removed.
        /// </summary>
        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload content.
            if (_isInitialized)
            {
                screen.UnloadContent();
            }

            _screens.Remove(screen);
            _screensToUpdate.Remove(screen);

        }


        /// <summary>
        /// Expose an array holding all the screens. We return a copy rather
        /// than the real master list, because screens should only ever be added
        /// or removed using the AddScreen and RemoveScreen methods.
        /// </summary>
        public GameScreen[] GetScreens()
        {
            return _screens.ToArray();
        }


        /// <summary>
        /// Helper draws a translucent black fullscreen sprite, used for fading
        /// screens in and out, and for darkening the background behind popups.
        /// </summary>
        public void FadeBackBufferToBlack(float alpha)
        {
            var viewport = GraphicsDevice.Viewport;

            SpriteBatch.Begin();

            SpriteBatch.Draw(_blankTexture,
                             new Rectangle(0, 0, viewport.Width, viewport.Height),
                             Color.Black * alpha);

            SpriteBatch.End();
        }
    }
}
