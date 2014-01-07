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
using DruidDefense.Creeps;

namespace DruidDefense
{

    public enum GameState
    {
        Loading,
        MenuScreen,
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
        
        // Handlers. Handlers everywhere.
        DefenseTileManager GridTileManager;
        EntityHandler EntityHandler;
        CreepHandler CreepHandler;

        Spritesheet TowerPlaceholders;
        Texture2D GrassTexture;
        Texture2D HeartTexture;

        // Tracks where the cursor is at over the board
        public TilePosition CursorLocation;
        Texture2D CursorImage;
        Texture2D Overlay;

        Point Grid_Tile_Size;

        CheatCodeManager CheatCodeSystem;

        GameState PreviousState;
        GameState CurrentState;

        float Money;
        float Life;

        Dictionary<String, float> TilePrices;

        Boolean DebugMode;
        
        TimeSpan ControllerMoveDelay;
        TimeSpan TimeSinceControllerMove;

        List<TilePosition> CreepSpawns;
        List<TilePosition> CreepGoals;

        public static Random Randomizer;

        // Creep testing stuffs.
        public Texture2D BlobCreepSheet;

        public static Direction[] PossibleDirections = { Direction.North, Direction.South, Direction.West, Direction.East };

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

            Randomizer = new Random();

            // Initialize the Input states for catching user input
            oldIS = new InputState().Update();
            newIS = new InputState().Update();

            // Define the size of the grid tiles
            Grid_Tile_Size = new Point(42, 42);

            // Initialize the tile manager (grid system)
            GridTileManager = new DefenseTileManager(new Point(16, 12), Grid_Tile_Size);

            // Define the starting location for the editing cursor
            CursorLocation = new TilePosition(Point.Zero, GridTileManager);

            // Initialize the entity handler for managing creeps and projectiles
            EntityHandler = new EntityHandler(150);

            // Initialize map with all grass tiles!
            for (int TilePosX = 0; TilePosX < GridTileManager.GridSize.X; TilePosX++)
                for (int TilePosY = 0; TilePosY < GridTileManager.GridSize.Y; TilePosY++){
                    Point TileGridPosition = new Point(TilePosX, TilePosY);
                    GridTileManager.ReplaceTile(new Tile(GrassTexture, new TilePosition(TileGridPosition, GridTileManager), "Tile.Grass"));
                }

            // Initialize the cheat code system.
            CheatCodeSystem = new CheatCodeManager(10, (int)CaptureSources.Keyboard, new TimeSpan(0, 0, 0, 0, 200));
            CheatCodeSystem.CheatCodeSubmitted += CheatCodeFinished;

            // Give the player some money to work with.
            Money = 300f;
            Life = 5f;

            // Start the game proper, in editing mode.
            PreviousState = GameState.Loading;
            CurrentState = GameState.Editing;

            TilePrices = new Dictionary<string, float>();
            TilePrices.Add("Tile.Grass", 0);
            TilePrices.Add("Tile.Tower.Turret", 50);
            TilePrices.Add("Tile.Tower.Tower2", 10);
            TilePrices.Add("Tile.Tower.Tower3", 25);

            DebugMode = true;

            ControllerMoveDelay = new TimeSpan(0, 0, 0, 0, 500);
            TimeSinceControllerMove = new TimeSpan();

            CreepHandler = new Creeps.CreepHandler(1000);
            CreepHandler.SpawnDelay = new TimeSpan(0, 0, 0, 0, 1);

            CreepGoals = new List<TilePosition>();
            CreepGoals.Add(new TilePosition(new Point(6, 6), GridTileManager));

            CreepSpawns = new List<TilePosition>();
            // Above
            CreepSpawns.Add(new TilePosition(new Point(3, 3), GridTileManager));
            CreepSpawns.Add(new TilePosition(new Point(6, 3), GridTileManager));
            CreepSpawns.Add(new TilePosition(new Point(9, 3), GridTileManager));

            // On-Level
            CreepSpawns.Add(new TilePosition(new Point(3, 6), GridTileManager));
            CreepSpawns.Add(new TilePosition(new Point(9, 6), GridTileManager));

            // Below
            CreepSpawns.Add(new TilePosition(new Point(3, 9), GridTileManager));
            CreepSpawns.Add(new TilePosition(new Point(6, 9), GridTileManager));
            CreepSpawns.Add(new TilePosition(new Point(9, 9), GridTileManager));
            CreepHandler.OnCreepSpawnTimeout += HandleCreepSpawnTimeout;
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
            GrassTexture = Content.Load<Texture2D>("Tiles/Grass-Bordered");

            // Load in the heart for life
            HeartTexture = Content.Load<Texture2D>("Images/Heart");

            TowerPlaceholders = new Spritesheet(Content.Load<Texture2D>("Towers/Tower-Base"));

            CursorImage = Content.Load<Texture2D>("Images/Pointer");

            DebugSkin = Content.Load<Texture2D>("Images/DebugSkin");

            Overlay = Content.Load<Texture2D>("Tiles/Overlay");

