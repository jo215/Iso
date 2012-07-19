using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace IsoGame.Events
{
    /// <summary>
    /// Matches events with their listeners.
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class EventManager : GameComponent
    {
        //	A set of unique event types the manager can handle  
        public HashSet<EventType> EventTypes { get; private set; }
        //	Set of unique listeners
        public HashSet<IEventListener> Listeners { get; private set; }
        //	Mapping of event types to a set of listeners
        public Dictionary<EventType, HashSet<IEventListener>> Registry { get; private set; }
        //	Double list of queued events ready to be distribute to listeners
        public List<GameEvent>[] EventQueue {get; private set;}
        private int _activeQueue;
        //  CPU budget
        public float MaxSeconds { get; set; }
        //  Event keymappings
        private readonly Keymap _keymap;
        public Dictionary<Keys, EventType> Map { get { return _keymap.Map; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        public EventManager(Game game)
            : base(game)
        {
            EventTypes = new HashSet<EventType>();
            Listeners = new HashSet<IEventListener>();
            Registry = new Dictionary<EventType, HashSet<IEventListener>>();
            EventQueue = new List<GameEvent>[2];
            EventQueue[0] = new List<GameEvent>();
            EventQueue[1] = new List<GameEvent>();
            _activeQueue = 0;
            MaxSeconds = 0.16f;
            _keymap = new Keymap();
        }

        /// <summary>
        /// Processes queued messages.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            //  Calculate how long to process events for
            // ReSharper disable PossibleLossOfFraction
            double previousTime = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
            // ReSharper restore PossibleLossOfFraction
            //  Swap active queues, emptying new queue after switch
            var queueToProcess = _activeQueue;
            _activeQueue = (_activeQueue + 1) % 2;
            EventQueue[_activeQueue].Clear();
            //  Process as many events as possible in alloted time
            while (EventQueue[queueToProcess].Count > 0)
            {
                var ev = EventQueue[queueToProcess][0];
                EventQueue[queueToProcess].RemoveAt(0);
                //  Send event to all wildcard listeners
                if (Registry[EventType.AllEvents] != null)
                    foreach (var listener in Registry[EventType.AllEvents])
                        listener.HandleEvent(ev);
                //  Send to all other registered listeners
                if (Registry.ContainsKey(ev.EventType))
                {
                    foreach (var listener in Registry[ev.EventType])
                    {
                        listener.HandleEvent(ev);
                    }
                }
                //  Abort if alloted time ran out
                // ReSharper disable PossibleLossOfFraction
                double currentTime = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                // ReSharper restore PossibleLossOfFraction
                if (currentTime - previousTime >= MaxSeconds)
                    break;
            }
            //  If any events left to process, push onto active queue
            if (EventQueue[queueToProcess].Count > 0)
                while (EventQueue[queueToProcess].Count > 0)
                {
                    var ev = EventQueue[queueToProcess][0];
                    EventQueue[queueToProcess].RemoveAt(0);
                    EventQueue[_activeQueue].Add(ev);
                }
            base.Update(gameTime);
        }

        /// <summary>
        /// Removes an event from the queue.
        /// </summary>
        /// <param name="ev">the event to abort</param>
        /// <param name="allEvents">true if all events of this type should be removed</param>
        /// <returns>true if event found and removed, false otherwise</returns>
        public bool AbortEvent(GameEvent ev, bool allEvents)
        {
            //  Skip if no listeners for this event
            if (!Registry.ContainsKey(ev.EventType))
                return false;
            //  Iterate thru queue searching for the event
            var removed = false;
            foreach (var gev in EventQueue[_activeQueue].ToArray().Where(gev => gev.EventType == ev.EventType))
            {
                EventQueue[_activeQueue].Remove(gev);
                removed = true;
                if (!allEvents)
                    break;
            }
            return removed;
        }

        /// <summary>
        /// Adds a new Game Event to the queue.
        /// </summary>
        /// <param name="ev">the GameEvent to add</param>
        /// <returns>true if added OK, false otherwise</returns>
        public bool QueueEvent(GameEvent ev)
        {
            //  Check there is a listener associated with this event
            if (!Registry.ContainsKey(ev.EventType))
                //  If no wildcard listener then abort adding to queue
                if (!Registry.ContainsKey(EventType.AllEvents))
                    return false;
            EventQueue[_activeQueue].Add(ev);
            return true;
        }

        /// <summary>
        /// Registers a new listener for a specific Game Event type.
        /// </summary>
        /// <param name="listener"> the listener to add</param>
        /// <param name="type"> </param>
        /// <returns>true if successful, false otherwise</returns>
        public bool AddEventListener(IEventListener listener, EventType type)
        {
            //  Update the event type list if necessary
            EventTypes.Add(type);
            //  Create registry mapping if none already
            if (!Registry.ContainsKey(type))
                Registry.Add(type, new HashSet<IEventListener>());
            //  Add listener to map if not already there
            Registry[type].Add(listener);
            //  Update listener list
            Listeners.Add(listener);
            return true;
        }

        /// <summary>
        /// Removes a listener/event pairing from the registry.
        /// </summary>
        /// <param name="listener">the listener to effect</param>
        /// <param name="type"> </param>
        /// <returns>true if successful, false otherwise</returns>
        public bool RemoveEventListener(IEventListener listener, EventType type)
        {
            return (Registry[type].Remove(listener));
        }
    }
}
