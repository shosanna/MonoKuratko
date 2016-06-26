using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKuratko.Logic;
using Nez;
using Nez.Sprites;
using Nez.Tiled;
using Microsoft.Xna.Framework.Media;
using Nez.Textures;
using System.Collections.Generic;

namespace MonoKuratko
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new KuratkoGame()) {
                game.Run();
            }
        }
    }

    public class KuratkoGame : Core
    {
        
        enum Animations
        {
            Default,
            Walk,
        }

        static Sprite<Animations> _animation;

        public KuratkoGame(int width = 1280, int height = 720, bool isFullScreen = false,
            bool enableEntitySystems = true, string windowTitle = "Nez")
            : base(width, height, isFullScreen, enableEntitySystems, windowTitle)
        {
            Window.ClientSizeChanged += Core.onClientSizeChanged;
        }

        protected override void Initialize()
        {
            base.Initialize();
            Window.AllowUserResizing = true;

            var defaultScene = Scene.createWithDefaultRenderer(Color.CornflowerBlue);

            // HUDBA
            var _hudbik = Content.Load<Song>("background");
            MediaPlayer.Play(_hudbik);

            // TILED MAPA 
            var tiledEntity = defaultScene.createEntity("tiled-map");
            var tiledMap = contentManager.Load<TiledMap>("mapa");
            tiledEntity.addComponent(new TiledMapComponent(tiledMap));

            // KURATKO - PLAYER
            var kuratko = defaultScene.createEntity("kuratko");
            kuratko.addComponent(new SimpleMover());

            // ANIMACE
            var texture = defaultScene.contentManager.Load<Texture2D>("sprites/zviratka");
            var subtextures = Subtexture.subtexturesFromAtlas(texture, 32, 32);
            _animation = kuratko.addComponent(new Sprite<Animations>(subtextures[8]));

            _animation.addAnimation(Animations.Default, new SpriteAnimation(new List<Subtexture>()
            {
                subtextures[8],
            }));

            _animation.addAnimation(Animations.Walk, new SpriteAnimation(new List<Subtexture>()
            {

                subtextures[9],
                subtextures[8],
                subtextures[16],

            }));


            Core.scene = defaultScene;
        }

        public class SimpleMover : Component, IUpdatable
        {
            public void update()
            {
                var animation = Animations.Default;

                if (Input.isKeyDown(Keys.W)) { 
                    entity.transform.position += new Vector2(0, -1);
                }

                if (Input.isKeyDown(Keys.S)) {
                    entity.transform.position += new Vector2(0, 1);
                }

                if (Input.isKeyDown(Keys.A)) {
                    entity.transform.position += new Vector2(-1, 0);
                }

                if (Input.isKeyDown(Keys.D)) { 
                    entity.transform.position += new Vector2(1, 0);
                }

                if (Input.isKeyDown(Keys.W) || Input.isKeyDown(Keys.S) || Input.isKeyDown(Keys.A) || Input.isKeyDown(Keys.D)) 
                {
                    animation = Animations.Walk;
                    if (!_animation.isAnimationPlaying(animation))
                        _animation.play(animation);
                }
                else
                {
                    _animation.stop();
  
                }
            }
        }
    }
}