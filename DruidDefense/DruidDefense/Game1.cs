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

using DruidDefense.Tiles;
using DruidDefense.Towers;

namespace DruidDefense
{

    public enum GameState
    {
        Loading,
        Editing,
        Playing,
        CheatScreen,
        GameOver
    };

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

        Texture2D DebugSkin;
        
        LayoutHelper TileManager;

        Spritesheet TowerPlaceholders;
        TileWithTower TurretTile;
        Tower Turret;

        Texture2D GrassTexture;

        // Tracks where the cursor is at over the board
        public TilePosition CursorLocation;
        Texture2D CursorImage;

        Point Grid_Tile_Size;

        CheatCodeManager CheatCodeSystem;

        GameState PreviousState;
        GameState CurrentState;

        float Money;
        Boolean NukeMode;

        float GridScale;

        Dictionary<String, float> TilePrices;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            graphics.ApplyChanges();

            Window.Title = "Druid Defenders";

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

            base.Initialize();

            // Initialize the Input states for catching user input
            oldIS = new InputState().Update();
            newIS = new InputState().Update();

            // Define the size of the grid tiles
            Grid_Tile_Size = new Point(42, 42);

            // Define the starting location for the editing cursor
            CursorLocation = new TilePosition(Point.Zero, Grid_Tile_Size);

            // Initialize the tile manager (grid system)
            TileManager = new LayoutHelper(new Point(16, 12), Grid_Tile_Size);

            // Initialize map with all grass tiles!
            for (int TilePosX = 0; TilePosX < TileManager.GridSize.X; TilePosX++)
                for (int TilePosY = 0; TilePosY < TileManager.GridSize.Y; TilePosY++){
                    Point TileGridPosition = new Point(TilePosX, TilePosY);
                    TileManager.SetTile(TileGridPosition, new Tile(GrassTexture, new TilePosition(TileGridPosition, Grid_Tile_Size), "Tile.Grass"));
                }

            // Initialize the cheat code system.
            CheatCodeSystem = new CheatCodeManager(10, (int)CaptureSources.Keyboard, new TimeSpan(0, 0, 0, 0, 200));
            CheatCodeSystem.CheatCodeSubmitted += CheatCodeFinished;

            // Give the player some money to work with.
            Money = 300f;

            // Set NukeMode to false.
            NukeMode = false;

            // Start the game proper, in editing mode.
            PreviousState = GameState.Loading;
            CurrentState = GameState.Editing;

            GridScale = 42 / 50;

