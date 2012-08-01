using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using ZarTools;

namespace IsoGame.Processes
{
    public class AnimatedMoveProcess : IProcess
    {
        Unit unit;
        Isometry iso;
        CompassDirection[] path;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="iso"></param>
        /// <param name="path"></param>
        public AnimatedMoveProcess(Unit unit, Isometry iso, params CompassDirection[] path)
        {
            this.unit = unit;
            this.iso = iso;
            this.path = path;
        }

        /// <summary>
        /// Initialisation.
        /// </summary>
        public override void Initialize()
        {
            var anim = new AnimProcess(unit, AnimAction.Walk, true);
            ClientGame._processManager.ProcessList.Add(anim);

            MovementProcess lastMove = null;

            for( int i = 0; i < path.Length; i ++)
            {
                MovementProcess walk;
                if (i == path.Length - 1)
                {
                    walk = new MovementProcess(unit, path[i], anim, iso);
                    walk.Next = Next;
                }
                else
                    walk = new MovementProcess(unit, path[i], null, iso);

                if (lastMove != null)
                    lastMove.Next = walk;
                else
                    ClientGame._processManager.ProcessList.Add(walk);
                lastMove = walk;  
            }
            //  If we were previously idle - stop any existing animations
            ClientGame._processManager.ClearAnimations(unit);
            ClientGame._processManager.ProcessList.Remove(this);
        }

    }
}
