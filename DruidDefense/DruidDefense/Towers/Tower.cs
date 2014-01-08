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
using DruidDefense.Creeps;

namespace DruidDefense.Towers
{

    /// <summary>
    /// Base tile class.
    /// </summary>
    public class Tower
    {

        public Spritesheet TowerTextures;

        public Frame Placeholder;

        public AnimationSystem AnimationHandler;

        protected float MaxHealth;
        public float Health;

        public TimeSpan ReloadSpeed;
        public TimeSpan TimeSinceReload;

        public float TileRange;

        protected SortedList<String, TileEntity> Targets;

        public List<TilePosition> TilesInRange { get; protected set; }

        public TilePosition ParentTileLocation { get; protected set; }

        public float DamagePerShot;

        public Tower(TilePosition ParentTileLocation, Spritesheet TextureSheet, Rectangle DefaultDrawingArea)
            : this(ParentTileLocation, 100, 50, 1, TextureSheet, DefaultDrawingArea, new TimeSpan()) { }

        public Tower(TilePosition ParentTileLocation, float Health, Spritesheet TextureSheet, Rectangle DefaultDrawingArea) 
            : this(ParentTileLocation, Health, 50, 1, TextureSheet, DefaultDrawingArea, new TimeSpan()) { }

        public Tower(TilePosition ParentTileLocation, float Health, float Range, float dps, Spritesheet TextureSheet, Rectangle DefaultDrawingArea, TimeSpan ReloadSpeed)
        {
            this.TowerTextures = TextureSheet;

            Placeholder = new Frame(DefaultDrawingArea, 1f);

            AnimationHandler = new AnimationSystem(TowerTextures);

            AnimationHandler.AddAnimation("Editor").SetNextInQueue("Editor");
            AnimationHandler.GetAnimation("Editor").AddFrame(Placeholder);

            this.TileRange = Range;

            this.ReloadSpeed = ReloadSpeed;
            
            this.MaxHealth = Health;

            this.Reset();

            this.ParentTileLocation = ParentTileLocation;

            this.DamagePerShot = dps;
        }

        public virtual void Reset()
        {

            AnimationHandler.RotationOverride = true;
            AnimationHandler.Rotation = 90f;

            AnimationHandler.SwitchToAnimation("Editor");

            this.Health = MaxHealth;
            this.TimeSinceReload = new TimeSpan();

            this.Targets = new SortedList<String, TileEntity>();

            this.TilesInRange = new List<TilePosition>();
            
        }

        public virtual Boolean IsTileInRange(TilePosition tile)
        {
            return TilePosition.DistanceBetween(tile, this.ParentTileLocation) <= TileRange;
        }

        public virtual void CheckEntityInRange(TileEntity e)
        {

            if (Targets.ContainsKey(e.UnlocalizedName))
            {
                if(TilePosition.DistanceBetween(e.Location, this.ParentTileLocation) > TileRange){
                    Targets.Remove(e.UnlocalizedName);
                    // Console.WriteLine("Entity {0} removed from watch list of Tower at {1}.", e.LocalizedName, ParentTileLocation);
                }
            }
            else
            {
                if (TilePosition.DistanceBetween(e.Location, this.ParentTileLocation) <= TileRange)
                {
                    if (Targets.Count < 10)
                    {
                        Targets.Add(e.UnlocalizedName, e);
                        if (e.GetType().Equals(typeof(Creep)) || e.GetType().Equals(typeof(BlobCreep)))
                        {
                            ((Creep)e).OnCreepDeath += this.HandleTargetDeath;
                        }

                        // Console.WriteLine("Entity {0} added to watch list of Tower at {1}.", e.LocalizedName, ParentTileLocation)
                    }
                }
            }
        }

        public virtual void HandleTargetDeath(Creep sender, EventArgs args)
        {

            // Console.WriteLine("Creep {0} died. Removing from target listing.", sender.LocalizedName);
            this.Targets.Remove(sender.UnlocalizedName);
        }

        public virtual void FaceTarget(Vector2 targetLocation)
        {
            float TowerMovement = (float)MovementHelper.FaceObject(this.ParentTileLocation.GetDrawingLocation(), targetLocation);
            AnimationHandler.Rotation = TowerMovement;
        }

        /// <summary>
        /// Update this tower's target squares.
        /// </summary>
        /// <param name="ParentTileLocation">The Tower's Parent Tile's TilePosition.</param>
        public virtual void UpdateRange()
        {
            TilesInRange.Clear();

            for (int x = ParentTileLocation.GridLocation.X - (int)TileRange; x < ParentTileLocation.GridLocation.X + (int)TileRange + 1; x++)
            {
                for (int y = ParentTileLocation.GridLocation.Y - (int)TileRange; y < ParentTileLocation.GridLocation.Y + (int)TileRange + 1; y++)
                {
                    TilePosition possibleTileTarget = new TilePosition(new Point(x, y), ParentTileLocation.GridManager);
                    if (IsTileInRange(possibleTileTarget))
                        this.TilesInRange.Add(possibleTileTarget);
                }
            }
        }

        public virtual void Update(GameTime time) {

            this.TimeSinceReload = this.TimeSinceReload.Add(time.ElapsedGameTime);
            this.AnimationHandler.Rotation %= 360;

            if (this.Targets.Count() > 0)
            {
                TileEntity te = this.Targets.Values[0];
                FaceTarget(new Vector2(te.HitBox.Center.X, te.HitBox.Center.Y));
            }

            if (TimeSinceReload >= ReloadSpeed) {
                TimeSinceReload = new TimeSpan();

                if (this.Targets.Count() > 0)
                {
                    ((Creep)this.Targets.Values[0]).Damage((float)Math.Floor(Health / 10));
                }
            }

            this.UpdateRange();
        }

        public virtual void Draw(GameTime time, SpriteBatch canvas, TilePosition location)
        {
            AnimationHandler.Draw(time, canvas, location.GetDrawingLocation() + new Vector2(TilePosition.TileCenter.X, TilePosition.TileCenter.Y)); 
        }
    }
}
