using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Editor.Model;
using ISOTools;
using IsoGame.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IsoGame.Screens.Base;

namespace IsoGame.Misc
{
    public class Scroller
    {
        private Rectangle _worldSpace;
        private Rectangle _screenSpace;
        private Rectangle _anchorSpace;
        private Point _screenAnchor;

        private ClientGame _game;
        private GameState _state;
        private MapDefinition _map;
        private Isometry _iso;
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        public Scroller(ClientGame game, GameState state)
        {
            _game = game;
            _map = state.Map;
            _iso = _map.Iso;
            _state = state;
            _spriteBatch = game.SpriteBatch;
            _screenSpace = new Rectangle(0, 0, _game.GraphicsDevice.PresentationParameters.BackBufferWidth, _game.GraphicsDevice.PresentationParameters.BackBufferHeight);

            //  World space
            var max = _iso.TilePlotter(new Point(_map.Width - 1, _map.Height - 1));
            _worldSpace = new Rectangle(0, 0, max.X + _map.TileWidth, max.Y + _map.TileHeight);
            //  Anchor space
            _anchorSpace = _worldSpace;
            var horizontal = _screenSpace.Right - _screenSpace.Left;
            _anchorSpace.Width -= horizontal;
            if (_anchorSpace.Right < _anchorSpace.Left)
                _anchorSpace.Width = 0;
            var vertical = _screenSpace.Bottom - _screenSpace.Top;
            _anchorSpace.Height -= vertical;
            if (_anchorSpace.Bottom < _anchorSpace.Top)
                _anchorSpace.Height = 0;
            //  Adjust for staggered maps to eliminate jaggies when scrolling to map edge
            if (_iso.Style == IsometricStyle.Staggered)
            {
                _anchorSpace.Y += _map.TileHeight;
                _anchorSpace.Height -= _map.TileHeight;
                _anchorSpace.X += _map.TileWidth;
                _anchorSpace.Width -= _map.TileWidth;
                _screenAnchor = new Point(_anchorSpace.Left, _anchorSpace.Top);
            }
            else
            {
                //  Screen anchor
                _screenAnchor = new Point(0, 0);
            }
        }

        /// <summary>
        /// Updates the scroller.
        /// </summary>
        /// <param name="gameTime"></param>
        internal void Update(GameTime gameTime, InputState input)
        {
            //  Scroll map on mouse at edges 
            if (input.CurrentMouseState.Y < 10)
                if (_screenAnchor.Y > _anchorSpace.Top)
                    _screenAnchor.Y -= 10 - input.CurrentMouseState.Y;


            if (input.CurrentMouseState.Y > _screenSpace.Bottom - 10)
                if (_screenAnchor.Y < _anchorSpace.Bottom)
                    _screenAnchor.Y += 10 - (_screenSpace.Bottom - input.CurrentMouseState.Y);

            if (input.CurrentMouseState.X < 10)
                if (_screenAnchor.X > _anchorSpace.Left)
                    _screenAnchor.X -= 10 - input.CurrentMouseState.X;


            if (input.CurrentMouseState.X > _screenSpace.Right - 10)
                if (_screenAnchor.X < _anchorSpace.Right)
                    _screenAnchor.X += 10 - (_screenSpace.Right - input.CurrentMouseState.X);

            if (_screenAnchor.X < _anchorSpace.X)
                _screenAnchor.X = _anchorSpace.X;
            if (_screenAnchor.X > _anchorSpace.Right)
                _screenAnchor.X = _anchorSpace.Right;

            if (_screenAnchor.Y < _anchorSpace.Y)
                _screenAnchor.Y = _anchorSpace.Y;
            if (_screenAnchor.Y > _anchorSpace.Bottom)
                _screenAnchor.Y = _anchorSpace.Bottom;
        }

        /// <summary>
        /// Renders the map.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            for (var y = 0; y < _map.Height; y++)
                for (var x = 0; x < _map.Width; x++)
                {
                    for (var layer = 0; layer < _map.Cells[0, 0].Length; layer++)
                                    //  Check for null (no tile)
                        if (_map.Cells[x, y][layer] != null)
                        {
                            //  Base drawing point for a 1x1 tile
                            var drawPoint = _iso.TilePlotter(new Point(x, y));

                            //  Correct for height of tile
                            if (_map.Cells[x, y][layer].Height > _map.TileHeight)
                            {
                                drawPoint.Y -= (_map.Cells[x, y][layer].Height - _map.TileHeight);
                            }

                            //  And now for width
                            drawPoint.X += (6 - _map.Cells[x, y][layer].BoundingBox[2])*6;

                            //  we use layer 2 for wall filler which needs a different offset
                            if (layer == 2)
                            {
                                if (_map.Cells[x, y][layer].Name.Contains("se") ||
                                    _map.Cells[x, y][layer].Name.Contains("nw"))
                                {

                                }
                                else
                                {
                                    drawPoint.X -= 12;
                                    drawPoint.Y -= 6;
                                }
                            }

                            _map.Cells[x, y][layer].DrawTexture(_spriteBatch, new Vector2(drawPoint.X - _screenAnchor.X, drawPoint.Y - _screenAnchor.Y), Color.White);
                        }
                }
            _spriteBatch.End();
        }
    }
}
