//-----------------------------------------------------------------------------
// MenuScreen.cs
//
// XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IsoGame.Events;

namespace IsoGame.Screens.Base
{
    /// <summary>
    /// Base class for screens that contain a menu of options. The user can
    /// move up and down to select an entry, or cancel to back out of the screen.
    /// </summary>
    abstract class MenuScreen : GameScreen
    {
        protected EventManager EventManager;

        readonly List<MenuEntry> _menuEntries = new List<MenuEntry>();
        int _selectedEntry;
        readonly string _menuTitle;

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> MenuEntries
        {
            get { return _menuEntries; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MenuScreen(string menuTitle, EventManager eventManager)
        {
            EventManager = eventManager;

            _menuTitle = menuTitle;

            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or cancelling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            // Move to the previous menu entry?
            if (input.IsMenuUp(ControllingPlayer))
            {
                EventManager.QueueEvent(new GameEvent(EventType.MenuHover));
                _selectedEntry--;

                if (_selectedEntry < 0)
                    _selectedEntry = _menuEntries.Count - 1;
            }

            // Move to the next menu entry?
            if (input.IsMenuDown(ControllingPlayer))
            {
                EventManager.QueueEvent(new GameEvent(EventType.MenuHover));
                _selectedEntry++;

                if (_selectedEntry >= _menuEntries.Count)
                    _selectedEntry = 0;
            }

            //  Mouse selection of menu entries
                for (var i = 0; i < _menuEntries.Count; i++)
                    // ReSharper disable PossibleLossOfFraction
                    if (ScreenManager != null && (input.LastMouseState.Y >= _menuEntries[i].Position.Y - ScreenManager.Font.LineSpacing / 2 && input.LastMouseState.Y < _menuEntries[i].Position.Y + ScreenManager.Font.LineSpacing / 2
                    // ReSharper restore PossibleLossOfFraction
                                                  && input.LastMouseState.X > _menuEntries[i].Position.X && input.LastMouseState.X < _menuEntries[i].Position.X + _menuEntries[i].GetWidth(this)))
                    {
                        if (i != _selectedEntry && TransitionAlpha > 0.9999)
                        {
                            EventManager.QueueEvent(new GameEvent(EventType.MenuHover));
                        }
                        _selectedEntry = i;
                        if (input.IsNewLeftClick())
                        {
                            OnSelectEntry(i, PlayerIndex.One);
                        }
                    }

            // Accept or cancel the menu? We pass in our ControllingPlayer, which may
            // either be null (to accept input from any player) or a specific index.
            // If we pass a null controlling player, the InputState helper returns to
            // us which player actually provided the input. We pass that through to
            // OnSelectEntry and OnCancel, so they can tell which player triggered them.
            PlayerIndex playerIndex;

            if (input.IsMenuSelect(ControllingPlayer, out playerIndex))
            {
                OnSelectEntry(_selectedEntry, playerIndex);
            }
            else if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
            {
                OnCancel(playerIndex);
            }
        }


        /// <summary>
        /// Handler for when the user has chosen a menu entry.
        /// </summary>
        protected virtual void OnSelectEntry(int entryIndex, PlayerIndex playerIndex)
        {
            _menuEntries[entryIndex].OnSelectEntry(playerIndex);
        }


        /// <summary>
        /// Handler for when the user has cancelled the menu.
        /// </summary>
        protected virtual void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }


        /// <summary>
        /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
        /// </summary>
        protected void OnCancel(object sender, PlayerIndexEventArgs e)
        {
            OnCancel(e.PlayerIndex);
        }

        /// <summary>
        /// Allows the screen the chance to position the menu entries. By default
        /// all menu entries are lined up in a vertical list, centered on the screen.
        /// </summary>
        protected virtual void UpdateMenuEntryLocations()
        {
            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            var transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // start at Y = 275; each X value is generated per entry
            var position = new Vector2(0f, 275f);

            // update each menu entry's location in turn
            foreach (var menuEntry in _menuEntries)
            {
                // each entry is to be centered horizontally
                position.X = ScreenManager.GraphicsDevice.Viewport.Width / 2 - menuEntry.GetWidth(this) / 2;

                if (ScreenState == ScreenState.TransitionOn)
                    position.X -= transitionOffset * 256;
                else
                    position.X += transitionOffset * 512;

                // set the entry's position
                menuEntry.Position = position;

                // move down for the next entry the size of this entry
                position.Y += menuEntry.GetHeight(this);
            }
        }


        /// <summary>
        /// Updates the menu.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Update each nested MenuEntry object.
            for (var i = 0; i < _menuEntries.Count; i++)
            {
                var isSelected = IsActive && (i == _selectedEntry);

                _menuEntries[i].Update(this, isSelected, gameTime);
            }
        }


        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // make sure our entries are in the right place before we draw them
            UpdateMenuEntryLocations();

            var graphics = ScreenManager.GraphicsDevice;
            var spriteBatch = ScreenManager.SpriteBatch;
            var font = ScreenManager.Font;

            spriteBatch.Begin();

            // Draw each menu entry in turn.
            for (var i = 0; i < _menuEntries.Count; i++)
            {
                var menuEntry = _menuEntries[i];

                var isSelected = IsActive && (i == _selectedEntry);

                menuEntry.Draw(this, isSelected, gameTime);
            }

            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            var transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // Draw the menu title centered on the screen
            // ReSharper disable PossibleLossOfFraction
            var titlePosition = new Vector2(graphics.Viewport.Width / 2, 80);
            // ReSharper restore PossibleLossOfFraction
            var titleOrigin = font.MeasureString(_menuTitle) / 2;
            var titleColor = Color.White * TransitionAlpha;
            const float titleScale = 1.25f;

            titlePosition.Y -= transitionOffset * 100;

            spriteBatch.DrawString(font, _menuTitle, titlePosition, titleColor, 0,
                                   titleOrigin, titleScale, SpriteEffects.None, 0);

            spriteBatch.End();
        }
    }
}
