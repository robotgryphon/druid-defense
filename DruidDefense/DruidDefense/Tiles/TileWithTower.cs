using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Layout;

using DruidDefense.Towers;

namespace DruidDefense.Tiles
{
    public class TileWithTower : Tile
    {
        public Tower TowerObject;

        public TileWithTower(Tower Tower, Texture2D TileTexture, TilePosition Location)
            : base(TileTexture, Location)
        {
            // So it made a tile. Woo.
            this.TowerObject = Tower;
        }

        public TileWithTower(Tower Tower, Texture2D TileTexture, TilePosition Location, String UnlocalName)
            : base(TileTexture, Location) {
            // So it made a tile. Woo.
            this.TowerObject = Tower;
            this.UnlocalizedName = UnlocalName;
        }

        public override void UpdateScaling(Point NewScale)
        {
            base.UpdateScaling(NewScale);
            // TowerObject.UpdateScaling(NewScale);
        }

        public override void Update(GameTime time) {

            TowerObject.Update(time);

            base.Update(time);
        }

        public override void Draw(GameTime time, Point TileSize, SpriteBatch canvas)
        {

            base.Draw(time, TileSize, canvas);

            // Now draw the tower
            TowerObject.Draw(time, canvas, this.Location);

            
        }
    }
}
