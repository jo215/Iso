//-----------------------------------------------------------------------------
// InputState.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using IsoGame.Misc;
using Nuclex.Input;
namespace IsoGame.Screens.Base
{
    /// <summary>
    /// Helper for reading input from keyboard, gamepad, and touch input. This class 
    /// tracks both the current and previous state of the input devices, and implements 
    /// query methods for high level input actions such as "move up through the menu"
    /// or "pause the game".
    /// </summary>
    public class InputState
    {
        public const int MaxInputs = 4;

        public readonly KeyboardState[] CurrentKeyboardStates;
        public readonly GamePadState[] CurrentGamePadStates;

        public readonly KeyboardState[] LastKeyboardStates;
        public readonly GamePadState[] LastGamePadStates;

        public MouseState CurrentMouseState;
        public MouseState LastMouseState;

        public readonly bool[] GamePadWasConnected;

        readonly Game _game;
        
        AwesomiumUIManager _ui;
        private InputManager inputManager;
        private List<char> enteredText;
        public bool UiIsActive;

        /// <summary>
        /// Constructs a new input state.
        /// </summary>
        public InputState(Game game)
        {
            CurrentKeyboardStates = new KeyboardState[MaxInputs];
            CurrentGamePadStates = new GamePadState[MaxInputs];

            LastKeyboardStates = new KeyboardState[MaxInputs];
            LastGamePadStates = new GamePadState[MaxInputs];
            Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
            CurrentMouseState = new MouseState();
            LastMouseState = new MouseState();

            GamePadWasConnected = new bool[MaxInputs];
            _game = game;
            UiIsActive = true;
            //  Nuclex assisted input
            inputManager = new InputManager(_game.Window.Handle);
            _game.Components.Add(inputManager);
            enteredText = new List<char>();
            inputManager.GetKeyboard().CharacterEntered += KeyboardCharacterEntered;
        }

        /// <summary>
        /// Reads the latest state of the keyboard and gamepad.
        /// </summary>
        public void Update()
        {
            if (_ui == null)
                _ui = ((ClientGame)_game).UI;

            LastMouseState = CurrentMouseState;

            CurrentMouseState = Mouse.GetState();

            for (var i = 0; i < MaxInputs; i++)
            {
                LastKeyboardStates[i] = CurrentKeyboardStates[i];
                LastGamePadStates[i] = CurrentGamePadStates[i];

                CurrentKeyboardStates[i] = Keyboard.GetState((PlayerIndex)i);
                CurrentGamePadStates[i] = GamePad.GetState((PlayerIndex)i);
                
                // Keep track of whether a gamepad has ever been
                // connected, so we can detect if it is unplugged.
                if (CurrentGamePadStates[i].IsConnected)
                {
                    GamePadWasConnected[i] = true;
                }
            }

            UpdateAwesomium();
        }

        /// <summary>
        /// Sends updates to the Awesomium UI.
        /// </summary>
        private void UpdateAwesomium()
        {
            if (!UiIsActive)
                return;

            var mouseState = CurrentMouseState;
            var lastMouseState = LastMouseState;

            //  Left mouse up/down
            if (mouseState.LeftButton == ButtonState.Pressed)
                _ui.LeftButtonDown();

            if (mouseState.LeftButton == ButtonState.Released && lastMouseState.LeftButton == ButtonState.Pressed)
                _ui.LeftButtonUp();

            //  Mouse move & Scroll wheel
            _ui.MouseMoved(mouseState.X, mouseState.Y);
            _ui.ScrollWheel(mouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue);
            
            //  Awesomium & Nuclex text input
            foreach(var c in enteredText)
            {
                _ui.KeyDown(c);
                _ui.KeyUp(c);
            }
            enteredText.Clear();
        }

        private void KeyboardCharacterEntered(char character)
        {
            enteredText.Add(character);
        }

