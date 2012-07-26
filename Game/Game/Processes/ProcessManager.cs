using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Editor.Model;


namespace IsoGame.Processes
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class ProcessManager : Microsoft.Xna.Framework.GameComponent
    {
        public List<IProcess> ProcessList;

        public ProcessManager(Game game)
            : base(game)
        {
            // TODO: Construct any child components here
            ProcessList = new List<IProcess>();
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Returns true if the manager has an active process.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool isProcessActive() {
		foreach (IProcess p in ProcessList) {
			if ( p.Active ) {
				return true;
			}
		}
		return false;
	}

        /// <summary>
        /// Allows the game component to update itself.
        /// Updates all active processes.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            foreach (IProcess p in ProcessList.ToArray())
            {
                if (p.Dead)
                {
                    IProcess next = p.Next;
                    ProcessList.Remove(p);
                    if (next != null)
                    {
                        p.Next = null;
                        ProcessList.Add(next);
                    }
                }
                else if (p.Active && !p.Paused)
                {
                    p.Update(gameTime);
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Immediately clears all animation processes belonging to the given unit.
        /// </summary>
        /// <param name="Unit"></param>
        internal void ClearAnimations(Unit Unit)
        {
            foreach (IProcess p in ProcessList)
            {
                AnimProcess ap = p as AnimProcess;
                if (ap != null && ap.Unit == Unit)
                {
                    ap.Kill();
                    ap.Next = null;
                }
            }
        }
    }
}
