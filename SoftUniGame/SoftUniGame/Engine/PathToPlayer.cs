﻿using SoftUniGame.Models.Figures.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using System.Linq;
using SoftUniGame.Engine;

namespace SoftUniGame.Engine
{
    public class PathToPlayer
    {

        private readonly Player _player;
        private readonly IMap _map;
        private readonly Texture2D _sprite;
        private readonly PathFinder _pathFinder;
        private Path _cells;

        public PathToPlayer(Player player, IMap map, Texture2D sprite)
        {
            _player = player;
            _map = map;
            _sprite = sprite;
            _pathFinder = new PathFinder(map);
        }
        public Cell FirstCell
        {
            get
            {
                return _cells.Steps.First();
            }
        }
        public void CreateFrom(int x, int y)
        {
            _cells = _pathFinder.ShortestPath(_map.GetCell(x, y), _map.GetCell(_player.X, _player.Y));
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_cells != null && Global.GameState == GameStates.States.Debugging)
            {
                foreach (Cell cell in _cells.Steps)
                {
                    if (cell != null)
                    {

                        float multiplier = _sprite.Width;
                        spriteBatch.Draw(_sprite, new Vector2(cell.X * multiplier, cell.Y * multiplier), null, null, null, 0.0f, Vector2.One, Color.Blue * .2f, SpriteEffects.None, LayerDepth.Figures);
                    }
                }
            }
        }
    }
}
