//-----------------------------------------------------------------------------
// OptionsMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using IsoGame.Screens.Base;
using IsoGame.Events;

namespace IsoGame.Screens
{
    /// <summary>
    /// The options screen is brought up over the top of the main menu
    /// screen, and gives the user a chance to configure the game
    /// in various hopefully useful ways.
    /// </summary>
    class OptionsMenuScreen : MenuScreen
    {
        readonly MenuEntry _ungulateMenuEntry;
        readonly MenuEntry _languageMenuEntry;
        readonly MenuEntry _frobnicateMenuEntry;
        readonly MenuEntry _elfMenuEntry;

        enum Ungulate
        {
            BactrianCamel,
            Dromedary,
            Llama,
        }

        static Ungulate _currentUngulate = Ungulate.Dromedary;

        static readonly string[] Languages = { "C#", "French", "Deoxyribonucleic acid" };
        static int _currentLanguage;

        static bool _frobnicate = true;

        static int _elf = 23;

        /// <summary>
        /// Constructor.
        /// </summary>
        public OptionsMenuScreen(EventManager eventManager)
            : base("Options", eventManager)
        {
            // Create our menu entries.
            _ungulateMenuEntry = new MenuEntry(string.Empty);
            _languageMenuEntry = new MenuEntry(string.Empty);
            _frobnicateMenuEntry = new MenuEntry(string.Empty);
            _elfMenuEntry = new MenuEntry(string.Empty);

            SetMenuEntryText();

            var back = new MenuEntry("Back");

            // Hook up menu event handlers.
            _ungulateMenuEntry.Selected += UngulateMenuEntrySelected;
            _languageMenuEntry.Selected += LanguageMenuEntrySelected;
            _frobnicateMenuEntry.Selected += FrobnicateMenuEntrySelected;
            _elfMenuEntry.Selected += ElfMenuEntrySelected;
            back.Selected += OnCancel;
            
            // Add entries to the menu.
            MenuEntries.Add(_ungulateMenuEntry);
            MenuEntries.Add(_languageMenuEntry);
            MenuEntries.Add(_frobnicateMenuEntry);
            MenuEntries.Add(_elfMenuEntry);
            MenuEntries.Add(back);
        }


        /// <summary>
        /// Fills in the latest values for the options screen menu text.
        /// </summary>
        void SetMenuEntryText()
        {
            _ungulateMenuEntry.Text = "Preferred ungulate: " + _currentUngulate;
            _languageMenuEntry.Text = "Language: " + Languages[_currentLanguage];
            _frobnicateMenuEntry.Text = "Frobnicate: " + (_frobnicate ? "on" : "off");
            _elfMenuEntry.Text = "elf: " + _elf;
            if (TransitionAlpha > 0.9999f)
                EventManager.QueueEvent(new GameEvent(EventType.MenuChange));
        }

        /// <summary>
        /// Event handler for when the Ungulate menu entry is selected.
        /// </summary>
        void UngulateMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            _currentUngulate++;

            if (_currentUngulate > Ungulate.Llama)
                _currentUngulate = Ungulate.BactrianCamel;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Language menu entry is selected.
        /// </summary>
        void LanguageMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            _currentLanguage = (_currentLanguage + 1) % Languages.Length;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Frobnicate menu entry is selected.
        /// </summary>
        void FrobnicateMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            _frobnicate = !_frobnicate;

            SetMenuEntryText();
        }


        /// <summary>
        /// Event handler for when the Elf menu entry is selected.
        /// </summary>
        void ElfMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            _elf++;

            SetMenuEntryText();
        }
    }
}
