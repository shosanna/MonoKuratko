using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
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
        private int framesPerStep = 1;
        private Les les = new Les(5);
        private Dictionary<string, Texture2D> Tiles = new Dictionary<string, Texture2D>();
        private int rozmerDlazdice = 32;
        private InputManager _inputManager;
        private Song _hudbik;
        private SoundEffect _pip;
        private Texture2D _boruvkovyStrom;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            _inputManager = new InputManager();
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
            _boruvkovyStrom = Content.Load<Texture2D>("blueberry_bush");

            _hudbik = Content.Load<Song>("background");
            _pip = Content.Load<SoundEffect>("pip");
            MediaPlayer.Play(_hudbik);

            les.NaplnMapu("C:\\dev\\MonoKuratko\\Content\\mapa.tmx");

            for (int i = 0; i < les.Pozadi.Size; i++) {
                for (int j = 0; j < les.Pozadi.Size; j++) {
                    var obrazek = les.Pozadi[j, i].Obrazek;
                    rozmerDlazdice = les.Pozadi[j, i].Sirka;

                    if (!Tiles.ContainsKey(obrazek)) {
                        var name = new FileInfo(obrazek).Name;
                        name = name.Replace(".png", "");
                        var texture2D = Content.Load<Texture2D>(name);
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
            _inputManager.Refresh();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboard = Keyboard.GetState();

            var offset = 1;

            if (_inputManager.IsKeyJustPressed(Keys.A)) {
                position.X -= offset;
                steps += 1;
            }
            if (_inputManager.IsKeyJustPressed(Keys.D)) {
                position.X += offset;
                steps += 1;
            }
            if (_inputManager.IsKeyJustPressed(Keys.W)) {
                position.Y -= offset;
                steps += 1;
            }
            if (_inputManager.IsKeyJustPressed(Keys.S)) {
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


            for (int i = 0; i < les.Pozadi.Size; i++) {
                for (int j = 0; j < les.Pozadi.Size; j++) {
                    var pos = new Vector2(i*rozmerDlazdice, j*rozmerDlazdice);
                    spriteBatch.Draw(Tiles[les.Pozadi[i, j].Obrazek], pos);

                    if (les.Boruvky[i, j]) {
                        spriteBatch.Draw(_boruvkovyStrom, pos);
                    }
                }
            }            

            spriteBatch.Draw(usedTexture, new Vector2(position.X*rozmerDlazdice, position.Y*rozmerDlazdice));

            spriteBatch.End();


            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}