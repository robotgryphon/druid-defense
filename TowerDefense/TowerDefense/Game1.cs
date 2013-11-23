using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Ostenvighx.Framework.Xna.Input;
using Ostenvighx.Framework.Xna.Graphics;
using Ostenvighx.Framework.Xna.Layout;
using Ostenvighx.Framework.Xna.Entities;
using Ostenvighx.Framework.Xna.Utilities;

using TowerDefense.Tiles;
using TowerDefense.Towers;

namespace TowerDefense
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch canvas;

        InputState oldIS;
        InputState newIS;

        SpriteFont Segoe;

        
        LayoutHelper TileManager;

        Spritesheet TurretSheet;
        TileWithTower TurretTile;
        Tower Turret;

        Texture2D GrassTexture;

        // Tracks where the cursor is at over the board
        public TilePosition CursorLocation;
        Texture2D CursorImage;

        Point GRID_SIZE;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferHeight = 600;
            Content.RootDirectory = "Content";
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
            oldIS = new InputState().Update();
            newIS = new InputState().Update();

            GRID_SIZE = new Point(50, 50);
            CursorLocation = new TilePosition(Point.Zero, GRID_SIZE);
            base.Initialize();

            // Initialize map with all grass tiles!
            for (int TilePosX = 0; TilePosX < TileManager.GridSize.X; TilePosX++)
                for (int TilePosY = 0; TilePosY < TileManager.GridSize.Y; TilePosY++){
                    Point TileGridPosition = new Point(TilePosX, TilePosY);
                    TileManager.SetTile(TileGridPosition, new Tile(GrassTexture, GRID_SIZE, new TilePosition(TileGridPosition, GRID_SIZE)));
                }

            Turret = new Tower(TurretSheet, new Rectangle(0, 0, 50, 50));
            TurretTile = new TileWithTower(Turret, GrassTexture, GRID_SIZE, new TilePosition(Point.Zero, GRID_SIZE));

            TileManager.SetTile(Point.Zero, TurretTile);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            canvas = new SpriteBatch(GraphicsDevice);

            Segoe = Content.Load<SpriteFont>("Fonts/Segoe");
            
            TileManager = new LayoutHelper(new Point(10, 10), GRID_SIZE);

            GrassTexture = Content.Load<Texture2D>("Tiles/Grass");

            TurretSheet = new Spritesheet(Content.Load<Texture2D>("Towers/Tiny"));

            CursorImage = Content.Load<Texture2D>("Images/Pointer");

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
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

            newIS.Update();

            if (newIS.controllers[0].Buttons.Back == ButtonState.Pressed || KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Escape))
                this.Exit();

            #region Cursor Moving
            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.W) || newIS.controllers[0].ThumbSticks.Left.Y < 0)
                CursorLocation.Move((int) Directions.Up, TileManager.GridSize); 
            
            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.A) || newIS.controllers[0].ThumbSticks.Left.X < 0)
                CursorLocation.Move((int) Directions.Left, TileManager.GridSize);

            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.S) || newIS.controllers[0].ThumbSticks.Left.Y > 0)
                CursorLocation.Move((int) Directions.Down, TileManager.GridSize);

            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.D) || newIS.controllers[0].ThumbSticks.Left.X > 0)
                CursorLocation.Move((int) Directions.Right, TileManager.GridSize);

            #endregion

            TileManager.Update(gameTime);

            oldIS.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Background
            TileManager.Draw(gameTime, canvas);

            Turret.Draw(gameTime, canvas, TurretTile.Location);

            // Foreground
            canvas.Begin();
            // canvas.DrawString(Segoe, "Tower Defense!", new Vector2(10, 10), Color.White);

            canvas.Draw(
                CursorImage, 
                new Rectangle(
                    (int) CursorLocation.GetDrawingLocation().X + GRID_SIZE.X / 2, 
                    (int) CursorLocation.GetDrawingLocation().Y + GRID_SIZE.Y / 2, 
                    CursorImage.Bounds.Width, 
                    CursorImage.Bounds.Height), 
                new Rectangle(0, 0, 25, 27),
                Color.White,
                0,
                new Vector2(10, 0),
                SpriteEffects.None,
                1);
            canvas.End();

            base.Draw(gameTime);
        }
    }
}
