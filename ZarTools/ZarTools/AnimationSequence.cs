using System.Collections.Generic;

namespace ZarTools
{
    public class AnimationSequence
    {
        public string Name { get; private set; }
        public int[] Frames { get; private set; }
        public Dictionary<int, List<object>> Events { get; private set; }
        public short AnimCollection { get; private set; }

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
