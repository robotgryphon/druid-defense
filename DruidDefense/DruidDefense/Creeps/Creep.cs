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
            if(this.Data.ContainsKey(key))
                this.Data.Remove(key);
        }

    }

    public class Creep : TileEntity, ICloneable 
    {

        public delegate void CreepEvent(Creep sender, EventArgs arguments);

        public event CreepEvent OnTileBoundaryOverlap;

        public event CreepEvent OnCreepDeath;

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

        public Direction NextDirectionToGo;

        protected float MovementLeft;

        protected float Health;

        public Creep(Spritesheet sheet, TilePosition startPosition) : this(sheet, startPosition, 100f) { }

        /// <summary>
        /// Create a new creep entity.
        /// </summary>
        public Creep(Spritesheet sheet, TilePosition startPosition, float health) : base(sheet, startPosition) {
            Speed = new Vector2(1f, 1f);

            // X = Left/Right, Y = Up/Down
            MovementDirection = Direction.North;

            NextDirectionToGo = Direction.South;

            LocalizedName = "Entity.TileEntity.Creep";

            EntityType = Entities.Enemy;

            base.RecalculateHitbox();

            alive = true;

            MovementLeft = 1000;

            this.Health = health;

        }

        public Direction SetNextRandomDirection(){
            // Console.WriteLine(Game1.PossibleDirections.Length);
            Direction NewRandomDirection = Game1.PossibleDirections[Game1.Randomizer.Next(0, Game1.PossibleDirections.Length)];



            return NewRandomDirection;
        }

        #region Moving
        public virtual void ChangeMovement(Direction direction, int movementAmount)
        {
            this.NextDirectionToGo = direction;
            this.MovementLeft = movementAmount;
        }

        public void Move(Direction moveDirection)
        {

            switch (MovementDirection)
            {
                case Direction.North:
                    if (Location.GridLocation.Y > 0 || 
                        (Location.GridLocation.Y == 0 && Position.Y > (Location.GridManager.TileSize.Y / 2) ))
                    {
                        Position.Y -= Speed.Y;
                    }
                    else
                    {
                        MovementDirection = SetNextRandomDirection(); 
                    }
                    break;

                case Direction.South:
                    if ((Location.GridLocation.Y < Location.GridManager.GridSize.Y - 1) || 
                        (Location.GridLocation.Y == Location.GridManager.GridSize.Y - 1 && Position.Y < (Location.GridManager.TileSize.Y / 2)))
                    {
                        Position.Y += Speed.Y;
                    }
                    else
                    {
                        MovementDirection = SetNextRandomDirection();
                            
                    }

                    break;

                case Direction.West:
                    if (Location.GridLocation.X > 0 || 
                        (Location.GridLocation.X == 0 && Position.X > Location.GridManager.TileSize.X / 2))
                    {
                        Position.X -= Speed.X;
                    }
                    else
                    {
                        MovementDirection = SetNextRandomDirection();  
                    }
                    break;

                case Direction.East:
                    if (Location.GridLocation.X < Location.GridManager.GridSize.X - 1 || 
                        (Location.GridLocation.X  == Location.GridManager.GridSize.X - 1 && Position.X < (Location.GridManager.TileSize.X / 2)))
                    {
                        Position.X += Speed.X;
                    }
                    else
                    {
                        MovementDirection = SetNextRandomDirection();
                    }

                    break;
            }
        }
        #endregion

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
            Rectangle CreepTileBounds = Location.GetTileDrawingBounds();
            CreepTileBounds.X = (int)Location.GridLocation.X * Location.GridManager.TileSize.X;
            CreepTileBounds.Y = (int)Location.GridLocation.Y * Location.GridManager.TileSize.Y;

            Direction CreepDirection = LocationHelper.CheckBounds(Position + Location.GetDrawingLocation(), CreepTileBounds);

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
                // If the creep is still in its tile.
                Point CreepLocationInSquare = new Point((int)Position.X, (int)Position.Y);
                if (CreepLocationInSquare == TilePosition.TileCenter)
                {
                    if (MovementLeft > 0)
                    {
                        this.MovementDirection = NextDirectionToGo;
                        NextDirectionToGo = SetNextRandomDirection();

                        if (MovementLeft == 1)
                        {
                            MovementLeft = 0.9f;
                        }
                        else
                        {
                            MovementLeft--;
                        }
                    }
                    else if (MovementLeft < 0)
                    {
                        MovementLeft = 0;
                    }
                }

                // Creep is still inside its bounds.
                if (MovementLeft > 0)
                {
                    Move(MovementDirection);

                }
            }

            base.Update(time);
        }

        public override void Draw(GameTime time, SpriteBatch canvas)
        {
            base.Draw(time, canvas, Location.GetDrawingLocation() + Position);
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
            return String.Format("{1}", this.LocalizedName, this.Health);
        }
    }
}
