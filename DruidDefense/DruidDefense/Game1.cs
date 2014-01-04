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

        // Tracks where the cursor is at over the board
        public TilePosition CursorLocation;
        Texture2D CursorImage;
        Texture2D Overlay;

        Point Grid_Tile_Size;

        CheatCodeManager CheatCodeSystem;

        GameState PreviousState;
        GameState CurrentState;

        float Money;
        Boolean NukeMode;

        float GridScale;

        Dictionary<String, float> TilePrices;

        Boolean DebugMode;
        
        TimeSpan ControllerMoveDelay;
        TimeSpan TimeSinceControllerMove;

        TilePosition CreepStart;
        TilePosition CreepEnd;

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
            TilePrices.Add("Tile.Tower.Tower3", 25);

            DebugMode = false;

            ControllerMoveDelay = new TimeSpan(0, 0, 0, 0, 500);
            TimeSinceControllerMove = new TimeSpan();

            CreepHandler = new Creeps.CreepHandler(50);
            CreepHandler.SpawnDelay = new TimeSpan(0, 0, 2);

            //new Point(Randomizer.Next(0, GridTileManager.GridSize.X), 0)
            CreepStart = new TilePosition(new Point(3, 0), GridTileManager);

            CreepEnd = new TilePosition(new Point(Randomizer.Next(0, GridTileManager.GridSize.X), GridTileManager.GridSize.Y - 1), GridTileManager);

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

            TowerPlaceholders = new Spritesheet(Content.Load<Texture2D>("Towers/Tower-Base"));

            CursorImage = Content.Load<Texture2D>("Images/Pointer");

            DebugSkin = Content.Load<Texture2D>("Images/DebugSkin");

            Overlay = Content.Load<Texture2D>("Tiles/Overlay");

            // Creeps
            BlobCreepSheet = Content.Load<Texture2D>("Creeps/Blob");
        }

        public void HandleCreepSpawnTimeout(CreepHandler handler, EventArgs args)
        {


            Direction RandomDirectionForCreep = PossibleDirections[Randomizer.Next(0, PossibleDirections.Count() - 1)];


            BlobCreep NewCreep = new BlobCreep(new Spritesheet(BlobCreepSheet), (TilePosition) CreepStart.Clone());
            NewCreep.UnlocalizedName = "BlobCreep." + CreepHandler.Entities.Count();
            NewCreep.MovementDirection = RandomDirectionForCreep;
            NewCreep.SpriteSystem.GetAnimation("Still").GetFrame(0).Coloration = new Color(Randomizer.Next(0, 255), Randomizer.Next(0, 255), Randomizer.Next(0, 255));

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
            else if(cheatcode.ToLower().Equals("immune"))
            {
                Console.WriteLine("Immunity activated.");
            }
            else if (cheatcode.ToLower().Equals("orbitnuke"))
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
                        CursorLocation.Move(Direction.North);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.A) || newIS.controllers[0].ThumbSticks.Left.X < 0)
                        CursorLocation.Move(Direction.West);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.S) || newIS.controllers[0].ThumbSticks.Left.Y < 0)
                        CursorLocation.Move(Direction.South);

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.D) || newIS.controllers[0].ThumbSticks.Left.X > 0)
                        CursorLocation.Move(Direction.East);

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

                    // TODO: Remove.
                    #region Manual Creep Movement. Remove when done.
                    if(KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Up)){
                        ((Creep) CreepHandler.GetEntity(0)).ChangeMovement(Direction.North, 1);
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Down))
                    {
                        ((Creep)CreepHandler.GetEntity(0)).ChangeMovement(Direction.South, 1);
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Left))
                    {
                        ((Creep)CreepHandler.GetEntity(0)).ChangeMovement(Direction.West, 1);
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Right))
                    {
                        ((Creep)CreepHandler.GetEntity(0)).ChangeMovement(Direction.East, 1);
                    }
                    #endregion

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
                        GridScale);

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
                    Vector2 Tower1Pos = new Vector2(Window.ClientBounds.Width - 50 - 70, 30);
                    canvas.Draw(TowerPlaceholders.GetSheet(), Tower1Pos, new Rectangle(0, 0, 50, 50), Color.White);
                    canvas.DrawString(Segoe, "Z/X", Tower1Pos + new Vector2(2, 2), Color.White);
                    canvas.DrawString(Segoe, String.Format("{0}", TilePrices["Tile.Tower.Turret"]), Tower1Pos + new Vector2(2, 16), Color.White);

                    // Money amount display
                    Vector2 MoneySize = Segoe.MeasureString(String.Format("{0:C}", Money));
                    canvas.DrawString(Segoe, String.Format("{0:C}", Money), new Vector2(Window.ClientBounds.Width - MoneySize.X - 10, 0), Color.White);
                    #endregion
                    break;

                case GameState.CheatScreen:
                    canvas.DrawString(Segoe, CheatCodeSystem.GetDisplay(), new Vector2(10, Window.ClientBounds.Height - 30), Color.White);
                    break;

                case GameState.Playing:

                    // Add playing-specific code here.
                    break;

            }

            

            if(DebugMode)
            {
                // Show start and stop positions for creeps
                canvas.Draw(Overlay, CreepStart.GetTileDrawingBounds(), Color.Green);
                canvas.Draw(Overlay, CreepEnd.GetTileDrawingBounds(), Color.Red);
            }

            canvas.End();

            EntityHandler.Draw(time, canvas, (DebugMode ? Segoe : null));
            CreepHandler.Draw(time, canvas, Segoe);
            base.Draw(time);
        }
    }
}
