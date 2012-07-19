//-----------------------------------------------------------------------------
// MainMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.IO;
using IsoGame.Network;
using IsoGame.Screens.Base;
using Microsoft.Xna.Framework;
using IsoGame.Events;
using System;
using System.Net;
using Awesomium.Core;

namespace IsoGame.Screens
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        private bool serverStarted;
        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen(EventManager eventManager)
            : base("Main Menu", eventManager)
        {
            // Create our menu entries.
            var startServerMenuEntry = new MenuEntry("Start server");
            var connectToServerMenuEntry = new MenuEntry("Connect to a server");
            var optionsMenuEntry = new MenuEntry("Options");
            var exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers.
            startServerMenuEntry.Selected += StartServerMenuEntrySelected;
            connectToServerMenuEntry.Selected += ConnectToServerMenuEntrySelected;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(startServerMenuEntry);
            MenuEntries.Add(connectToServerMenuEntry);
            MenuEntries.Add(optionsMenuEntry);
            MenuEntries.Add(exitMenuEntry);  
        }

        /// <summary>
        /// Event handler for when the Start server menu entry is selected.
        /// </summary>
        void StartServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            var game = (ClientGame)ScreenManager.Game;
            if (!serverStarted)
            //  Start the server
            {
                new Server(game.Tag, game.Content);
                serverStarted = true;
            }

            //  Start the client & connect
            CheckPlayerProfile();
        }

        private void CheckPlayerProfile()
        {
            ScreenManager.UI.LoadFile("Content\\html\\profile.html");
            ScreenManager.UI.WebView.LoadCompleted += ProfileSettingsLoadCompleted;
        }

        private void ConnectToServer(PlayerIndexEventArgs e)
        {
            var game = (ClientGame)ScreenManager.Game;
            game.Agent.Connect(game.ServerIP, game.ServerPort);
            //  Hope for response
            try
            {
                ScreenManager.Input.UiIsActive = false;
                LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, new LobbyScreen(EventManager));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Event handler for when the Connect to server menu entry is selected.
        /// </summary>
        void ConnectToServerMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.UI.LoadFile("Content\\html\\serversettings.html");
            ScreenManager.UI.WebView.LoadCompleted += ServerSettingsLoadCompleted;
        }

        /// <summary>
        /// Profile UI initialisation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProfileSettingsLoadCompleted(Object sender, EventArgs e)
        {
            ScreenManager.UI.CreateObject("UIEventManager", "click", WebEventManager);
            ScreenManager.UI.WebView.ExecuteJavascript("disableSelection(document.body);");
            ScreenManager.UI.WebView.ExecuteJavascript("document.getElementById('playerName').focus();");
            //  Get previously entered profile
            if (!File.Exists(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\profile.ini")) return;
            using(var fs = new StreamReader(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\profile.ini"))
            {
                ScreenManager.UI.WebView.ExecuteJavascript("document.getElementById('playerName').value = '" + fs.ReadLine() + "';");   
            }
        }

        /// <summary>
        /// Connect to server UI initialisation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerSettingsLoadCompleted(Object sender, EventArgs e)
        {
            ScreenManager.UI.CreateObject("UIEventManager", "click", WebEventManager);
            ScreenManager.UI.WebView.ExecuteJavascript("disableSelection(document.body);");
            ScreenManager.UI.WebView.ExecuteJavascript("document.getElementById('serverAddress').focus();");
            //  Get previously entered server settings
            if (!File.Exists(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\serverIP.ini")) return;
            using (var fs = new StreamReader(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\serverIP.ini"))
            {
                ScreenManager.UI.WebView.ExecuteJavascript("document.getElementById('serverAddress').value = '" + fs.ReadLine() + "';");
            }
        }

        /// <summary>
        /// Remote server settings UI interaction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WebEventManager(object sender, JSCallbackEventArgs e)
        {
            switch (e.Arguments[0].ToString())
            {
                case "send":
                    //  Clicked Connect on server connection screen
                    IPAddress serverIP;
                    int port;
                    if (IPAddress.TryParse(e.Arguments[1].ToString(), out serverIP) &&
                        int.TryParse(e.Arguments[2].ToString(), out port))
                    {
                        var game = (ClientGame)ScreenManager.Game;
                        game.ServerIP = e.Arguments[1].ToString();
                        game.ServerPort = port;
                        //  save settings
                        using (var fs = new StreamWriter(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\serverIP.ini"))
                        {
                            fs.WriteLine(game.ServerIP);
                        }
                        CheckPlayerProfile();
                    }
                    break;
                case "update":
                    //  Clicked to enter name
                    if (e.Arguments[1].ToString().Length > 0)
                    {
                        ((ClientGame) ScreenManager.Game).PlayerName = e.Arguments[1].ToString();
                        //  save settings
                        using (var fs = new StreamWriter(ScreenManager.Game.Content.RootDirectory + "\\Preferences\\profile.ini"))
                        {
                            fs.WriteLine(e.Arguments[1].ToString());
                        }
                        ConnectToServer(new PlayerIndexEventArgs(PlayerIndex.One));
                    }
                    break;
                case "cancel":
                    ScreenManager.UI.LoadFile("");
                    break;
            }
        }

        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        void OptionsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen(EventManager), e.PlayerIndex);
        }


        /// <summary>
        /// When the user cancels the main menu, goto the Exit screen.
        /// </summary>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.AddScreen(new ExitScreen(EventManager), playerIndex);
        }

        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            ScreenManager.UI.Draw(gameTime);
        }
    }
}
