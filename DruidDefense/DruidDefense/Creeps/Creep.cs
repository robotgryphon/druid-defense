using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Graphics;
using Ostenvighx.Framework.Xna.Layout;
using Ostenvighx.Framework.Xna.Utilities;
using Ostenvighx.Framework.Xna.Entities;

namespace DruidDefense.Creeps
{

    public class Creep : TileEntity, ICloneable 
    {

        public delegate void CreepEvent(Creep sender, EventArgs arguments);

        public event CreepEvent OnTileBoundaryOverlap;

        public event CreepEvent OnCreepDeath;

        public event CreepEvent OnGoalAchieved;

        /// <summary>
        /// The tile the creep is trying to get to.
        /// </summary>
        public TilePosition Goal;

        /// <summary>
        /// The horizontal and vertical movement speeds for this creep.
        /// </summary>
        public Vector2 Speed;

        /// <summary>
        /// The direction the creep is headed.
        /// </summary>
        public Direction MovementDirection;

        protected float MovementLeft;

        protected float Health;

        public Creep(Spritesheet sheet, TilePosition startPosition) : this(sheet, startPosition, 100f) { }

        /// <summary>
        /// Create a new creep entity.
        /// </summary>
        public Creep(Spritesheet sheet, TilePosition startPosition, float health) : base(sheet, startPosition) {
            Speed = new Vector2(1f, 1f);

            // X = Left/Right, Y = Up/Down
            MovementDirection = Direction.South;

            UnlocalizedName = "Entity.TileEntity.Creep";

            EntityType = Entities.Enemy;

            base.RecalculateHitbox();

            alive = true;

            MovementLeft = 1;

            this.Health = health;

        }

        public Boolean IsGoalAchieved()
        {
            if(this.Location.Equals(Goal)){

                Rectangle PositionAsRect = new Rectangle(HitBox.Center.X - 2, HitBox.Center.Y - 2, 5, 5);
                Point GoalCenter = Goal.GetTileDrawingBounds().Center;
                Rectangle GoalCenter2 = new Rectangle(GoalCenter.X - 2, GoalCenter.Y - 2, 5, 5);
                Rectangle IntersectTest = Rectangle.Intersect(PositionAsRect, GoalCenter2);

                if(new Rectangle(0, 0, IntersectTest.Width, IntersectTest.Height).Equals(new Rectangle(0, 0, 5, 5))){
                    if (this.OnGoalAchieved != null)
                    {
                        OnGoalAchieved(this, new CreepMovementArguments((TilePosition)this.Location.Clone()));
                    }

                    return true;
                }
                
            }

            return false;
        }

        public void Move(Direction moveDirection, Vector2 Speed)
        {

            switch (MovementDirection)
            {
                case Direction.North:
                    if (Location.GridLocation.Y >= 0)
                    {
                        Position.Y -= Speed.Y;
                    }
                    break;

                case Direction.South:
                    if (Location.GridLocation.Y <= Location.GridManager.GridSize.Y)
                    {
                        Position.Y += Speed.Y;
                    }

                    break;

                case Direction.West:
                    if (Location.GridLocation.X >= 0)
                    {
                        Position.X -= Speed.X;
                    }
                    break;

                case Direction.East:
                    if (Location.GridLocation.X <= Location.GridManager.GridSize.X - 1)
                    {
                        Position.X += Speed.X;
                    }

                    break;
            }
        }

        public virtual void Damage(float amount)
        {
            this.Health -= amount;
            if (Health <= 0)
            {
                this.alive = false;
                if(this.OnCreepDeath != null)
                    OnCreepDeath(this, EventArgs.Empty);
            }
        }

