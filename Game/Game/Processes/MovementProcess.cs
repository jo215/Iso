using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Microsoft.Xna.Framework;

namespace IsoGame.Processes
{
    public class MovementProcess : IProcess
    {
        Unit _unit;
        CompassDirection _direction;
        AnimProcess _linkedAnimProcess;
        float _speed;
        Isometry _iso;
        float ticks, x, y;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="direction"></param>
        /// <param name="linkedAnimProcess"></param>
        /// <param name="delay"></param>
        public MovementProcess(Unit unit, CompassDirection direction, AnimProcess linkedAnimProcess, Isometry iso, float delay = .015f)
            : base()
        {
            _unit = unit;
            _direction = direction;
            _linkedAnimProcess = linkedAnimProcess;
            _speed = delay;
            _iso = iso;
        }

        /// <summary>
        /// Initialises this Movement.
        /// </summary>
        public override void Initialize()
        {
            _unit.Facing = _direction;
        }

        /// <summary>
        /// Updates the Movement.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            ticks+= .01667f;
                if (ticks >= _speed)
                {
                    updateMovementOffset();
                    ticks = 0;
                }
            killIfFinished();
        }

        /**
         * Checks if move complete and kills the process if so.
         */
        private void killIfFinished()
        {
            switch (_direction)
            {
                case CompassDirection.East: if (_unit.Sprite.AnimXOffset >= 72) Kill(); break;
                case CompassDirection.West: if (_unit.Sprite.AnimXOffset <= -72) Kill(); break;
                case CompassDirection.North: if (_unit.Sprite.AnimYOffset <= -36) Kill(); break;
                case CompassDirection.South: if (_unit.Sprite.AnimYOffset >= 36) Kill(); break;
                case CompassDirection.SouthWest: if (_unit.Sprite.AnimXOffset <= -36) Kill(); break;
                case CompassDirection.SouthEast: if (_unit.Sprite.AnimXOffset >= 36) Kill(); break;
                case CompassDirection.NorthEast: if (_unit.Sprite.AnimXOffset >= 36) Kill(); break;
                case CompassDirection.NorthWest: if (_unit.Sprite.AnimXOffset <= -36) Kill(); break;
            }
            return;
        }

        /**
         * Kills this process and any animation process linked to it.
         */
        public override void Kill()
        {
            System.Drawing.Point nP = _iso.TileWalker(new System.Drawing.Point(_unit.X, _unit.Y), _direction);
            _unit.X = (short)nP.X;
            _unit.Y = (short)nP.Y;

            _unit.Sprite.AnimXOffset = 0;
            _unit.Sprite.AnimYOffset = 0;

            if (_linkedAnimProcess != null)
            {
                _linkedAnimProcess.Kill();
            }
            base.Kill();
        }

        /**
        * Updates the location of this actor.
        */
        private void updateMovementOffset()
        {
            switch (_direction)
            {
                case CompassDirection.North:
                    y-=2;
                    break;
                case CompassDirection.NorthWest:
                    x-=2;
                    y --;
                    break;
                case CompassDirection.West:
                    x-=2;
                    break;
                case CompassDirection.SouthWest:
                    x-=2;
                    y ++;
                    break;
                case CompassDirection.South:
                    y+=2;
                    break;
                case CompassDirection.SouthEast:
                    x +=2;
                    y ++;
                    break;
                case CompassDirection.East:
                    x+=2;
                    break;
                case CompassDirection.NorthEast:
                    x+=2;
                    y --;
                    break;
            }
            _unit.Sprite.AnimXOffset = (int)x / 2;
            _unit.Sprite.AnimYOffset = (int)y / 2;
        }

    }
}
