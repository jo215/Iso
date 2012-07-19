namespace IsoGame.Events
{
    /// <summary>
    /// Any class wishing to listen to game events should implement this interface.
    /// </summary>
    public interface IEventListener
    {
        /// <summary>
        /// Returns the name of the listener.
        /// </summary>
        /// <returns>the string name of the listener.</returns>
        string GetListenerName();

        /// <summary>
        /// Handles game events.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns>true to indicate event consumed, false to indicate event not consumed.</returns>
        bool HandleEvent(GameEvent ev);

        /// <summary>
        /// Registers events this listener is interested in.
        /// </summary>
        void RegisterListeners();
    }
}
