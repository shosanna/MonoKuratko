﻿using System.Collections.Generic;
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


        protected override void LoadContent()
        {

            // HUDBA
            //_hudbik = Content.Load<Song>("background");
            //_pip = Content.Load<SoundEffect>("pip");
            //MediaPlayer.Play(_hudbik);

        }


        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            //_inputManager.Refresh();

            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            //    Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            //var offset = 1;

            //if (_inputManager.IsKeyJustPressed(Keys.A)) {
            //    position.X -= offset;
            //    steps += 1;
            //}
            //if (_inputManager.IsKeyJustPressed(Keys.D)) {
            //    position.X += offset;
            //    steps += 1;
            //}
            //if (_inputManager.IsKeyJustPressed(Keys.W)) {
            //    position.Y -= offset;
            //    steps += 1;
            //}
            //if (_inputManager.IsKeyJustPressed(Keys.S)) {
            //    position.Y += offset;
            //    steps += 1;
            //}

            //steps %= 3*framesPerStep;

            //base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            //GraphicsDevice.Clear(Color.CornflowerBlue);

            //spriteBatch.Begin();

            //Texture2D usedTexture = null;
            //if (0 * framesPerStep <= steps && steps < 1 * framesPerStep) usedTexture = kuratkoTexture;
            //if (1 * framesPerStep <= steps && steps < 2 * framesPerStep) usedTexture = kuratkoStepLeftTexture;
            //if (2 * framesPerStep <= steps && steps < 3 * framesPerStep) usedTexture = kuratkoStepRightTexture;


            //for (int i = 0; i < les.Pozadi.Size; i++)
            //{
            //    for (int j = 0; j < les.Pozadi.Size; j++)
            //    {
            //        var pos = new Vector2(i * rozmerDlazdice, j * rozmerDlazdice);
            //        spriteBatch.Draw(Tiles[les.Pozadi[i, j].Obrazek], pos);
            //    }
            //}

            //spriteBatch.Draw(usedTexture, new Vector2(position.X * rozmerDlazdice, position.Y * rozmerDlazdice));

            //spriteBatch.End();


            //base.Draw(gameTime);
        }
    }
}