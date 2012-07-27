using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ZarTools
{
    /// <summary>
    /// An individual sprite instance.
    /// </summary>
    public class ZSprite
    {
        public readonly BaseZarSprite _baseSprite;

        public Dictionary<string, AnimationSequence> Sequences { get { return _baseSprite.Sequences; } private set { } }
        public AnimationCollection[] Collections { get { return _baseSprite.Collections; } private set { } }

        public string CurrentSequence { get; set; }
        public int CurrentFrameInSequence { get; set; }
        public int CurrentFrameInCollection { get; set; }
        public int Direction { get; set; }
        Rectangle CurrentPickRect;

        bool _overlay;
        string _overlayName;
        int overlayFrame, overlayEnd, lastFrameInSequence;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="fileName"></param>
        public ZSprite(GraphicsDevice device, String fileName)
        {
            _baseSprite = BaseZarSprite.GetInstance(device, fileName);

        }

        /// <summary>
        /// Saves out a 1-image reprsentation of this sprite.
        /// </summary>
        public void SavePreviewImages()
        {
            if (_baseSprite.FileName.Contains("DeathClaw") || _baseSprite.FileName.Contains("Treadmill")
                || _baseSprite.FileName.Contains("MutantFreak") || _baseSprite.FileName.Contains("Dummy"))
                return;
            Console.WriteLine(_baseSprite.FileName);

            var pics = new string[] { "Stand", "StandClub", "StandHeavy", "StandKnife", "StandMinigun", "StandPistol",
                                        "StandRifle", "StandRocket", "StandSMG", "StandSpear"};
            foreach (var q in pics)
            {
                AnimationSequence seq;
                if (_baseSprite.Sequences.TryGetValue(q, out seq))
                {
                    var collection = _baseSprite.Sequences[q].AnimCollection;

                    _baseSprite.ReadAnimation(collection);
                    var i = _baseSprite.Collections[collection].FrameCount * 4;
                    var width = _baseSprite.Collections[collection].Textures[i].Width;
                    var height = _baseSprite.Collections[collection].Textures[i].Height;

                    var s = _baseSprite.FileName.Substring(41, _baseSprite.FileName.Length - 45);
                    var fileName = "D:\\temp\\" + s + q + ".png";

                    _baseSprite.Collections[collection].Textures[i].SaveAsPng(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write), width, height);
                }
            }
        }

        /// <summary>
        /// Returns true if the passed in screen co-ordinates are over the sprite's current position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool HitTest(int x, int y)
        {
            if (CurrentPickRect.Contains(new Point(x, y)))
                return true;
            return false;
        }

        /// <summary>
        /// Draws the correct image for this sprite's current animation sequence.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="position"></param>
        /// <param name="c"></param>
        public void DrawCurrentImage(SpriteBatch sb, Vector2 position, Color c)
        {
            var collection = _baseSprite.Sequences[CurrentSequence].AnimCollection;

            //  Add in frame position offsets
            var nuPosition = position;
            nuPosition.X += _baseSprite.Collections[collection].FrameRect[CurrentFrameInCollection].X;
            nuPosition.Y += _baseSprite.Collections[collection].FrameRect[CurrentFrameInCollection].Y;

            nuPosition.X -= _baseSprite.Center.X - 36;
            nuPosition.Y -= _baseSprite.Center.Y - 30;
            
            //  Update picking rectangle
            CurrentPickRect = new Rectangle((int)nuPosition.X, (int)nuPosition.Y, _baseSprite.Collections[collection].Textures[CurrentFrameInCollection].Width, _baseSprite.Collections[collection].Textures[CurrentFrameInCollection].Height);

            //  Draw
            sb.Draw(_baseSprite.Collections[collection].Textures[CurrentFrameInCollection], nuPosition, c);

            //  Overlay begins

            if (_baseSprite.Sequences[CurrentSequence].Events.ContainsKey(CurrentFrameInSequence)
                && _baseSprite.Sequences[CurrentSequence].Events[CurrentFrameInSequence].Contains("sproverlay"))
            {
                _overlay = true;
                overlayFrame = 0;
                lastFrameInSequence = CurrentFrameInSequence;
                _overlayName = CurrentSequence + "Overlay";
                overlayEnd = overlayFrame + _baseSprite.Sequences[_overlayName].Frames.Length;
                
            }
            //  Take care of overlay
            if (_overlay)
            {
                //  Figure out current frame in sequence
                if (overlayFrame < overlayEnd)
                {
                    var collection2 = _baseSprite.Sequences[_overlayName].AnimCollection;
                    var nuPosition2 = position;
                    var overlayCollectionFrame = overlayFrame + (Direction * _baseSprite.Sequences[_overlayName].Frames.Length);
                    //  Add in frame position offsets
                    nuPosition2.X += _baseSprite.Collections[collection2].FrameRect[overlayCollectionFrame].X;
                    nuPosition2.Y += _baseSprite.Collections[collection2].FrameRect[overlayCollectionFrame].Y;

                    nuPosition2.X -= _baseSprite.Center.X - 36;
                    nuPosition2.Y -= _baseSprite.Center.Y - 30;

                    sb.Draw(_baseSprite.Collections[collection2].Textures[overlayCollectionFrame], nuPosition2, c);
                    
                    if (lastFrameInSequence != CurrentFrameInSequence)
                        overlayFrame++;

                    lastFrameInSequence = CurrentFrameInSequence;
                }
                else
                {
                    _overlay = false;
                }
            }
        }
    }
}
