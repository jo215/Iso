using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace ZarTools
{
    /// <summary>
    /// An individual sprite instance.
    /// </summary>
    public class ZSprite
    {
        static StringBuilder stringBuilder;

        BaseZarSprite baseSprite;
        string stance = "Stand";
        public string Stance { get { return stance; } set { stance = value; rebuildSequence(); } }
        string action = "Walk";
        public string Action { get { return action; } set { action = value; rebuildSequence(); } }
        string weapon = "SMG";
        public string Weapon { get { return weapon; } set { weapon = value; rebuildSequence(); } }
        string attack = "Burst";
        public string Attack { get { return attack; } set { attack = value; rebuildSequence(); } }
        short direction = 3;
        public short Direction { get { return direction; } set { direction = value; if (direction < 0) direction = 7; if (direction > 7) direction = 0; rebuildSequence(); } }

        private string sequence;
        private void rebuildSequence()
        {
            ticks = 0;
            overlay = false;
            sequence = buildSequenceName();
        }

        int ticks, overlayTicks;
        bool overlay;
        string overlayName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="fileName"></param>
        public ZSprite(GraphicsDevice device, String fileName)
        {
            if (stringBuilder == null)
                stringBuilder = new StringBuilder();
            baseSprite = BaseZarSprite.GetInstance(device, fileName);
            rebuildSequence();
        }

        /// <summary>
        /// Ensure the respective AnimationCollection is decoded. Therefore must be called before DrawCurrentImage()
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="vector2"></param>
        /// <param name="color"></param>
        public void GetCurrentImage()
        {
            short collection = baseSprite.Sequences[sequence].animCollection;
            if (baseSprite.Collections[collection].textures[0] == null)
            {
                baseSprite.readAnimation(collection);
                //  Overlay
                if (baseSprite.Sequences[sequence].events[ticks / 5].Contains("sproverlay"))
                {
                    overlayName = sequence + "Overlay";
                    short collection2 = baseSprite.Sequences[overlayName].animCollection;
                    if (baseSprite.Collections[collection2].textures[0] == null)
                        baseSprite.readAnimation(collection2);
                }
            }
        }

        /// <summary>
        /// Draws the correct image for this sprite's current animation sequence.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="position"></param>
        /// <param name="c"></param>
        public void DrawCurrentImage(SpriteBatch sb, Vector2 position, Color c)
        {
            
            short collection = baseSprite.Sequences[sequence].animCollection;

            int fCount = baseSprite.Collections[collection].frameCount;
            int dCount = baseSprite.Collections[collection].dirCount;
            //  Figure out total frames and current frame in sequence
            int totalFrames = baseSprite.Sequences[sequence].frames.Length;
            int currentFrame = baseSprite.Sequences[sequence].frames[ticks / 5] + fCount * direction;

            //  Add in frame position offsets
            Vector2 nuPosition = position;
            nuPosition.X += baseSprite.Collections[collection].frameRect[currentFrame].X;
            nuPosition.Y += baseSprite.Collections[collection].frameRect[currentFrame].Y;
            //  Subtract collection offset
            nuPosition.X -= baseSprite.Collections[0].collectionOffset.X;
            nuPosition.Y -= baseSprite.Collections[0].collectionOffset.Y;

            //  Draw
            sb.Draw(baseSprite.Collections[collection].textures[currentFrame], nuPosition, c);
            //  Overlay begins
            if (baseSprite.Sequences[sequence].events[ticks / 5].Contains("sproverlay"))
            {
                overlay = true;
                overlayTicks = ticks;
                overlayName = sequence + "Overlay";
            }
            //  Take care of overlay
            if (overlay)
            {
                short collection2 = baseSprite.Sequences[overlayName].animCollection;

                Vector2 nuPosition2 = position;
                fCount = baseSprite.Collections[collection2].frameCount;
                dCount = baseSprite.Collections[collection2].dirCount;
                //  Figure out current frame in sequence
                if ((ticks - overlayTicks) / 5 < baseSprite.Sequences[overlayName].frames.Length)
                {

                    currentFrame = baseSprite.Sequences[overlayName].frames[(ticks - overlayTicks) / 5] + fCount * direction;

                    //  Add in frame position offsets
                    nuPosition2.X += baseSprite.Collections[collection2].frameRect[currentFrame].X;
                    nuPosition2.Y += baseSprite.Collections[collection2].frameRect[currentFrame].Y;
                    //  Subtract collection offset
                    nuPosition2.X -= baseSprite.Collections[0].collectionOffset.X;
                    nuPosition2.Y -= baseSprite.Collections[0].collectionOffset.Y;
                    //  Draw
                    sb.Draw(baseSprite.Collections[collection2].textures[currentFrame], nuPosition2, c);
                }
            }

            //  End of sequence actions
            ticks += 1;
            if (ticks >= totalFrames * 5)
            {
                switch (action)
                {
                    case "Walk":
                        action = "Walk";
                        break;
                    case "Fallback":
                        action = "Getupback";
                        break;
                    case "Fallforward":
                        action = "Getupforward";
                        break;
                    case "Crouch":
                        action = "Breathe";
                        stance = "Crouch";
                        break;
                    case "Prone":
                        action = "Breathe";
                        stance = "Prone";
                        break;
                    case "Stand":
                        action = "Breathe";
                        stance = "Stand";
                        break;
                    default:
                        action = "Breathe";
                        break;
                }
                if (stance.Equals("Death"))
                {
                    stance = "Stand";
                    action = "Breathe";
                }
                rebuildSequence();
            }
        }

        //  Helper to build the correct name for the animation sequence to display.
        private string buildSequenceName()
        {
            //  Build the sequence name
            stringBuilder.Clear();
            stringBuilder.Append(stance);
            stringBuilder.Append(action);
            switch (stance)
            {
                case "Death":
                    break;
                case "Stand":
                    if (action.Equals("Attack") || action.Equals("Breathe") || action.Equals("Walk"))
                        stringBuilder.Append(weapon);
                    break;
                default:
                    if (action.Equals("Attack") || action.Equals("Breathe"))
                        stringBuilder.Append(weapon);
                    break;
            }
            if (action.Equals("Attack"))
                stringBuilder.Append(attack);

            String sequence = stringBuilder.ToString();

            return sequence;
        }
    }
}