            TilePrices = new Dictionary<string, float>();
            TilePrices.Add("Tile.Grass", 0);
            TilePrices.Add("Tile.Tower.Turret", 50);
            TilePrices.Add("Tile.Tower.Tower2", 10);
        }

        /// <summary>
        /// Load in all the needed resources for things. (And stuff!)
        /// </summary>
        protected override void LoadContent()
        {

            base.LoadContent();

            // Set the game state to loading.
            CurrentState = GameState.Loading;

            // Create a new SpriteBatch, which can be used to draw textures.
            canvas = new SpriteBatch(GraphicsDevice);

            // Load in a font for drawing text
            Segoe = Content.Load<SpriteFont>("Fonts/Segoe");
            
            // Load in a grass texture.
            GrassTexture = Content.Load<Texture2D>("Tiles/Grass");

            TowerPlaceholders = new Spritesheet(Content.Load<Texture2D>("Towers/TowerPlaceholders"));

            CursorImage = Content.Load<Texture2D>("Images/Pointer");

            DebugSkin = Content.Load<Texture2D>("Images/DebugSkin");
            
        }

        /// <summary>
        /// Used when the cheat code system is done.
        /// This method parses the finished cheat code and tests it
        /// against the ones defined. If it's valid.. well, you know.
        /// </summary>
        private void CheatCodeFinished(CheatCodeManager ccManager)
        {
            Console.WriteLine(ccManager.ToString());
            
            String cheatcode = "";
            foreach (Keys key in ccManager.KeyboardBuffer)
               cheatcode += key.ToString();

            Console.WriteLine("Cheat code entered: " + cheatcode);

            if (cheatcode.Equals("MONEH"))
            {
                Money = int.MaxValue;
            }
            else if(cheatcode.Equals("IMMUNE"))
            {
                Console.WriteLine("Immunity activated.");
            }
            else if (cheatcode.Equals("ORBITNUKE"))
            {
                // Oh god, nuke all the creeps
                NukeMode = !NukeMode;
            }

            CurrentState = PreviousState;
            PreviousState = GameState.CheatScreen;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            newIS.Update();

            if((CurrentState == GameState.Editing || CurrentState == GameState.Playing) && 
                (newIS.controllers[0].Buttons.Back == ButtonState.Pressed || KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Escape)))
                    this.Exit();

            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
            {
                PreviousState = GameState.Editing;
                CurrentState = GameState.CheatScreen;
            }

            if (CurrentState == GameState.CheatScreen)
            {

                CheatCodeSystem.Update(gameTime, newIS);

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Back))
                {
                    CheatCodeSystem.DeleteInputs();
                }

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Escape))
                {
                    CurrentState = PreviousState;
                    PreviousState = GameState.CheatScreen;
                }

            }
            else if (CurrentState == GameState.Editing)
            {
                #region Cursor Moving
                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.W) || newIS.controllers[0].ThumbSticks.Left.Y > 0)
                    CursorLocation.Move((int)Directions.Up, TileManager.GridSize);

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.A) || newIS.controllers[0].ThumbSticks.Left.X < 0)
                    CursorLocation.Move((int)Directions.Left, TileManager.GridSize);

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.S) || newIS.controllers[0].ThumbSticks.Left.Y < 0) {
                    if (CursorLocation.GridLocation.X < TileManager.GridSize.X) {
                        CursorLocation.Move((int)Directions.Down, TileManager.GridSize);
                    } else {

                    }
                }

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.D) || newIS.controllers[0].ThumbSticks.Left.X > 0)
                    CursorLocation.Move((int)Directions.Right, TileManager.GridSize);

                #endregion

                TileManager.SetTileSize(new Point(42, 42));
                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Delete) || (oldIS.controllers[0].Buttons.Y == ButtonState.Released && newIS.controllers[0].Buttons.Y == ButtonState.Pressed)) {
                    String TileTypeAtCursor = TileManager.GetTileType(CursorLocation.GridLocation);
                    if (!TileTypeAtCursor.Equals("Tile.Grass") || !TileTypeAtCursor.Equals("Null")) {
                        Money += TilePrices[TileTypeAtCursor];
                        TileManager.SetTile(CursorLocation.GridLocation, new Tile(GrassTexture, (TilePosition) CursorLocation.Clone(), "Tile.Grass"));
                    }
                }

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Z) || (oldIS.controllers[0].Buttons.A == ButtonState.Released && newIS.controllers[0].Buttons.A == ButtonState.Pressed)) {
                    if (!TileManager.IsTileFilled(CursorLocation.GridLocation)) {
                        if (Money >= TilePrices["Tile.Tower.Turret"]) {
                            Money -= TilePrices["Tile.Tower.Turret"];
                            TileWithTower TurretTower = TowerFactory.CreateNewTurret(TowerPlaceholders, CursorLocation, GrassTexture);
                            TileManager.SetTile(CursorLocation.GridLocation, TurretTower);
                        }
                    } else {
                        // Tile already has something in it
                        Console.WriteLine("Tile is already filled at: " + CursorLocation.GridLocation.ToString());
                    }
                }

                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.X) || (oldIS.controllers[0].Buttons.B == ButtonState.Released && newIS.controllers[0].Buttons.B == ButtonState.Pressed)) {
                    if (!TileManager.IsTileFilled(CursorLocation.GridLocation)) {
                        if (Money >= TilePrices["Tile.Tower.Tower2"]) {
                            Money -= TilePrices["Tile.Tower.Tower2"];
                            TileWithTower TurretTower = TowerFactory.CreateNewTower2(TowerPlaceholders, CursorLocation, GrassTexture);
                            TileManager.SetTile(CursorLocation.GridLocation, TurretTower);
                        }
                    } else {
                        // Tile already has something in it
                        Console.WriteLine("Tile is already filled at: " + CursorLocation.GridLocation.ToString());
                    }
                }

                // We started the cheat code enterer
                if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
                {
                    PreviousState = CurrentState;
                    CurrentState = GameState.CheatScreen;
                }

                
            } else if (CurrentState == GameState.Playing){
                GridScale = 1;
                TileManager.Update(gameTime);
            }
            

            oldIS.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // 56, 70, 19 is the grass color
            GraphicsDevice.Clear(new Color(56, 70, 19));

            // Background
            TileManager.Draw(gameTime, canvas);

            

            // Foreground
            canvas.Begin();
            // canvas.DrawString(Segoe, TurretTile.Location.ToString(), new Vector2(10, 10), Color.White);

            canvas.Draw(
                CursorImage, 
                new Rectangle(
                    (int) CursorLocation.GetDrawingLocation().X + Grid_Tile_Size.X / 2, 
                    (int) CursorLocation.GetDrawingLocation().Y + Grid_Tile_Size.Y / 2, 
                    CursorImage.Bounds.Width, 
                    CursorImage.Bounds.Height), 
                new Rectangle(0, 0, 25, 27),
                Color.White,
                0,
                new Vector2(10, 0),
                SpriteEffects.None,
                GridScale);

            Vector2 MoneySize = Segoe.MeasureString(String.Format("{0:C}", Money));
            canvas.DrawString(Segoe, String.Format("{0:C}", Money), new Vector2(Window.ClientBounds.Width - MoneySize.X - 10, 0), Color.White);

            canvas.End();

            if (CurrentState == GameState.CheatScreen)
            {
                canvas.Begin();
                canvas.DrawString(Segoe, CheatCodeSystem.ToString(), Vector2.Zero, Color.White);
                canvas.End();
            }

            base.Draw(gameTime);
        }
    }
}
