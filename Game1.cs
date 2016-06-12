using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKuratko.Logic;
using OpenTK;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace MonoKuratko
{
    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private Texture2D kuratkoTexture;
        private Texture2D kuratkoStepLeftTexture;
        private Texture2D kuratkoStepRightTexture;
        private Vector2 position;
        private SpriteBatch spriteBatch;
        private Texture2D texture;
        private int steps = 0;
        private int framesPerStep = 10;
        private Les les = new Les(5);
        private Dictionary<string, Texture2D> Tiles = new Dictionary<string, Texture2D>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }


        protected override void Initialize()
        {
            position = new Vector2(0);

            var size = 5;
            base.Initialize();
        }


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            kuratkoTexture = Content.Load<Texture2D>("kuratko_basic");
            kuratkoStepLeftTexture = Content.Load<Texture2D>("kuratko_step_left");
            kuratkoStepRightTexture = Content.Load<Texture2D>("kuratko_step_right");

            les.NaplnMapu("C:\\dev\\opengl_kuratko\\res\\xmlova.tmx");

            for (int i = 0; i < les.Pozadi.Size; i++) {
                for (int j = 0; j < les.Pozadi.Size; j++) {
                    var obrazek = les.Pozadi[j, i].Obrazek;

                    if (!Tiles.ContainsKey(obrazek)) {
                        var texture2D = Content.Load<Texture2D>(obrazek);
                        Tiles[obrazek] = texture2D;
                    }
                }
            }

        }


        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboard = Keyboard.GetState();

            var offset = 1;

            if (keyboard.IsKeyDown(Keys.A)) {
                position.X -= offset;
                steps += 1;
            }
            if (keyboard.IsKeyDown(Keys.D)) {
                position.X += offset;
                steps += 1;
            }
            if (keyboard.IsKeyDown(Keys.W)) {
                position.Y -= offset;
                steps += 1;
            }
            if (keyboard.IsKeyDown(Keys.S)) {
                position.Y += offset;
                steps += 1;
            }

            steps %= 3*framesPerStep;

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            Texture2D usedTexture = null;
            if (0*framesPerStep <= steps && steps < 1*framesPerStep) usedTexture = kuratkoTexture;
            if (1*framesPerStep <= steps && steps < 2*framesPerStep) usedTexture = kuratkoStepLeftTexture;
            if (2*framesPerStep <= steps && steps < 3*framesPerStep) usedTexture = kuratkoStepRightTexture;

            spriteBatch.Draw(usedTexture, new Vector2(position.X + 10, position.Y + 10));

            for (int i = 0; i < les.Pozadi.Size; i++)
            {
                for (int j = 0; j < les.Pozadi.Size; j++)
                {
                   spriteBatch.Draw(Tiles[les.Pozadi[i,j].Obrazek], new Vector2(i,j));
                }
            }

            spriteBatch.End();


            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}