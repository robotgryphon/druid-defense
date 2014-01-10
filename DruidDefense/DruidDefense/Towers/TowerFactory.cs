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

        public enum TowerType : int 
        {
            Small = 1,
            Medium = 2,
            Large = 3
        }

        public static TileWithTower CreateNewTower(Spritesheet Textures, TilePosition CursorLocation, Texture2D GrassTexture, TowerType type)
        {
            

            Color towerColoration = Color.White;
            float damage = 1;

            switch (type)
            {
                case TowerType.Small:
                    towerColoration = Color.White;
                    damage = 1;
                    break;

                case TowerType.Medium:
                    towerColoration = Color.LightBlue;
                    damage = 4;
                    break;


                case TowerType.Large:
                    towerColoration = Color.LightPink;
                    damage = 9;
                    break;
            }

            Tower NewTurret = new Tower((TilePosition)CursorLocation.Clone(), ((int)type) * 50, ((int) type), damage, Textures, new Rectangle(0, 0, 25, 19), new TimeSpan(0, 0, 0, 0, 250));
            NewTurret.AnimationHandler.GetAnimation("Editor").GetFrame(0).Origin = new Vector2(15, 9);
            NewTurret.AnimationHandler.GetAnimation("Editor").GetFrame(0).Coloration = towerColoration;

            TileWithTower TurretTower = new TileWithTower(NewTurret, GrassTexture, (TilePosition)CursorLocation.Clone(), "Tile.Tower." + type.ToString());
            return TurretTower;
        }

        public static Frame CreateTowerImage(TowerType type, Spritesheet Textures)
        {

            Frame towerImage = new Frame(new Rectangle(0, 0, 25, 19), Vector2.Zero, 1f, 0f, Vector2.Zero, 1f);

            Color towerColoration = Color.White;
            switch (type)
            {
                case TowerType.Small:
                    towerColoration = Color.White;
                    break;

                case TowerType.Medium:
                    towerColoration = Color.LightBlue;
                    break;


                case TowerType.Large:
                    towerColoration = Color.LightPink;
                    break;
            }

            towerImage.Coloration = towerColoration;

            return towerImage;
        }
    }
}
