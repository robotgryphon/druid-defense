using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Ostenvighx.Framework.Xna.Graphics;
using Ostenvighx.Framework.Xna.Layout;
using Ostenvighx.Framework.Xna.Utilities;

namespace DruidDefense.Creeps
{
    class BlobCreep : Creep
    {

        public BlobCreep(Spritesheet sheet, TilePosition startPosition)
            :base(sheet, startPosition)
        {

            this.CreateSpriteArea("Still", new Rectangle(0, 0, 30, 30));
            this.SpriteSystem.GetAnimation("Still").SetNextInQueue("Still");
            this.SpriteSystem.SwitchToAnimation("Still");
            this.Position = new Vector2(startPosition.GridManager.TileSize.X / 2, startPosition.GridManager.TileSize.Y / 2);
            this.Speed = new Vector2(0.5f, 0.5f);
            this.MovementDirection = Direction.North;
        }
    }
}
