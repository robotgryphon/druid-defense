using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Layout;

using TowerDefense.Towers;

namespace TowerDefense.Tiles
{
    class TileWithTower : Tile
    {
        public Tower TowerObject;

        public TileWithTower(Tower Tower, Texture2D TileTexture, Point TileSize, TilePosition Location)
            : base(TileTexture, TileSize, Location)
        {
            // So it made a tile. Woo.
            this.TowerObject = Tower;
        }

        public override void Draw(GameTime time, SpriteBatch canvas)
        {
            // Now draw the tower
            TowerObject.Draw(time, canvas, this.Location);

            base.Draw(time, canvas);
        }
    }
}
