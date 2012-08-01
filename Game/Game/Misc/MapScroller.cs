using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IsoGame.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IsoGame.Screens.Base;
using ZarTools;
using Microsoft.Xna.Framework.Input;
using IsoGame.Processes;
using Core;
using System.Windows.Forms;
using Core.AStar;
using IsoGame.Events;

namespace IsoGame.Misc
{
    public class MapScroller : IEventListener
    {
        private Rectangle _worldSpace;
        private Rectangle _screenSpace;
        private Rectangle _anchorSpace;
        private Point _screenAnchor;

        private ClientGame _game;
        private GameState _state;
        private Module _module;
        private Isometry _iso;
        private SpriteBatch _spriteBatch;
        private SpriteFont _gameFont;
        private Cursor _lastCursor;
        private Texture2D _circle;

        public Unit SelectedUnit { get; set; }
        bool characterAvailable;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        public MapScroller(ClientGame game, GameState state)
        {
            RegisterListeners();
            characterAvailable = true;
            _game = game;
            _module = state.Module;
            _iso = _module.Map.Iso;
            _state = state;
            _spriteBatch = game.SpriteBatch;
            _gameFont = game.Content.Load<SpriteFont>("Fonts//gameFont");
            _circle = _game.Content.Load<Texture2D>("Textures//circle");
            _screenSpace = new Rectangle(0, 0, _game.GraphicsDevice.PresentationParameters.BackBufferWidth, _game.GraphicsDevice.PresentationParameters.BackBufferHeight);

            //  World space
            var max = _iso.TilePlotter(new Point(_module.Map.Width - 1, _module.Map.Height - 1));
            _worldSpace = new Rectangle(0, 0, max.X + _module.Map.TileWidth, max.Y + _module.Map.TileHeight);
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
                _anchorSpace.Y += _module.Map.TileHeight / 2;
                _anchorSpace.Height -= _module.Map.TileHeight / 2;

                _anchorSpace.X += _module.Map.TileWidth / 2;
                _anchorSpace.Width -= _module.Map.TileWidth / 2;

                _screenAnchor = new Point(_anchorSpace.Left, _anchorSpace.Top);
            }
            else
            {
                //  Screen anchor
                _screenAnchor = new Point(0, 0);
            }

        }

        /// <summary>
        /// Updates the map scroller.
        /// </summary>
        /// <param name="gameTime"></param>
        internal void Update(GameTime gameTime, InputState input)
        {
            ScrollMap(input);
            HandleInput(input);
            UpdateCursor(input);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void HandleInput(InputState input)
        {
            //  LMB Mouse clicks
            if (input.CurrentMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && input.LastMouseState.LeftButton != Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                //  Check for click on unit
                foreach (Unit u in _module.Roster)
                {
                    if (GetSprite(u).HitTest(input.CurrentMouseState.X, input.CurrentMouseState.Y))
                    {
                        if (u.OwnerID == _game.PlayerID)
                        {
                            //  Own unit - select
                            SelectedUnit = u;
                        }
                        else
                        {
                            //  Enemy unit - Handle attack order
                        }
                        return;
                    }
                }

                //  Check for move order
                if (SelectedUnit != null && characterAvailable)
                {
                    var destination = _iso.MouseMapper(new System.Drawing.Point(
                                                        input.CurrentMouseState.X + _screenAnchor.X,
                                                        input.CurrentMouseState.Y + _screenAnchor.Y));
                    //  Check destination is valid
                    if (_module.Map.IsOnGrid(destination) && _module.Map.Cells[destination.X, destination.Y].IsWalkable()
                        && destination != new System.Drawing.Point(SelectedUnit.X, SelectedUnit.Y))
                    {
                        //  Get the path if one exists
                        Path<MapCell> path = AStar.FindPath<MapCell>
                            (_module.Map.Cells[SelectedUnit.X, SelectedUnit.Y],
                            _module.Map.Cells[destination.X, destination.Y],
                            MapCell.Distance, MapCell.DiagonalHeuristic);
                        if (path == null) return;
                        //  Do it
                        MapCell prev = null;
                        List<CompassDirection> dirs = new List<CompassDirection>();
                        foreach (MapCell cell in path.Reverse())
                        {
                            if (prev != null)
                            {
                                dirs.Add(_iso.DirectionOfNeighbour(prev.MapCoordinate, cell.MapCoordinate));
                            }
                            prev = cell;
                        }
                        AnimatedMoveProcess proc = new AnimatedMoveProcess(SelectedUnit, _iso, dirs.ToArray());

                        ClientGame._processManager.ProcessList.Add(proc);
                    }
                }
            }
        }

        /// <summary>
        /// Context-sensitive cursor.
        /// </summary>
        /// <param name="input"></param>
        private void UpdateCursor(InputState input)
        {
            Cursor c = CustomCursors.Normal;

            foreach (Unit u in _module.Roster)
            {
                if (GetSprite(u).HitTest(input.CurrentMouseState.X, input.CurrentMouseState.Y))
                    c = (u.OwnerID == _game.PlayerID) ? CustomCursors.Select : CustomCursors.Attack;
            }
            if (_lastCursor != c)
                ClientGame.winForm.Cursor = c;
            _lastCursor = c;
        }

        /// <summary>
        /// Scrolls the map if mouse it at edges.
        /// </summary>
        /// <param name="input"></param>
        private void ScrollMap(InputState input)
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

            //  Bounds
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

            for (var y = 0; y < _module.Map.Height; y++)
                for (var x = 0; x < _module.Map.Width; x++)
                {
                    _module.Map.Cells[x, y].IsOccupied = false;

                    for (var layer = 0; layer < _module.Map.Cells[0, 0].Length; layer++)
                    {
                        //  Draw any characters
                        if (layer == 2 )
                        {
                            DrawCharacters(x, y, gameTime);
                        }
                        //  Check for null (no tile)
                        if (_module.Map.Cells[x, y][layer] != null)
                        {
                            //  Base drawing point for a 1x1 tile
                            var drawPoint = _iso.TilePlotter(new Point(x, y));

                            //  Correct for height of tile
                            if (_module.Map.Cells[x, y][layer].Height > _module.Map.TileHeight)
                            {
                                drawPoint.Y -= (_module.Map.Cells[x, y][layer].Height - _module.Map.TileHeight);
                            }

                            //  And now for width
                            drawPoint.X += (6 - _module.Map.Cells[x, y][layer].BoundingBox[2]) * 6;

                            //  we use layer 2 for wall filler which needs a different offset
                            if (layer == 2)
                            {
                                if (_module.Map.Cells[x, y][layer].Name.Contains("se") ||
                                    _module.Map.Cells[x, y][layer].Name.Contains("nw"))
                                {

                                }
                                else
                                {
                                    drawPoint.X -= 12;
                                    drawPoint.Y -= 6;
                                }
                            }

                            _module.Map.Cells[x, y][layer].DrawTexture(_spriteBatch, new Vector2(drawPoint.X - _screenAnchor.X, drawPoint.Y - _screenAnchor.Y), Color.White);
                        }
                    }
                }
            _spriteBatch.End();
        }

