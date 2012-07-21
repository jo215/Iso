using System.Collections.Generic;

namespace ZarTools
{
    class AnimationSequence
    {
        internal string Name { get; private set; }
        internal int[] Frames { get; private set; }
        internal Dictionary<int, List<object>> Events { get; private set; }
        internal short AnimCollection { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="framesArray"></param>
        /// <param name="events"></param>
        /// <param name="collection"></param>
        public AnimationSequence(string name, int[] framesArray, Dictionary<int, List<object>> events, short collection)
        {
            Name = name;
            Frames = framesArray;
            Events = events;
            AnimCollection = collection;
        }
    }
}
