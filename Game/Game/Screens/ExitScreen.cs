using IsoGame.Screens.Base;
using IsoGame.Events;

namespace IsoGame.Screens
{
    /// <summary>
    /// The confirm you want to quit screen.
    /// </summary>
    class ExitScreen : MenuScreen
    {
        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public ExitScreen(EventManager eventManager)
            : base("Are you sure?", eventManager)
        {
            // Create our menu entries.
            var okMenuEntry = new MenuEntry("OK");
            var cancelMenuEntry = new MenuEntry("Cancel");

            // Hook up menu event handlers.
            okMenuEntry.Selected += OKSelected;
            cancelMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(okMenuEntry);
            MenuEntries.Add(cancelMenuEntry);

        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void OKSelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.Game.Exit();

        }

    }
}
