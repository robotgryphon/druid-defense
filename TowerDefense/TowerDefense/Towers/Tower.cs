using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Graphics;
using Ostenvighx.Framework.Xna.Layout;

namespace TowerDefense.Towers
{

    /// <summary>
    /// Base tile class.
    /// </summary>
    class Tower
    {

        public Spritesheet TowerTextures;

        public int CurrentFrame;
        public List<Frame> TowerFrames;

        public Tower(Spritesheet TextureSheet, Rectangle DefaultDrawingArea)
        {
            this.TowerTextures = TextureSheet;

            this.CurrentFrame = 0;

            this.TowerFrames = new List<Frame>();
            this.TowerFrames.Add(new Frame(DefaultDrawingArea, 1f));

            
        }

        public virtual void Draw(GameTime time, SpriteBatch canvas, TilePosition location)
        {
            canvas.Begin();
            this.TowerFrames[CurrentFrame].Draw(time, canvas, TowerTextures, location.GetDrawingLocation());
            canvas.End();
        }
    }
}
