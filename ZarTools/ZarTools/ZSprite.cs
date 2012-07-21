using System;
using System.IO;
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
        static StringBuilder _stringBuilder;

        readonly BaseZarSprite _baseSprite;
        string _stance = "Stand";
        public string Stance { get { return _stance; } set { _stance = value; RebuildSequence(); } }
        string _action = "Walk";
        public string Action { get { return _action; } set { _action = value; RebuildSequence(); } }
        string _weapon = "SMG";
        public string Weapon { get { return _weapon; } set { _weapon = value; RebuildSequence(); } }
        string _attack = "Burst";
        public string Attack { get { return _attack; } set { _attack = value; RebuildSequence(); } }
        short _direction = 3;
        public short Direction { get { return _direction; } set { _direction = value; if (_direction < 0) _direction = 7; if (_direction > 7) _direction = 0; RebuildSequence(); } }

        private string _sequence;
        private void RebuildSequence()
        {
            _ticks = 0;
            _overlay = false;
            _sequence = BuildSequenceName();
        }

        int _ticks, _overlayTicks;
        bool _overlay;
        string _overlayName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="fileName"></param>
        public ZSprite(GraphicsDevice device, String fileName)
        {
            if (_stringBuilder == null)
                _stringBuilder = new StringBuilder();
            _baseSprite = BaseZarSprite.GetInstance(device, fileName);
            RebuildSequence();
        }

        /// <summary>
        /// Ensure the respective AnimationCollection is decoded. Therefore must be called before DrawCurrentImage()
        /// </summary>
        public void GetCurrentImage()
        {
            var collection = _baseSprite.Sequences[_sequence].AnimCollection;
            if (_baseSprite.Collections[collection].Textures[0] != null) return;
            _baseSprite.ReadAnimation(collection);
            //  Overlay
            if (!_baseSprite.Sequences[_sequence].Events[_ticks/5].Contains("sproverlay")) return;
            _overlayName = _sequence + "Overlay";
            var collection2 = _baseSprite.Sequences[_overlayName].AnimCollection;
            if (_baseSprite.Collections[collection2].Textures[0] == null)
                _baseSprite.ReadAnimation(collection2);
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
        /// Draws the correct image for this sprite's current animation sequence.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="position"></param>
        /// <param name="c"></param>
        public void DrawCurrentImage(SpriteBatch sb, Vector2 position, Color c)
        {
            
            var collection = _baseSprite.Sequences[_sequence].AnimCollection;

            var fCount = _baseSprite.Collections[collection].FrameCount;

            //  Figure out total frames and current frame in sequence
            var totalFrames = _baseSprite.Sequences[_sequence].Frames.Length;
            var currentFrame = _baseSprite.Sequences[_sequence].Frames[_ticks / 5] + fCount * _direction;

            //  Add in frame position offsets
            var nuPosition = position;
            nuPosition.X += _baseSprite.Collections[collection].FrameRect[currentFrame].X;
            nuPosition.Y += _baseSprite.Collections[collection].FrameRect[currentFrame].Y;
            //  Subtract collection offset
            nuPosition.X -= _baseSprite.Collections[0].CollectionOffset.X;
            nuPosition.Y -= _baseSprite.Collections[0].CollectionOffset.Y;

            //  Draw
            sb.Draw(_baseSprite.Collections[collection].Textures[currentFrame], nuPosition, c);
            //  Overlay begins
            if (_baseSprite.Sequences[_sequence].Events[_ticks / 5].Contains("sproverlay"))
            {
                _overlay = true;
                _overlayTicks = _ticks;
                _overlayName = _sequence + "Overlay";
            }
            //  Take care of overlay
            if (_overlay)
            {
                var collection2 = _baseSprite.Sequences[_overlayName].AnimCollection;
                var nuPosition2 = position;
                fCount = _baseSprite.Collections[collection2].FrameCount;
                //  Figure out current frame in sequence
                if ((_ticks - _overlayTicks) / 5 < _baseSprite.Sequences[_overlayName].Frames.Length)
                {
                    currentFrame = _baseSprite.Sequences[_overlayName].Frames[(_ticks - _overlayTicks) / 5] + fCount * _direction;
                    //  Add in frame position offsets
                    nuPosition2.X += _baseSprite.Collections[collection2].FrameRect[currentFrame].X;
                    nuPosition2.Y += _baseSprite.Collections[collection2].FrameRect[currentFrame].Y;
                    //  Subtract collection offset
                    nuPosition2.X -= _baseSprite.Collections[0].CollectionOffset.X;
                    nuPosition2.Y -= _baseSprite.Collections[0].CollectionOffset.Y;
                    //  Draw
                    sb.Draw(_baseSprite.Collections[collection2].Textures[currentFrame], nuPosition2, c);
                }
            }

            //  End of sequence actions
            _ticks += 1;
            if (_ticks < totalFrames*5) return;
            switch (_action)
            {
                case "Walk":
                    _action = "Walk";
                    break;
                case "Fallback":
                    _action = "Getupback";
                    break;
                case "Fallforward":
                    _action = "Getupforward";
                    break;
                case "Crouch":
                    _action = "Breathe";
                    _stance = "Crouch";
                    break;
                case "Prone":
                    _action = "Breathe";
                    _stance = "Prone";
                    break;
                case "Stand":
                    _action = "Breathe";
                    _stance = "Stand";
                    break;
                default:
                    _action = "Breathe";
                    break;
            }
            if (_stance.Equals("Death"))
            {
                _stance = "Stand";
                _action = "Breathe";
            }
            RebuildSequence();
        }

        //  Helper to build the correct name for the animation sequence to display.
        private string BuildSequenceName()
        {
            //  Build the sequence name
            _stringBuilder.Clear();
            _stringBuilder.Append(_stance);
            _stringBuilder.Append(_action);
            switch (_stance)
            {
                case "Death":
                    break;
                case "Stand":
                    if (_action.Equals("Attack") || _action.Equals("Breathe") || _action.Equals("Walk"))
                        _stringBuilder.Append(_weapon);
                    break;
                default:
                    if (_action.Equals("Attack") || _action.Equals("Breathe"))
                        _stringBuilder.Append(_weapon);
                    break;
            }
            if (_action.Equals("Attack"))
                _stringBuilder.Append(_attack);

            var sequence = _stringBuilder.ToString();

            return sequence;
        }
    }
}