        /// <summary>
        /// Draws the characters
        /// </summary>
        /// <param name="gameTime"></param>
        private void DrawCharacters(int x, int y, GameTime gameTime)
        {
            foreach (var unit in _module.Roster)
            {
                ZSprite sprite = GetSprite(unit);

                if (sprite.AnimXOffset != 0 || sprite.AnimYOffset != 0)
                {
                    //  We're moving so adjust draw order
                    switch (unit.Facing)
                    {
                        case CompassDirection.East:
                            y--;
                            x--;
                            break;
                        case CompassDirection.West:
                            y--;
                            break;
                        case CompassDirection.SouthEast:
                            y--;
                            x--;
                            break;
                        case CompassDirection.South:
                            y -= 2;
                            break;
                        case CompassDirection.SouthWest:
                            y--;
                            x--;
                            break;
                    }
                }

                if (unit.X == x && unit.Y == y)
                {
                    _module.Map.Cells[x, y].IsOccupied = true;

                    Point p = _iso.TilePlotter(new Point(unit.X, unit.Y));

                    Vector2 position = new Vector2(p.X - _screenAnchor.X, (p.Y - _screenAnchor.Y));
                    //  Selection circle
                    if (unit == SelectedUnit)
                        _spriteBatch.Draw(_circle, new Vector2(position.X + _module.Map.TileHeight / 2 + sprite.AnimXOffset, position.Y + _module.Map.TileHeight / 4 + sprite.AnimYOffset), Color.DarkCyan);
                    sprite.DrawCurrentImage(_spriteBatch, position, Color.White);
                }
            }
        }

        /// <summary>
        /// Returns the sprite for the given unit.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private ZSprite GetSprite(Unit unit)
        {
            if (unit.Sprite != null) return unit.Sprite;
            //  Lazy load
            unit.Body = BodyType.LeatherMale;
            unit.Weapon = WeaponType.SMG;
            unit.Facing = CompassDirection.East;
            ZSprite sprite = new ZSprite(_game.GraphicsDevice, "D:\\workspace\\BaseGame\\sprites\\characters\\" + Enum.GetName(typeof(BodyType), unit.Body) + ".spr");
            sprite._baseSprite.ReadAnimation(sprite.Sequences["StandBreathe"].AnimCollection);
            sprite.CurrentSequence = "StandBreathe";
            unit.Sprite = sprite;
            ClientGame._processManager.ProcessList.Add(new AnimProcess(unit, AnimAction.Breathe, true));
            //  Test some anims!

            //var anim = new AnimatedMoveProcess(unit, _iso, CompassDirection.NorthWest, CompassDirection.NorthWest, CompassDirection.North);
            //var wait = new WaitProcess(1000);
            //anim.Next = wait;
            //var anim2 = new AnimatedMoveProcess(unit, _iso, CompassDirection.East, CompassDirection.East, CompassDirection.East, CompassDirection.East, CompassDirection.SouthEast, CompassDirection.SouthEast, CompassDirection.SouthEast, CompassDirection.SouthEast);
            //wait.Next = anim2;
            //ClientGame._processManager.ProcessList.Add(anim);
            return sprite;
        }

        public string GetListenerName()
        {
            return "MapScroller";
        }

        public bool HandleEvent(GameEvent ev)
        {
            if (ev.EventType == EventType.CharacterBusy)
                characterAvailable = false;
            if (ev.EventType == EventType.CharacterAvailable)
                characterAvailable = true;
            return true;
        }

        public void RegisterListeners()
        {
            ClientGame._eventManager.AddEventListener(this, EventType.CharacterBusy);
            ClientGame._eventManager.AddEventListener(this, EventType.CharacterAvailable);
        }
    }
}
