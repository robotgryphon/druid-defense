using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostenvighx.Framework.Xna.Layout;
using Microsoft.Xna.Framework;
using Ostenvighx.Framework.Xna.Graphics;
using Microsoft.Xna.Framework.Graphics;
using DruidDefense.Tiles;

namespace DruidDefense.Towers {
    public class TowerFactory
    {

        public static TileWithTower CreateNewTurret(Spritesheet Textures, TilePosition CursorLocation, Texture2D GrassTexture)
        {
            TilePosition location = (TilePosition) CursorLocation.Clone();
            Tower NewTurret = new Tower(location, 120, 7, Textures, new Rectangle(0, 0, 25, 19), new TimeSpan(0, 0, 0, 0, 250));
            NewTurret.AnimationHandler.GetAnimation("Editor").GetFrame(0).Origin = new Vector2(15, 9);
            TileWithTower TurretTower = new TileWithTower(NewTurret, GrassTexture, location, "Tile.Tower.Turret");
            return TurretTower;
        }
    }
}
