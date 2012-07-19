using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using IsoGame.Events;
using System.Threading;
using System.IO;


namespace IsoGame.Audio
{
    /// <summary>
    /// This class manages game audio in a separate thread.
    /// </summary>
    public class AudioManager : IEventListener
    {
        readonly Game _game;
        readonly Dictionary<EventType, SoundEffect> _baseSounds;
        readonly SoundEffectInstance[] _soundInstances;
        int _marker;
        readonly ConcurrentQueue<GameEvent> _eventQueue;
        public volatile bool IsRunning;
        bool _initialised;
        private const string ContentFolder = "Sounds";
        readonly EventManager _eventManager;
        GameEvent _tempEvent;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="eventManager"> </param>
        public AudioManager(Game game, EventManager eventManager)
        {
            _game = game;
            _eventManager = eventManager;
            RegisterListeners();
            _baseSounds = new Dictionary<EventType, SoundEffect>();
            _soundInstances = new SoundEffectInstance[20];
            _eventQueue = new ConcurrentQueue<GameEvent>();
            _initialised = false;
            _marker = 0;
            var t = new Thread(Run);
            t.Start();
        }

        /// <summary>
        /// Runs the thread.
        /// </summary>
        private void Run()
        {
            if (!_initialised)
                Initialise();
            
            while (IsRunning)
                if (_eventQueue.TryDequeue(out _tempEvent))
                    PlaySound(_tempEvent);
                else
                    Thread.Yield();
        }

        /// <summary>
        /// Initialises the audio manager.
        /// </summary>
        private void Initialise()
        {
            LoadFilesInFolder(new DirectoryInfo(_game.Content.RootDirectory + "/" + ContentFolder));
            _initialised = true;
            IsRunning = true;
        }

        /// <summary>
        /// Recursively loads all files in given folder.
        /// </summary>
        /// <param name="folder"></param>
        private void LoadFilesInFolder(DirectoryInfo folder)
        {
            foreach (var file in folder.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var effect = _game.Content.Load<SoundEffect>(ContentFolder + "/" + folder.Name + "/" + fileName);
                if (fileName != null && !_baseSounds.ContainsKey((EventType)Enum.Parse(typeof(EventType), fileName)))
                {
                    _baseSounds.Add((EventType)Enum.Parse(typeof(EventType), fileName), effect);
                }
            }
            foreach (var subFolder in folder.GetDirectories())
                LoadFilesInFolder(subFolder);
        }

        /// <summary>
        /// Plays the sound associated with an event.
        /// </summary>
        /// <param name="evnt"></param>
        private void PlaySound(GameEvent evnt)
        {
            SoundEffect basefx;
            if (!_baseSounds.TryGetValue(evnt.EventType, out basefx))
                return;

            _soundInstances[_marker] = basefx.CreateInstance();
            _soundInstances[_marker].Play();
            _marker++;
            if (_marker == _soundInstances.Length)
                _marker = 0;
        }

        /// <summary>
        /// Returns the name of this event listener.
        /// </summary>
        /// <returns></returns>
        public string GetListenerName()
        {
            return "Threaded Audio Manager";
        }

        /// <summary>
        /// Handles events.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        public bool HandleEvent(GameEvent ev)
        {
            _eventQueue.Enqueue(ev);
            return false;
        }

        /// <summary>
        /// Registers the events this listener is interested in.
        /// </summary>
        public void RegisterListeners()
        {
            _eventManager.AddEventListener(this, EventType.AllEvents);
        }
    }
}
