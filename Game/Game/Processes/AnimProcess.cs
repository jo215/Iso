using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ZarTools;
using IsoGame.Events;
using IsoTools;

namespace IsoGame.Processes
{
    public class AnimProcess : IProcess
    {
        static StringBuilder _stringBuilder = new StringBuilder();

        public Unit Unit { get; set; }
        public ZSprite Sprite { get; set; }
        public AnimAction Action { get; set; }
        public bool IsRepeating { get; set; }
        public int ShowPeriod { get; set; }

        string sequence;
        int TotalFrames;
        int CurrentFrame;
        int period;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="isRepeating"></param>
        /// <param name="showPeriod"></param>
        public AnimProcess(Unit unit, AnimAction action, bool isRepeating = false, int showPeriod = 7)
            : base()
        {
            Unit = unit;
            Sprite = unit.Sprite;
            Action = action;
            IsRepeating = isRepeating;
            ShowPeriod = showPeriod;
        }

        /// <summary>
        /// Initialises this Animation.
        /// </summary>
        public override void Initialize()
        {
            //  If we're not just standing around we're busy
            if (Action != AnimAction.None && Action != AnimAction.Breathe)
                ClientGame._eventManager.QueueEvent(new GameEvent(EventType.CharacterBusy, Unit.ID));

            //  If we were previously idle - stop any existing animations


            //  Get the correct sequence
            sequence = GetSequenceName();
            DecodeAnimation(sequence);
            Sprite.CurrentSequence = sequence;

            //  #### Account for direction here!
            int frameCount = Sprite.Collections[Sprite.Sequences[sequence].AnimCollection].FrameCount;

            //  Figure out total frames and current frame in sequence
            CurrentFrame = 0;
            TotalFrames = Sprite.Sequences[sequence].Frames.Length;
            Sprite.CurrentFrameInCollection = Sprite.Sequences[sequence].Frames[CurrentFrame] + frameCount * (int)Unit.Facing;
            Sprite.CurrentFrameInSequence = 0;
            Sprite.Direction = (int)Unit.Facing;
        }

        /// <summary>
        /// Ensures the correct images are decoded.
        /// </summary>
        private void DecodeAnimation(string sequence)
        {
            var collection = Sprite.Sequences[sequence].AnimCollection;
            if (Sprite.Collections[collection].Textures[0] != null) return;
            Sprite._baseSprite.ReadAnimation(collection);

            //  Overlay
            bool overlay = false;
            foreach (List<object> i in Sprite.Sequences[sequence].Events.Values)
            {
                if (i.Contains("sproverlay"))
                {
                    overlay = true;
                    break;
                }
            }
            if (overlay == false) return;

            string overlayName = sequence + "Overlay";
            var collection2 = Sprite.Sequences[overlayName].AnimCollection;
            if (Sprite.Collections[collection2].Textures[0] != null) return;
            Sprite._baseSprite.ReadAnimation(collection2);
        }

        /// <summary>
        /// Returns the correct Sequence name for the given Action, taking into account the Unit's Stance & Weapon.
        /// </summary>
        /// <returns></returns>
        private string GetSequenceName()
        {
            _stringBuilder.Clear();

            //  Death animations are slightly different
            if (IsDeathAction(Action))
            {
                _stringBuilder.Append(Enum.GetName(typeof(AnimAction),Action));
                return _stringBuilder.ToString();
            }

            //  Stance
            _stringBuilder.Append(Enum.GetName(typeof(Stance), Unit.Stance));

            //  Action
            if (IsAttackAction(Action))
            {
                //  Attack Actions
                _stringBuilder.Append("Attack");
                if (Unit.Weapon != WeaponType.None)
                {
                    _stringBuilder.Append(Enum.GetName(typeof(WeaponType), Unit.Weapon));
                }
                _stringBuilder.Append(Enum.GetName(typeof(AnimAction), Action));
                return _stringBuilder.ToString();
            }
            else if (Action != AnimAction.None)
            {
                //  Other Actions
                _stringBuilder.Append(Enum.GetName(typeof(AnimAction), Action));
            }

            //  Possible Weapon
            if (Sprite.Sequences.ContainsKey(_stringBuilder.ToString() + Enum.GetName(typeof(WeaponType), Unit.Weapon)))
                _stringBuilder.Append(Enum.GetName(typeof(WeaponType), Unit.Weapon));
            
            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Updates the animation.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            period++;
            if (period == ShowPeriod)
            {
                CurrentFrame++;
                if (CurrentFrame != TotalFrames)
                {
                    period = 0;
                    int frameCount = Sprite.Collections[Sprite.Sequences[sequence].AnimCollection].FrameCount;
                    Sprite.CurrentFrameInCollection = Sprite.Sequences[sequence].Frames[CurrentFrame] + frameCount * (int)Unit.Facing;
                    Sprite.CurrentFrameInSequence = CurrentFrame;
                }
                else
                {
                    if (IsRepeating)
                        CurrentFrame = 0;
                    else
                        Kill();
                }
            }
        }

        /// <summary>
        /// Kills off this animation.
        /// </summary>
        public override void Kill()
        {
            base.Kill();

            //  First change stance if necessary.
            if (Action == AnimAction.Prone)
                Unit.Stance = Stance.Prone;
            else if (Action == AnimAction.Stand)
                Unit.Stance = Stance.Stand;
            else if (Action == AnimAction.Crouch)
                Unit.Stance = Stance.Crouch;

            if (Next == null)
            {
                if (!IsDeathAction(Action))
                {
                    //  If this is the end of all queued animations for this character we set to breathe
                    Next = new AnimProcess(Unit, AnimAction.Breathe, true);
                    ClientGame._eventManager.QueueEvent(new GameEvent(EventType.CharacterAvailable, Unit.ID));
                }
            }
        }

        /// <summary>
        /// Determines if the given Action results in death.
        /// </summary>
        /// <param name="Action"></param>
        /// <returns></returns>
        private bool IsDeathAction(AnimAction Action)
        {
            if (Action == AnimAction.Death || Action == AnimAction.DeathBighole || Action == AnimAction.DeathCutinhalf
                || Action == AnimAction.DeathElectrify || Action == AnimAction.DeathExplode || Action == AnimAction.DeathFire
                || Action == AnimAction.DeathMelt || Action == AnimAction.DeathRiddled)
                return true;
            return false;
        }

        /// <summary>
        /// Determines if the given action is an Attack.
        /// </summary>
        /// <param name="Action"></param>
        /// <returns></returns>
        private bool IsAttackAction(AnimAction Action)
        {
            if (Action == AnimAction.Burst || Action == AnimAction.Single || Action == AnimAction.Slash || Action == AnimAction.Swing
            || Action == AnimAction.Throw || Action == AnimAction.Thrust || Action == AnimAction.UnarmedOne || Action == AnimAction.UnarmedTwo)
                return true;
            return false;
        }
    }
}
