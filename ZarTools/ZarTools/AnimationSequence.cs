using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZarTools
{
    class AnimationSequence
    {
        internal string name { get; private set; }
        internal int[] frames { get; private set; }
        internal Dictionary<int, List<object>> events { get; private set; }
        internal short animCollection { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="framesArray"></param>
        /// <param name="events"></param>
        /// <param name="collection"></param>
        public AnimationSequence(string name, int[] framesArray, Dictionary<int, List<object>> events, short collection)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.frames = framesArray;
            this.events = events;
            this.animCollection = collection;
        }
    }
}
