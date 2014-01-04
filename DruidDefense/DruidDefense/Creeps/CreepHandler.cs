using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Ostenvighx.Framework.Xna.Entities;
using Ostenvighx.Framework.Xna.Utilities;
using Ostenvighx.Framework.Xna.Layout;
using DruidDefense.Tiles;
using Microsoft.Xna.Framework.Graphics;

namespace DruidDefense.Creeps
{
    public class CreepHandler : EntityHandler
    {

        public delegate void CreepSpawnEvent(CreepHandler sender, EventArgs args);

        public event CreepSpawnEvent OnCreepSpawnTimeout;

        public TimeSpan SpawnDelay;
        protected TimeSpan SpawnerCountdown;

        public CreepHandler()
            : base(25) {
                this.SpawnDelay = new TimeSpan(0, 0, 1);
                this.SpawnerCountdown = new TimeSpan();
        }

        public CreepHandler(int limit) 
            :base(limit) {
                this.SpawnDelay = new TimeSpan(0, 0, 1);
                this.SpawnerCountdown = new TimeSpan();
            }

        public CreepHandler(int limit, TimeSpan SpawnDelay)
            : base(limit)
        {
            this.SpawnDelay = SpawnDelay;
            this.SpawnerCountdown = new TimeSpan();
        }

        public void AddCreep(Creep creep, DefenseTileManager Grid)
        {
            if (!base.BufferFull())
            {
                creep.OnTileBoundaryOverlap += HandleCreepOverlap;
                creep.OnTileBoundaryOverlap += Grid.CheckTowerRanges;
                base.AddEntity(creep);
            }
        }

        public override void Update(GameTime time)
        {

            SpawnerCountdown = SpawnerCountdown.Add(time.ElapsedGameTime);
            if (SpawnerCountdown >= SpawnDelay)
            {

                if (this.OnCreepSpawnTimeout != null)
                    OnCreepSpawnTimeout(this, EventArgs.Empty);

                // Make an event for this delay being up, so the game can handle this at root level
                SpawnerCountdown = new TimeSpan();
            }

            for (int curEntity = 0; curEntity < Entities.Count(); curEntity++)
            {
                if (Entities[curEntity] != null && !Entities[curEntity].alive)
                {
                    this.RemoveEntity(curEntity);
                }
            }

            // Update here?
            base.Update(time);
        }

        /// <summary>
        /// Handles the event called whan a creep is outside of its tile's bounds.
        /// </summary>
        /// <param name="sender">Creep that is outside of bounds.</param>
        /// <param name="args">Event information arguments.</param>
        public void HandleCreepOverlap(Creep sender, EventArgs args)
        {
            // First we need to check which side the creep left.
            // That is returned with the EventArgs. What's passed here is a CreepEventArguments object, typically.

            // Use the LocationHelper's Direction enum to figure out what to do to move the creep to the new tile.
            CreepMovementArguments moveArgs = (CreepMovementArguments) args;
            Direction SideLeft = (Direction) moveArgs.Data["side"];
            
            // Console.WriteLine(String.Format("Creep on the move {0}: from {1}, since creep is at: {2}", SideLeft, sender.Location.GridLocation, sender.Position));

            Direction nd = sender.SetNextRandomDirection();
            // Console.WriteLine("Creep decided to go " + nd + " from here.");

            sender.NextDirectionToGo = nd;

            switch (SideLeft)
            {
                case Direction.North:
                    sender.Location.Move(Direction.North);
                    if (sender.Location.GridLocation.Y >= 0)
                        sender.Position.Y = sender.Location.GridManager.TileSize.Y - 1;
                    break;

                case Direction.South:
                    sender.Location.Move(Direction.South);
                    if (sender.Location.GridLocation.Y < sender.Location.GridManager.GridSize.Y)
                        sender.Position.Y = 1;  
                    break;

                case Direction.West:
                    sender.Location.Move(Direction.West);
                    if (sender.Location.GridLocation.X >= 0)
                        sender.Position.X = sender.Location.GridManager.TileSize.X - 1;
                    break;

                case Direction.East:
                    sender.Location.Move(Direction.East);
                    if (sender.Location.GridLocation.X < sender.Location.GridManager.GridSize.X)
                        sender.Position.X = 1;
                    break;
            }
        }

        public override void Draw(GameTime time, SpriteBatch canvas, SpriteFont debugFont)
        {

            canvas.Begin();

            if (debugFont != null)
            {
                foreach (Entity entity in Entities)
                {
                    if (entity != null && entity.alive)
                    {
                        Creep ce = (Creep)entity;
                        canvas.DrawString(debugFont, entity.GetDebugString(), ce.Location.GetDrawingLocation() + ce.Position + (ce.Size / 2), Color.White);
                    }
                }
            }

            // No use for the time object yet.
            foreach (Entity entity in Entities)
            {
                if (entity != null && entity.alive)
                    entity.Draw(time, canvas);
            }

            canvas.End();
        }
    }
}
