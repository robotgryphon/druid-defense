using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Layout;
using DruidDefense.Creeps;

namespace DruidDefense.Tiles
{
    public class DefenseTileManager : TileManager
    {

        public DefenseTileManager(Point GridSize, Point TileSize)
            : base(GridSize, TileSize) { }

        public void ResetTowers()
        {
            // Reset all towers
            foreach (Tile tile in Tiles)
            {
                if (tile != null)
                {
                    if (tile.GetType().Equals(typeof(TileWithTower)))
                    {
                        TileWithTower tile2 = (TileWithTower)tile;
                        tile2.TowerObject.Reset();
                    }
                }
            }
        }

        public override void Draw(GameTime time, SpriteBatch canvas)
        {
            this.Draw(time, canvas, false);
        }

        public void Draw(GameTime time, SpriteBatch canvas, Boolean playing) {
            this.Draw(time, canvas, playing, false, null);
        }

        public void Draw(GameTime time, SpriteBatch canvas, Boolean playing, Boolean debugDraw, SpriteFont font)
        {

            canvas.Begin();

            foreach (Tile tile in Tiles)
            {
                if (tile != null)
                {
                    if (tile.GetType().Equals(typeof(TileWithTower)))
                    {
                        TileWithTower tile2 = (TileWithTower) tile;

                        if (debugDraw)
                        {
                            tile2.Draw(time, canvas, true, font);
                        }
                        else
                        {
                            tile2.Draw(time, canvas);
                        }
                    }
                    else
                    {
                        if (debugDraw)
                        {
                            tile.Draw(time, canvas, true, font);
                        }
                        else
                        {
                            tile.Draw(time, canvas, false, null);
                        }
                    }
                }
            }
            canvas.End();
        }

        /// <summary>
        /// Check all the towers to see if a creep entered their range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void CheckTowerRanges(Creep sender, EventArgs args)
        {
            // Console.WriteLine("Creep went out of bounds. Sending check to my towers.");
            TilePosition senderPosition = (TilePosition)sender.Location.Clone();

            foreach (Tile tile in Tiles)
            {
                if (tile.GetType().Equals(typeof(TileWithTower)))
                {
                    TileWithTower thisTile = (TileWithTower) tile;
                    thisTile.TowerObject.CheckEntityInRange(sender);
                }
            }
            
        }
    }
}
