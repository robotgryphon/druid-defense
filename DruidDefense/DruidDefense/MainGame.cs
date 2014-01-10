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
    public class MainGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch canvas;

        InputState oldIS;
        InputState newIS;

        SpriteFont Segoe;
        
        DefenseTileManager GridTileManager;
        EntityHandler EntityHandler;
        
        Spritesheet TurretSpritesheet;
        Texture2D GrassTexture;
        Texture2D HeartTexture;
        Texture2D Overlay;
        Texture2D Hole;

        // Tracks where the cursor is at over the board
        TilePosition CursorLocation;
        Texture2D CursorImage;

        CheatCodeManager CheatCodeSystem;

        GameState PreviousState;
        GameState CurrentState;

        float Money;
        float Life;

        Dictionary<TowerFactory.TowerType, InputBinding> TowerInputBinding;
        Dictionary<String, float> TilePrices;

        Boolean DebugMode;
        
        TimeSpan ControllerMoveDelay;
        TimeSpan TimeSinceControllerMove;

        Texture2D CreepTextures;
        
        CreepHandler CreepHandler;
        List<TilePosition> CreepSpawns;
        List<TilePosition> CreepGoals;

        public static Random Randomizer;

        public MainGame()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
            graphics.ApplyChanges();

            Window.Title = "Druid Defenders";

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Initialize all the variables to their default settings.
        /// Setup ALL the things!
        /// </summary>
        protected override void Initialize()
        {

            base.Initialize();

            Randomizer = new Random();

            // Initialize the Input states for catching user input
            oldIS = new InputState().Update();
            newIS = new InputState().Update();

            // Initialize the tile manager (grid system)
            GridTileManager = new DefenseTileManager(new Point(16, 12), new Point(42, 42));

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
            CheatCodeSystem = new CheatCodeManager(10, CaptureSource.Keyboard, new TimeSpan(0, 0, 0, 0, 200));
            CheatCodeSystem.CheatCodeSubmitted += CheatCodeFinished;

            // Give the player some money to work with.
            Money = 300f;
            Life = 1f;

            // Start the game proper, in editing mode.
            PreviousState = GameState.Loading;
            CurrentState = GameState.Editing;

            TilePrices = new Dictionary<string, float>();
            TilePrices.Add("Tile.Grass", 0);
            TilePrices.Add("Tile.Tower." + TowerFactory.TowerType.Small.ToString(), 10);
            TilePrices.Add("Tile.Tower." + TowerFactory.TowerType.Medium.ToString(), 25);
            TilePrices.Add("Tile.Tower." + TowerFactory.TowerType.Large.ToString(), 50);

            TowerInputBinding = new Dictionary<TowerFactory.TowerType, InputBinding>();
            TowerInputBinding.Add(TowerFactory.TowerType.Small, new InputBinding(Keys.Z, Buttons.X));
            TowerInputBinding.Add(TowerFactory.TowerType.Medium, new InputBinding(Keys.X, Buttons.A));
            TowerInputBinding.Add(TowerFactory.TowerType.Large, new InputBinding(Keys.C, Buttons.B));

            DebugMode = false;

            ControllerMoveDelay = new TimeSpan(0, 0, 0, 0, 250);
            TimeSinceControllerMove = new TimeSpan();

            CreepHandler = new Creeps.CreepHandler(100);
            CreepHandler.SpawnDelay = new TimeSpan(0, 0, 0, 2, 0);

            CreepGoals = new List<TilePosition>();
            CreepGoals.Add(new TilePosition(new Point(Randomizer.Next(0, GridTileManager.GridSize.X), GridTileManager.GridSize.Y - 1), GridTileManager));

            List<int> CreepSpawnXs = new List<int>();
            CreepSpawns = new List<TilePosition>();

            while(CreepSpawnXs.Count() < 3){
                int RandomSpawnX = Randomizer.Next(0, GridTileManager.GridSize.X);
                if(CreepSpawnXs.Contains(RandomSpawnX)){
                    // Can not add, spawn already exists
                } else {
                    // Spawn does not exist yet, add it
                    CreepSpawnXs.Add(RandomSpawnX);
                    CreepSpawns.Add(new TilePosition(new Point(RandomSpawnX, 0), GridTileManager));
                }
            }
            

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
            GrassTexture = Content.Load<Texture2D>("Tiles/Grass");

            // Load in the heart for life
            HeartTexture = Content.Load<Texture2D>("Images/Heart");

            TurretSpritesheet = new Spritesheet(Content.Load<Texture2D>("Towers/Tower-Base"));

            CursorImage = Content.Load<Texture2D>("Images/Pointer");

            Overlay = Content.Load<Texture2D>("Images/Overlay");

            // Creeps
            CreepTextures = Content.Load<Texture2D>("Creeps/Blob");

            Hole = Content.Load<Texture2D>("Images/Hole");
        }

        public void HandleCreepSpawnTimeout(CreepHandler handler, EventArgs args)
        {

                BlobCreep NewCreep = new BlobCreep(new Spritesheet(CreepTextures), (TilePosition) CreepSpawns[Randomizer.Next(0, CreepSpawns.Count())].Clone());
                NewCreep.UnlocalizedName = "BlobCreep." + CreepHandler.Entities.Count();
                NewCreep.MovementDirection = Direction.South;
                NewCreep.Goal = (TilePosition)CreepGoals[Randomizer.Next(0, CreepGoals.Count())].Clone();

                NewCreep.OnGoalAchieved += HandleCreepGoalAchieved;

                NewCreep.OnCreepDeath += HandleCreepDeath;

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

        public void HandleCreepDeath(Creep sender, EventArgs args)
        {
            if(!sender.Location.Equals(sender.Goal))
                this.Money += Randomizer.Next(1, 10);
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

            if (Life <= 0)
            {
                CurrentState = GameState.GameOver;
            }

            TimeSinceControllerMove = TimeSinceControllerMove.Add(gameTime.ElapsedGameTime);

            if(CurrentState != GameState.CheatScreen && (newIS.controllers[0].Buttons.Back == ButtonState.Pressed || KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Escape)))
                    this.Exit();

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
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.W) || newIS.controllers[0].ThumbSticks.Left.Y > 0 && TimeSinceControllerMove > ControllerMoveDelay)
                    {
                        CursorLocation.Reposition(Direction.North);
                        TimeSinceControllerMove = new TimeSpan();
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.A) || newIS.controllers[0].ThumbSticks.Left.X < 0 && TimeSinceControllerMove > ControllerMoveDelay)
                    {
                        CursorLocation.Reposition(Direction.West);
                        TimeSinceControllerMove = new TimeSpan();
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.S) || newIS.controllers[0].ThumbSticks.Left.Y < 0 && TimeSinceControllerMove > ControllerMoveDelay)
                    {
                        CursorLocation.Reposition(Direction.South);
                        TimeSinceControllerMove = new TimeSpan();
                    }

                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.D) || newIS.controllers[0].ThumbSticks.Left.X > 0 && TimeSinceControllerMove > ControllerMoveDelay)
                    {
                        CursorLocation.Reposition(Direction.East);
                        TimeSinceControllerMove = new TimeSpan();
                    }

                    #endregion
    
                    // Remove placed towers
                    if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.Delete) || (oldIS.controllers[0].Buttons.Y == ButtonState.Released && newIS.controllers[0].Buttons.Y == ButtonState.Pressed)) {
                        String TileTypeAtCursor = GridTileManager.GetTileType(CursorLocation.GridLocation);
                        if (!TileTypeAtCursor.Equals("Tile.Grass") || !TileTypeAtCursor.Equals("Null")) {
                            Money += TilePrices[TileTypeAtCursor];
                            GridTileManager.ReplaceTile(new Tile(GrassTexture, (TilePosition) CursorLocation.Clone(), "Tile.Grass"));
                        }
                    }

                    foreach (KeyValuePair<TowerFactory.TowerType, InputBinding> towerKeybind in TowerInputBinding)
                    {
                        if (KeyboardHelper.WasKeyJustPressed(oldIS, newIS, towerKeybind.Value.key) ||
                            newIS.controllers[0].IsButtonDown(towerKeybind.Value.button) && oldIS.controllers[0].IsButtonUp(towerKeybind.Value.button))
                        {
                            if (!GridTileManager.IsTileFilled(CursorLocation.GridLocation))
                            {
                                if (Money >= TilePrices["Tile.Tower." + towerKeybind.Key.ToString()])
                                {
                                    Money -= TilePrices["Tile.Tower." + towerKeybind.Key.ToString()];
                                    TileWithTower TurretTower = TowerFactory.CreateNewTower(TurretSpritesheet, CursorLocation, GrassTexture, towerKeybind.Key);
                                    GridTileManager.ReplaceTile(TurretTower);
                                }
                            }
                            else
                            {
                                // Tile already has something in it
                                Console.WriteLine("Tile is already filled at: " + CursorLocation.GridLocation.ToString());
                            }
                        }
                    }
                    
                    #region Swapping State

                    if ((CurrentState != GameState.GameOver) && KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
                    {
                        PreviousState = GameState.Editing;
                        CurrentState = GameState.CheatScreen;
                    }

                    // We started the cheat code enterer
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

                    if ((CurrentState != GameState.GameOver) && KeyboardHelper.WasKeyJustPressed(oldIS, newIS, Keys.OemTilde))
                    {
                        PreviousState = GameState.Playing;
                        CurrentState = GameState.CheatScreen;
                    }

                    break;

                case GameState.GameOver:
                    if(newIS.keyboard.GetPressedKeys().Count() > 0)
                        Initialize();
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

            Color TileColor = new Color(56, 70, 19);

            // 56, 70, 19 is the grass' main color
            GraphicsDevice.Clear(CurrentState == GameState.GameOver ? Color.Black : TileColor);

            Vector2 MoneySize = Segoe.MeasureString(String.Format("{0:C}", Money));

            if (CurrentState != GameState.GameOver)
            {
                // Background - Displaying the tiles
                GridTileManager.Draw(time, canvas, CurrentState == GameState.Playing);

                canvas.Begin();
                foreach (TilePosition spawn in CreepSpawns)
                {
                    canvas.Draw(Hole, spawn.GetTileDrawingBounds(), Color.LightGreen);
                }


                foreach (TilePosition goal in CreepGoals)
                {
                    canvas.Draw(Hole, goal.GetTileDrawingBounds(), Color.Red);
                }

                if (DebugMode)
                {
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
                }
                canvas.End();

                EntityHandler.Draw(time, canvas, (DebugMode ? Segoe : null));
                CreepHandler.Draw(time, canvas, Segoe);
            }

            // Foreground - Debug Information and Onscreen text.
            canvas.Begin();

            switch(CurrentState){

                case GameState.Editing:
                    // Draw the cursor.

                    GridTileManager.DrawTowerRanges(canvas, Overlay);

                    canvas.Draw(
                        CursorImage,
                        new Rectangle(
                            (int)CursorLocation.GetDrawingLocation().X + GridTileManager.TileSize.X / 2,
                            (int)CursorLocation.GetDrawingLocation().Y + GridTileManager.TileSize.Y / 2,
                            CursorImage.Bounds.Width,
                            CursorImage.Bounds.Height),
                        new Rectangle(0, 0, 25, 27),
                        Color.White,
                        0,
                        new Vector2(10, 0),
                        SpriteEffects.None,
                        (42 / 50));

                    canvas.DrawString(Segoe, String.Format("{0:C}", Money), new Vector2(Window.ClientBounds.Width - MoneySize.X - 10, 0), Color.White);

                    foreach (TowerFactory.TowerType type in Enum.GetValues(typeof(TowerFactory.TowerType)))
                    {
                        Frame TowerImage = TowerFactory.CreateTowerImage(type, TurretSpritesheet);
                        TowerImage.Draw(time, canvas, TurretSpritesheet, new Vector2(Window.ClientBounds.Width - 125, 11 + (GridTileManager.TileSize.Y * (int) type)));

                        canvas.DrawString(Segoe, String.Format("({0:C})", TilePrices["Tile.Tower." + type.ToString()]), new Vector2(Window.ClientBounds.Width + TowerImage.Area.Width - 120, GridTileManager.TileSize.Y * (int) type + 5), Color.White);
                    }

                    break;

                case GameState.CheatScreen:
                    canvas.DrawString(Segoe, CheatCodeSystem.GetDisplay(), new Vector2(10, Window.ClientBounds.Height - 30), Color.White);
                    break;

                case GameState.Playing:

                    Rectangle MoneyBackdrop = new Rectangle(
                        (int) Window.ClientBounds.Width - (int) MoneySize.X - 24, 
                        0, 
                        (int) MoneySize.X + 22, 
                        (int) MoneySize.Y + 6);

                    canvas.Draw(Overlay, MoneyBackdrop, new Rectangle(0, 0, 1, 1), Color.Black);
                    canvas.DrawString(Segoe, string.Format("{0:c}", Money), new Vector2(MoneyBackdrop.X + (MoneyBackdrop.Width - MoneySize.X - 4), MoneyBackdrop.Y + 2), Color.White);

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

                case GameState.GameOver:
                    canvas.DrawString(Segoe, "GAME OVER!", new Vector2(10, 10), Color.Red, 0, Vector2.Zero, 2f, SpriteEffects.None, 1f);

                    Vector2 LineHeight = new Vector2(0, Segoe.MeasureString("M").Y + 4);

                    Vector2 curPos = new Vector2(10, 50);
                    canvas.DrawString(Segoe, "Programmer: ", curPos, Color.White);
                    canvas.DrawString(Segoe, "Ted Senft", curPos + new Vector2(Segoe.MeasureString("Programmer: ").X, 0), Color.LightBlue);

                    curPos += LineHeight;
                    canvas.DrawString(Segoe, "Graphics: ", curPos, Color.White);
                    canvas.DrawString(Segoe, "Some Person", curPos + new Vector2(Segoe.MeasureString("Graphics: ").X, 0), Color.LightBlue);

                    canvas.DrawString(Segoe, "Press any key to restart.",
                        new Vector2(
                            Window.ClientBounds.Width - Segoe.MeasureString("Press any key to restart.").X - 8,
                            Window.ClientBounds.Height - Segoe.MeasureString("Press any key to restart.").Y - 8),
                        Color.Green);

                    break;

                    
            }

            canvas.End();

            base.Draw(time);
        }
    }
}