            // Creeps
            BlobCreepSheet = Content.Load<Texture2D>("Creeps/Blob");
        }

        public void HandleCreepSpawnTimeout(CreepHandler handler, EventArgs args)
        {

                BlobCreep NewCreep = new BlobCreep(new Spritesheet(BlobCreepSheet), (TilePosition) CreepSpawns[Randomizer.Next(0, CreepSpawns.Count())].Clone());
                NewCreep.UnlocalizedName = "BlobCreep." + CreepHandler.Entities.Count();
                NewCreep.MovementDirection = Direction.South;
                NewCreep.Goal = (TilePosition)CreepGoals[Randomizer.Next(0, CreepGoals.Count())].Clone();

                NewCreep.OnGoalAchieved += HandleCreepGoalAchieved;

                CreepHandler.AddCreep(NewCreep, GridTileManager);
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

            if (cheatcode.ToLower().Equals("millionare"))
            {
                Money = 1000000;
            }
            else
            {
                // Bad cheat code
            }

            CurrentState = PreviousState;
            PreviousState = GameState.CheatScreen;
        }

        public void HandleCreepGoalAchieved(Creep sender, EventArgs args)
        {
            this.Life -= 1;
            sender.Damage(1000);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            newIS.Update();

            TimeSinceControllerMove = TimeSinceControllerMove.Add(gameTime.ElapsedGameTime);

            if((CurrentState == GameState.Editing || CurrentState == GameState.Playing) && 
                (newIS.controllers[0].Buttons.Back == ButtonState.Pressed || KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Escape)))
                    this.Exit();

            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
            {
                PreviousState = GameState.Editing;
                CurrentState = GameState.CheatScreen;
            }