        /// <summary>
        /// Helper for checking if a key was newly pressed during this update. The
        /// controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a keypress
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewKeyPress(Keys key, PlayerIndex? controllingPlayer,
                                            out PlayerIndex playerIndex)
        {
            if (!controllingPlayer.HasValue)
            {
                // Accept input from any player.
                return (IsNewKeyPress(key, PlayerIndex.One, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Two, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Three, out playerIndex) ||
                        IsNewKeyPress(key, PlayerIndex.Four, out playerIndex));
            }
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            var i = (int) playerIndex;

            return (CurrentKeyboardStates[i].IsKeyDown(key) &&
                    LastKeyboardStates[i].IsKeyUp(key));
        }


        /// <summary>
        /// Helper for checking if a button was newly pressed during this update.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When a button press
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsNewButtonPress(Buttons button, PlayerIndex? controllingPlayer,
                                                     out PlayerIndex playerIndex)
        {
            if (!controllingPlayer.HasValue)
            {
                // Accept input from any player.
                return (IsNewButtonPress(button, PlayerIndex.One, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Two, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Three, out playerIndex) ||
                        IsNewButtonPress(button, PlayerIndex.Four, out playerIndex));
            }
            // Read input from the specified player.
            playerIndex = controllingPlayer.Value;

            var i = (int) playerIndex;

            return (CurrentGamePadStates[i].IsButtonDown(button) &&
                    LastGamePadStates[i].IsButtonUp(button));
        }

        /// <summary>
        /// Checks for left-mouse click.
        /// </summary>
        /// <returns></returns>
        public bool IsNewLeftClick()
        {
            return (CurrentMouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed);
        }

        /// <summary>
        /// Checks for right-mouse click.
        /// </summary>
        /// <returns></returns>
        public bool IsNewRightClick()
        {
            return (CurrentMouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed);
        }

        /// <summary>
        /// Checks for scroll-wheel mouse click.
        /// </summary>
        /// <returns></returns>
        public bool IsNewScrollWheelClick()
        {
            return (CurrentMouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed);
        }

        /// <summary>
        /// Checks if mouse scrolled up.
        /// </summary>
        /// <returns></returns>
        public bool IsNewMouseScrollUp()
        {
            return (CurrentMouseState.ScrollWheelValue > LastMouseState.ScrollWheelValue);
        }

        /// <summary>
        /// Checks if mouse scrolled down.
        /// </summary>
        /// <returns></returns>
        public bool IsNewMouseScrollDown()
        {
            return (CurrentMouseState.ScrollWheelValue < LastMouseState.ScrollWheelValue);
        }

        /// <summary>
        /// Checks for a "menu select" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuSelect(PlayerIndex? controllingPlayer,
                                 out PlayerIndex playerIndex)
        {
            return IsNewKeyPress(Keys.Enter, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.A, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
        }


        /// <summary>
        /// Checks for a "menu cancel" input action.
        /// The controllingPlayer parameter specifies which player to read input for.
        /// If this is null, it will accept input from any player. When the action
        /// is detected, the output playerIndex reports which player pressed it.
        /// </summary>
        public bool IsMenuCancel(PlayerIndex? controllingPlayer,
                                 out PlayerIndex playerIndex)
        {
            return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.B, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Back, controllingPlayer, out playerIndex);
        }


        /// <summary>
        /// Checks for a "menu up" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuUp(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Up, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.DPadUp, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.LeftThumbstickUp, controllingPlayer, out playerIndex) ||
                   IsNewMouseScrollUp();
        }


        /// <summary>
        /// Checks for a "menu down" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsMenuDown(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Down, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.DPadDown, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.LeftThumbstickDown, controllingPlayer, out playerIndex) ||
                   IsNewMouseScrollDown();
        }


        /// <summary>
        /// Checks for a "pause the game" input action.
        /// The controllingPlayer parameter specifies which player to read
        /// input for. If this is null, it will accept input from any player.
        /// </summary>
        public bool IsPauseGame(PlayerIndex? controllingPlayer)
        {
            PlayerIndex playerIndex;

            return IsNewKeyPress(Keys.Escape, controllingPlayer, out playerIndex) ||
                   IsNewButtonPress(Buttons.Start, controllingPlayer, out playerIndex);
        }
    }
}
