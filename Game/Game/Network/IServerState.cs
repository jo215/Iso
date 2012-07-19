namespace IsoGame.Network
{
    public interface IServerState
    {
        /// <summary>
        /// The actions to take each loop whilst in this state.
        /// </summary>
        void Execute();
    }
}
