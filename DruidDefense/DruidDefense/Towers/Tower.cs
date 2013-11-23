using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Ostenvighx.Framework.Xna.Graphics;
using Ostenvighx.Framework.Xna.Layout;

namespace DruidDefense.Towers
{

    /// <summary>
    /// Base tile class.
    /// </summary>
    class Tower
    {

        public Spritesheet TowerTextures;

        public int CurrentFrame;
        public List<Frame> TowerFrames;

        public float Health;
        public TimeSpan ReloadSpeed;
        public TimeSpan TimeSinceReload;

        
        public Tower(Spritesheet TextureSheet, Rectangle DefaultDrawingArea)
            : this(100, TextureSheet, DefaultDrawingArea, new TimeSpan()) { }

        public Tower(float Health, Spritesheet TextureSheet, Rectangle DefaultDrawingArea) 
            : this(Health, TextureSheet, DefaultDrawingArea, new TimeSpan()) { }

        public Tower(float Health, Spritesheet TextureSheet, Rectangle DefaultDrawingArea, TimeSpan ReloadSpeed)
        {
            this.TowerTextures = TextureSheet;

            this.CurrentFrame = 0;

            this.TowerFrames = new List<Frame>();
            this.TowerFrames.Add(new Frame(DefaultDrawingArea, 1f));

            this.ReloadSpeed = ReloadSpeed;
            this.TimeSinceReload = new TimeSpan();

            this.Health = Health;
            
        }

        public virtual void Update(GameTime time) {
            this.TimeSinceReload = this.TimeSinceReload.Add(time.ElapsedGameTime);

            if (TimeSinceReload >= ReloadSpeed) {
                TimeSinceReload = new TimeSpan();
                Console.WriteLine("FIAR");
            }
        }

        public virtual void Draw(GameTime time, SpriteBatch canvas, TilePosition location)
        {
            canvas.Begin();
            this.TowerFrames[CurrentFrame].Draw(time, canvas, TowerTextures, location.GetDrawingLocation());
            canvas.End();
        }
    }
}
