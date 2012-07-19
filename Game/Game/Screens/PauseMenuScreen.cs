//-----------------------------------------------------------------------------
// PauseMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using IsoGame.Screens.Base;
using Microsoft.Xna.Framework;
using IsoGame.Events;

namespace IsoGame.Screens
{
    /// <summary>
    /// The pause menu comes up over the top of the game,
    /// giving the player options to resume or quit.
    /// </summary>
    class PauseMenuScreen : MenuScreen
    {
        private bool _paused;
        /// <summary>
        /// Constructor.
        /// </summary>
        public PauseMenuScreen(EventManager eventManager)
            : base("Paused", eventManager)
        {
            // Create our menu entries.
            var resumeGameMenuEntry = new MenuEntry("Resume Game");
            var optionsGameMenuEntry = new MenuEntry("Options");
            var quitGameMenuEntry = new MenuEntry("Quit");
            
            // Hook up menu event handlers.
            resumeGameMenuEntry.Selected += OnCancel;
            optionsGameMenuEntry.Selected += OptionsGameMenuEntrySelected;
            quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;

            // Add entries to the menu.
            MenuEntries.Add(resumeGameMenuEntry);
            MenuEntries.Add(optionsGameMenuEntry);
            MenuEntries.Add(quitGameMenuEntry);
            UsesGameStateSound = false;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (!_paused)
            {
                ScreenManager.EventManager.QueueEvent(new GameEvent(EventType.Paused));
                _paused = true;
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        /// <summary>
        /// Event handler for when the Options Game menu entry is selected.
        /// </summary>
        void OptionsGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen(EventManager), e.PlayerIndex);
        }

        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new ExitScreen(EventManager), e.PlayerIndex);
        }
    }
}
