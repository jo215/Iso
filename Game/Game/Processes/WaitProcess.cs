using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IsoGame.Processes
{
    public class WaitProcess : IProcess
    {
        int totalMillis;
        double currentMillis;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="millis"></param>
        public WaitProcess(int millis)
        {
            totalMillis = millis;
            currentMillis = 0;
        }

        /// <summary>
        /// Updates the wait process.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
            //  Assume 60fps
            currentMillis += (17);
            if (currentMillis > totalMillis)
                Kill();
        }

        /// <summary>
        /// Initialisation.
        /// </summary>
        public override void Initialize()
        {
            //  Do nothing
        }
    }
}