        public override void Update(GameTime time)
        {

            Move(MovementDirection, Speed);

            
            // If the creep is still in its tile.
            if (!IsGoalAchieved())
            {

                Rectangle CreepTileBounds = Location.GetTileDrawingBounds();

                Vector2 CheckLocation = Position + Location.GetDrawingLocation();

                Direction CreepDirection = LocationHelper.CheckBounds(CheckLocation, CreepTileBounds);

                if (CreepDirection != Direction.Inside)
                {
                    // If the creep left its tile (outside of the tile boundaries)
                    if (this.OnTileBoundaryOverlap != null)
                    {
                        CreepMovementArguments moveArgs = new CreepMovementArguments((TilePosition)Location.Clone());
                        moveArgs.AddData("side", CreepDirection);
                        OnTileBoundaryOverlap(this, moveArgs);
                    }
                }
                else
                {

                    #region Determining direction to move
                    Direction RelationToGoal = LocationHelper.CheckBounds(this.Location.GetDrawingLocation() + Position, Goal.GetTileDrawingBounds());


                    // Determine if the creep is fully inside its tile yet
                    Rectangle FullyInside = Rectangle.Intersect(this.HitBox, this.Location.GetTileDrawingBounds());
                    Rectangle FullyInside2 = new Rectangle(0, 0, FullyInside.Width, FullyInside.Height);

                    switch (RelationToGoal)
                    {
                        case Direction.NorthWest:
                        case Direction.North:
                        case Direction.NorthEast:
                            this.MovementDirection = Direction.South;
                            break;

                        case Direction.SouthWest:
                        case Direction.South:
                        case Direction.SouthEast:
                            this.MovementDirection = Direction.North;
                            break;

                            
                        case Direction.West:
                            // We are to the left of the goal                            
                            if (FullyInside2.Height == HitBox.Height)
                            {
                                MovementDirection = Direction.East;
                            }
                            else
                            {

                                Point GoalCenter = Goal.GetTileDrawingBounds().Center;
                                Direction DirToCenter = LocationHelper.CheckBounds(this.Position, new Rectangle(GoalCenter.X, GoalCenter.Y, 1, 1));

                                if ((Location.GetDrawingLocation() + Position).Y > GoalCenter.Y)
                                {
                                    MovementDirection = Direction.North;
                                }
                                else
                                {
                                    MovementDirection = Direction.South;
                                }
                                
                            }
                            break;

                        case Direction.East:
                            if (FullyInside2.Height == HitBox.Height)
                            {
                                MovementDirection = Direction.West;
                            }
                            else
                            {
                                Point GoalCenter = Goal.GetTileDrawingBounds().Center;
                                Direction DirToCenter = LocationHelper.CheckBounds(this.Position, new Rectangle(GoalCenter.X, GoalCenter.Y, 1, 1));

                                if ((Location.GetDrawingLocation() + Position).Y > GoalCenter.Y)
                                {
                                    MovementDirection = Direction.North;
                                }
                                else
                                {
                                    MovementDirection = Direction.South;
                                }
                            }
                            break;

                        case Direction.Inside:
                            // Flail. Where the heck should we go?
                            if(this.OnGoalAchieved != null)
                                OnGoalAchieved(this, new CreepMovementArguments((TilePosition)this.Location.Clone()));
                            break;

                        default:
                            Console.WriteLine(RelationToGoal);
                            break;
                    }


                }
                #endregion

                
            }

            base.Update(time);
        }

        public override void Draw(GameTime time, SpriteBatch canvas)
        {
            base.Draw(time, canvas, Location.GetDrawingLocation() + Position);
        }

        public override void Draw(GameTime time, SpriteBatch canvas, Texture2D debugSkin)
        {
            base.Draw(time, canvas, Location.GetDrawingLocation() + Position);

            if (debugSkin != null)
            {
                canvas.Draw(
                    debugSkin, 
                    new Rectangle(
                        (int) (HitBox.X), 
                        (int) (HitBox.Y), 
                        HitBox.Width, 
                        HitBox.Height), 
                    Color.Red); 
            }
        }

        public override string ToString()
        {
            return "Entity.TileEntity.Creep: " + this.Location.ToString();
        }

        public override object Clone() {

            Creep NewCreep = new Creep((Spritesheet) this.Sprites.Clone(), (TilePosition) this.Location.Clone());

            NewCreep.SpriteSystem = (AnimationSystem) SpriteSystem.Clone();

            return NewCreep;
        }

        public override string GetDebugString()
        {
            return this.Health.ToString();
        }
    }

    public class CreepMovementArguments : EventArgs
    {

        public TilePosition location;
        public Dictionary<string, object> Data { get; protected set; }
        public DateTime Timestamp { get; protected set; }



        public CreepMovementArguments(TilePosition CreepLocation)
        {
            this.Data = new Dictionary<string, object>();
            this.Timestamp = DateTime.Now;
            this.location = CreepLocation;
        }

        public void AddData(string key, object data)
        {
            if (!this.Data.ContainsKey(key))
            {
                this.Data.Add(key, data);
            }
            else
            {
                throw new Exception("Key already exists in data.");
            }
        }

        public void RemData(string key)
        {
            if (this.Data.ContainsKey(key))
                this.Data.Remove(key);
        }

    }
}
