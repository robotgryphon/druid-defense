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

            Frame NewFrame = new Frame(
                new Rectangle(0, 0, 30, 30),
                Vector2.Zero,
                1f,
                0f,
                new Vector2(Size.X / 2, Size.Y / 2),
                1f);

            NewFrame.Coloration = new Color(Game1.Randomizer.Next(0, 255), Game1.Randomizer.Next(0, 255), Game1.Randomizer.Next(0, 255));

            SpriteSystem.AddAnimation("Still").SetNextInQueue("Still");
            SpriteSystem.GetAnimation("Still").AddFrame(NewFrame);

            this.SpriteSystem.GetAnimation("Still").SetNextInQueue("Still");
            this.SpriteSystem.SwitchToAnimation("Still");
            this.Position = new Vector2(startPosition.GridManager.TileSize.X / 2, startPosition.GridManager.TileSize.Y / 2);
            this.Speed = new Vector2(3f, 3f);
            this.MovementDirection = Direction.North;
        }
    }
}
