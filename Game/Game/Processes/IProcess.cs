using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace IsoGame.Processes
{
    abstract public class IProcess
    {
        public bool Dead { get; set; }
        public bool Active { get; set; }
        public bool Paused { get; set; }
        public bool Initialized { get; set; }
        public IProcess Next { get; set; }
	
	    /**
	     * Constructor
	     */
	    public IProcess() {
		    Dead = false;
		    Active = true;
		    Paused = false;
		    Next = null;
		    Initialized = false;
	    }
	
	    /**
	     * Called once every frame
	     * @param deltaMilliseconds
	     */
	    public virtual void Update(GameTime gameTime) {
		    if (!Initialized) {
			    Initialize();
			    Initialized = true;
		    }
	    }

        /// <summary>
        /// Called when the process is over.
        /// </summary>
        public virtual void Kill()
        {
            Dead = true;
        }
	
	    /**
	     * Any code to run on process first becoming active.
	     */
	    public abstract void Initialize();
	
        }
}
