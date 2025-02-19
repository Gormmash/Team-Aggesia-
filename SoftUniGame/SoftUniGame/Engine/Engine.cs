﻿using SoftUniGame.Models.Figures.Enemies;
using SoftUniGame.Models.Figures.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using RogueSharp.DiceNotation;
using RogueSharp.MapCreation;
using System.Collections.Generic;


namespace SoftUniGame.Engine
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Engine : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private Texture2D _floor;
        private Texture2D _wall;
        private IMap _map;
        private Player _player;
        private InputState _inputState;
        private List<AggressiveEnemy> _aggressiveEnemies = new List<AggressiveEnemy>();

        public Engine()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            _inputState = new InputState();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            //Setting the Camera Viewport Width and Height
            Global.Camera.ViewportWidth = graphics.GraphicsDevice.Viewport.Width;
            Global.Camera.ViewportHeight = graphics.GraphicsDevice.Viewport.Height;
            IMapCreationStrategy<Map> mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(50, 30, 100, 7, 3);
            _map = Map.Create(mapCreationStrategy);
            

            base.Initialize();
        }

        /// <summary>
        /// GetRandomEmptyCell will generate random position for the player on a walkable coords
        ///  where he can be summoned.       
        /// </summary>
        private Cell GetRandomEmptyCell()
        {
            while (true)
            {
                int x = Global.Random.Next(49);
                int y = Global.Random.Next(29);
                if (_map.IsWalkable(x, y))
                {
                    return _map.GetCell(x, y);
                }
            }
        }

        /// <summary>
        /// UpdatePlayerFieldOfView will set the Field of View of the player        
        /// </summary>
        private void UpdatePlayerFieldOfView()
        {
            _map.ComputeFov(_player.X, _player.Y, 30, true);
            foreach (Cell cell in _map.GetAllCells())
            {
                if (_map.IsInFov(cell.X, cell.Y))
                {
                    _map.SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                }
            }
        }
        /// <summary>
        /// UpdatePlayerFieldOfView will generate enemies       
        /// </summary>
        private void AddAggressiveEnemies(int numberOfEnemies)
        {
            for (int i = 0; i < numberOfEnemies; i++)
            {
                // Find a new empty cell for each enemy
                Cell enemyCell = GetRandomEmptyCell();
                var pathFromAggressiveEnemy =
                  new PathToPlayer(_player, _map, Content.Load<Texture2D>("White"));
                pathFromAggressiveEnemy.CreateFrom(enemyCell.X, enemyCell.Y);
                var enemy = new AggressiveEnemy(_map, pathFromAggressiveEnemy)
                {
                    X = enemyCell.X,
                    Y = enemyCell.Y,
                    Sprite = Content.Load<Texture2D>("Hound"),
                    // Hounds will get hit 50% of the time with no attack bonus
                    ArmorClass = 10,
                    AttackBonus = 0,
                    // Hounds roll one 3 sided Die for damage
                    Damage = Dice.Parse("d3"),
                    Health = 10,
                    Name = "Hunting Hound"
                };
                // Add each enemy to list of enemies
                _aggressiveEnemies.Add(enemy);
            }
        }
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _floor = Content.Load<Texture2D>("Floor");
            _wall = Content.Load<Texture2D>("Wall");
            
            Cell startingCell = GetRandomEmptyCell();
            _player = new Player
            {
                X = startingCell.X,
                Y = startingCell.Y,                
                Sprite = Content.Load<Texture2D>("Player"),
                // With a 15 armor class if the enemy has no attack bonus
                // the player will be hit 25% of the time
                ArmorClass = 15,
                AttackBonus = 1,
                // The player will roll 2D4 for damage or 2 x 4 sided Die
                // We can use the Dice class in RogueSharp for this
                Damage = Dice.Parse("2d4"),                
                
                Health = 50,
                Name = "Mr. Rogue"
            };
            UpdatePlayerFieldOfView();
            Global.Camera.CenterOn(startingCell);
            startingCell = GetRandomEmptyCell();
            var pathFromAggressiveEnemy = new PathToPlayer(_player, _map, Content.Load<Texture2D>("White"));
            pathFromAggressiveEnemy.CreateFrom(startingCell.X, startingCell.Y);
            AddAggressiveEnemies(10);
            Global.CombatManager = new CombatManager(_player, _aggressiveEnemies);
            Global.GameState = GameStates.States.PlayerTurn;
            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            _inputState.Update();
            Global.Camera.HandleInput(_inputState, PlayerIndex.One);      //Camera Update

            if (_inputState.IsExitGame(PlayerIndex.One))
            {
                Exit();
            }
            // New code to handle switching modes when spacebar is pressed
            else if (_inputState.IsSpace(PlayerIndex.One))
            {
                if (Global.GameState == GameStates.States.PlayerTurn)
                {
                    Global.GameState = GameStates.States.Debugging;
                }
                else if (Global.GameState == GameStates.States.Debugging)
                {
                    Global.GameState = GameStates.States.PlayerTurn;
                }
            }
            else
            {
                if (Global.GameState == GameStates.States.PlayerTurn
                    && _player.HandleInput(_inputState, _map))
                {
                    UpdatePlayerFieldOfView();
                    Global.Camera.CenterOn(_map.GetCell(_player.X, _player.Y));
                    Global.GameState = GameStates.States.EnemyTurn;
                }
                if (Global.GameState == GameStates.States.EnemyTurn)
                {
                    foreach (var enemy in _aggressiveEnemies)
                    {
                        enemy.Update();
                    }
                    Global.GameState = GameStates.States.PlayerTurn;
                }
            }
            // TODO: Add your update logic here           
           
            
            base.Update(gameTime);
        }

        
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here

            
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, null, null, null, null, Global.Camera.TranslationMatrix);  //Start of drawing



            foreach (Cell cell in _map.GetAllCells())
            {
                var position = new Vector2(cell.X * Global.SpriteWidth, cell.Y * Global.SpriteHeight);
                if (!cell.IsExplored && Global.GameState != GameStates.States.Debugging)
                {
                    continue;
                }
                Color tint = Color.White;
                if (!cell.IsInFov && Global.GameState != GameStates.States.Debugging)
                {
                    tint = Color.Gray;
                }
                if (cell.IsWalkable)
                {
                    spriteBatch.Draw(_floor, position, null, null, null, 0.0f, Vector2.One, tint, SpriteEffects.None, LayerDepth.Cells);
                }
                else
                {
                    spriteBatch.Draw(_wall, position, null, null, null, 0.0f, Vector2.One, tint, SpriteEffects.None, LayerDepth.Cells);
                }
            }

            
            _player.Draw(spriteBatch);   //Draw player

            foreach (var enemy in _aggressiveEnemies)  //Draw Aggresive enemies
            {                
                if (Global.GameState == GameStates.States.Debugging || _map.IsInFov(enemy.X, enemy.Y))
                {
                    enemy.Draw(spriteBatch);
                }
            }

            spriteBatch.End();   //End of drawing 

            base.Draw(gameTime);
        }
    }
}