            #region State Handling
            switch (CurrentState){
                case GameState.CheatScreen:

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
                    break;

                case GameState.Editing:
                    #region Cursor Moving
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.W) || newIS.controllers[0].ThumbSticks.Left.Y > 0)
                        CursorLocation.Reposition(Direction.North);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.A) || newIS.controllers[0].ThumbSticks.Left.X < 0)
                        CursorLocation.Reposition(Direction.West);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.S) || newIS.controllers[0].ThumbSticks.Left.Y < 0)
                        CursorLocation.Reposition(Direction.South);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.D) || newIS.controllers[0].ThumbSticks.Left.X > 0)
                        CursorLocation.Reposition(Direction.East);

                    #endregion

                    #region Tower Placing
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Delete) || (oldIS.controllers[0].Buttons.Y == ButtonState.Released && newIS.controllers[0].Buttons.Y == ButtonState.Pressed)) {
                        String TileTypeAtCursor = GridTileManager.GetTileType(CursorLocation.GridLocation);
                        if (!TileTypeAtCursor.Equals("Tile.Grass") || !TileTypeAtCursor.Equals("Null")) {
                            Money += TilePrices[TileTypeAtCursor];
                            GridTileManager.ReplaceTile(new Tile(GrassTexture, (TilePosition) CursorLocation.Clone(), "Tile.Grass"));
                        }
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Z) || (oldIS.controllers[0].Buttons.A == ButtonState.Released && newIS.controllers[0].Buttons.A == ButtonState.Pressed)) {
                        if (!GridTileManager.IsTileFilled(CursorLocation.GridLocation)) {
                            if (Money >= TilePrices["Tile.Tower.Turret"]) {
                                Money -= TilePrices["Tile.Tower.Turret"];
                                TileWithTower TurretTower = TowerFactory.CreateNewTurret(TowerPlaceholders, CursorLocation, GrassTexture);
                                GridTileManager.ReplaceTile(TurretTower);
                            }
                        } else {
                            // Tile already has something in it
                            Console.WriteLine("Tile is already filled at: " + CursorLocation.GridLocation.ToString());
                        }
                    }
                    #endregion
                    
                    #region Swapping State Testing
                    // We started the cheat code enterer
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
                    {
                        PreviousState = CurrentState;
                        CurrentState = GameState.CheatScreen;
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.P) || (oldIS.controllers[0].Buttons.Start == ButtonState.Released && newIS.controllers[0].Buttons.Start == ButtonState.Pressed))
                    {
                        PreviousState = CurrentState;
                        CurrentState = GameState.Playing;
                    
                    }
                    #endregion
                    break;

                case GameState.Playing:
                    GridTileManager.SetTileSize(new Point(50, 50));
                    GridTileManager.Update(gameTime);
                    EntityHandler.Update(gameTime);
                    CreepHandler.Update(gameTime);

                    // Move from playing to editing
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.P) || 
                        (oldIS.controllers[0].Buttons.Start == ButtonState.Released && newIS.controllers[0].Buttons.Start == ButtonState.Pressed))
                    {
                        
                        CreepHandler.RemoveAllEntities();
                        GridTileManager.ResetTowers();

                        GridTileManager.SetTileSize(new Point(42, 42));

                        PreviousState = CurrentState;
                        CurrentState = GameState.Editing;
                    
                    }

                    break;

                default:
                    // Run any extra code here.
                    break;

            }
            #endregion

            if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.End))
                Initialize();

            oldIS.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="time">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime time)
        {
            // 56, 70, 19 is the grass' main color
            GraphicsDevice.Clear(new Color(56, 70, 19));

            // Background - Displaying the tiles
            GridTileManager.Draw(time, canvas, CurrentState == GameState.Playing);

            // Foreground - Debug Information and Onscreen text.
            canvas.Begin();

            switch(CurrentState){

                case GameState.Editing:
                    // Draw the cursor.
                    canvas.Draw(
                        CursorImage,
                        new Rectangle(
                            (int)CursorLocation.GetDrawingLocation().X + Grid_Tile_Size.X / 2,
                            (int)CursorLocation.GetDrawingLocation().Y + Grid_Tile_Size.Y / 2,
                            CursorImage.Bounds.Width,
                            CursorImage.Bounds.Height),
                        new Rectangle(0, 0, 25, 27),
                        Color.White,
                        0,
                        new Vector2(10, 0),
                        SpriteEffects.None,
                        (42 / 50));

                    foreach (Tile tile in GridTileManager.Tiles)
                    {

                        if (tile.GetType().Equals(typeof(TileWithTower)))
                        {
                            Tower t = ((TileWithTower)tile).TowerObject;
                            foreach (TilePosition turretTargetTile in t.TilesInRange)
                            {
                                canvas.Draw(Overlay, turretTargetTile.GetTileDrawingBounds(), Color.Pink);
                            }
                        }
                    }

                    #region Shop Display
                    // Money amount display
                    Vector2 MoneySize = Segoe.MeasureString(String.Format("{0:C}", Money));
                    canvas.DrawString(Segoe, String.Format("{0:C}", Money), new Vector2(Window.ClientBounds.Width - MoneySize.X - 10, 0), Color.White);

                    // Display tower 1
                    Rectangle Tower1Pos = new Rectangle(Window.ClientBounds.Width - 95, 11 + GridTileManager.TileSize.Y, 25, 19);
                    canvas.Draw(
                        TowerPlaceholders.GetSheet(), 
                        Tower1Pos, 
                        new Rectangle(0, 0, 25, 19), 
                        Color.White, 
                        MathHelper.ToRadians(90), 
                        Vector2.Zero, 
                        SpriteEffects.None, 
                        1f);

                    canvas.DrawString(Segoe, String.Format("Z/X ({0})", TilePrices["Tile.Tower.Turret"]), new Vector2(Tower1Pos.X + 2, Tower1Pos.Y - 11), Color.White);

                    
                    // Display tower 2
                    Rectangle Tower2Pos = new Rectangle(Window.ClientBounds.Width - 95, 11 + (GridTileManager.TileSize.Y * 2), 25, 19);
                    canvas.Draw(
                        TowerPlaceholders.GetSheet(), 
                        Tower2Pos, 
                        new Rectangle(0, 0, 25, 19), 
                        Color.LightBlue, 
                        MathHelper.ToRadians(90), 
                        Vector2.Zero, 
                        SpriteEffects.None, 
                        1f);

                    canvas.DrawString(Segoe, String.Format("Z/X ({0})", TilePrices["Tile.Tower.Turret"]), new Vector2(Tower2Pos.X + 2, Tower2Pos.Y - 11), Color.White);
                    #endregion
                    break;

                case GameState.CheatScreen:
                    canvas.DrawString(Segoe, CheatCodeSystem.GetDisplay(), new Vector2(10, Window.ClientBounds.Height - 30), Color.White);
                    break;

                case GameState.Playing:

                    Vector2 LifePosition = new Vector2(Window.ClientBounds.Width - 60, Window.ClientBounds.Height - 60);
                    canvas.Draw(HeartTexture, new Rectangle((int) LifePosition.X, (int) LifePosition.Y, 40, 40), Color.White);

                    Vector2 LifeSize = Segoe.MeasureString(Life.ToString());

                    Color LifeColor = Color.White;
                    if (Life > 35) LifeColor = Color.LimeGreen;
                    if (Life > 15 && Life <= 35) LifeColor = Color.Yellow;
                    if (Life > 0 && Life <= 15) LifeColor = Color.Salmon;
                    canvas.DrawString(Segoe, 
                        Life.ToString(), 
                        new Vector2(LifePosition.X + 20, LifePosition.Y + 20), 
                        LifeColor, 
                        0, 
                        new Vector2(LifeSize.X / 2, LifeSize.Y / 2),
                        1f,
                        SpriteEffects.None,
                        1f);
                    break;

            }

            

            if(DebugMode)
            {
                // Show start and stop positions for creeps
                foreach (TilePosition spawn in CreepSpawns)
                    canvas.Draw(Overlay, spawn.GetTileDrawingBounds(), Color.Green);

                foreach(TilePosition goal in CreepGoals)
                    canvas.Draw(Overlay, goal.GetTileDrawingBounds(), Color.Red);
            }

            canvas.End();

            EntityHandler.Draw(time, canvas, (DebugMode ? Segoe : null));
            CreepHandler.Draw(time, canvas, Segoe);
            base.Draw(time);
        }
    }
}
