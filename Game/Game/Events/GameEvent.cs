using Microsoft.Xna.Framework;

namespace IsoGame.Events
{
    /// <summary>
    /// A game event and its data.
    /// </summary>
    public struct GameEvent
    {
        public EventType EventType;
        public int GameObjectID;
        public float FloatAmount;
        public Vector3 Position;

        /// <summary>
        /// Constructors follow.
        /// </summary>

        public GameEvent(EventType type)
        {
            EventType = type;
            GameObjectID = -1;
            FloatAmount = 0;
            Position = Vector3.Zero;
        }

        public GameEvent(EventType type, float amount)
        {
            EventType = type;
            GameObjectID = -1;
            FloatAmount = amount;
            Position = Vector3.Zero;
        }

        public GameEvent(EventType type, int sourceObjectID)
        {
            EventType = type;
            GameObjectID = sourceObjectID;
            FloatAmount = 0;
            Position = Vector3.Zero;
        }

        public GameEvent(EventType type, int sourceObjectID, float amount)
        {
            EventType = type;
            GameObjectID = sourceObjectID;
            FloatAmount = amount;
            Position = Vector3.Zero;
        }

        public GameEvent(EventType type, Vector3 pos)
        {
            EventType = type;
            GameObjectID = 0;
            FloatAmount = 0;
            Position = pos;
        }
    }
}
