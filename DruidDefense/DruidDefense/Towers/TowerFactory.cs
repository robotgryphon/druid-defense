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
    public class TowerFactory {

        public static TileWithTower CreateNewTurret(Spritesheet Textures, TilePosition CursorLocation, Texture2D GrassTexture) {

            Tower NewTurret = new Tower(120, Textures, new Rectangle(0, 0, 50, 50), new TimeSpan(0, 0, 0, 1));
            TileWithTower TurretTower = new TileWithTower(NewTurret, GrassTexture, (TilePosition) CursorLocation.Clone(), "Tile.Tower.Turret");
            return TurretTower;
        }

        public static TileWithTower CreateNewTower2(Spritesheet Textures, TilePosition CursorLocation, Texture2D GrassTexture) {

            Tower NewTurret = new Tower(120, Textures, new Rectangle(50, 0, 50, 50), new TimeSpan(0, 0, 0, 1));
            TileWithTower TurretTower = new TileWithTower(NewTurret, GrassTexture, (TilePosition) CursorLocation.Clone(), "Tile.Tower.Tower2");
            return TurretTower;
        }
    }
}
