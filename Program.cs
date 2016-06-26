using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoKuratko.Logic;
using Nez;
using Nez.Sprites;
using Nez.Tiled;

namespace MonoKuratko
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //new Les(5).NaplnMapu("C:\\dev\\opengl_kuratko\\res\\xmlova.tmx");

            //using (var game = new Game1())
            //game.Run();
            using (var game = new KuratkoGame()) {
                game.Run();
            }
        }
    }

    public class KuratkoGame : Core
    {
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

            var tiledEntity = defaultScene.createEntity("tiled-map");
            var tiledMap = contentManager.Load<TiledMap>("mapa");
            tiledEntity.addComponent(new TiledMapComponent(tiledMap));

            var kuratkoTex = defaultScene.contentManager.Load<Texture2D>("sprites/kuratko");

            var kuratko = defaultScene.createEntity("kuratko");
            kuratko.addComponent(new Sprite(kuratkoTex));

            kuratko.addComponent(new SimpleMover());

            Core.scene = defaultScene;
        }

        public class SimpleMover : Component, IUpdatable
        {
            public void update()
            {
                if (Input.isKeyDown(Keys.W))
                    entity.transform.position += new Vector2(0, -1);
                if (Input.isKeyDown(Keys.S))
                    entity.transform.position += new Vector2(0, 1);
                if (Input.isKeyDown(Keys.A))
                    entity.transform.position += new Vector2(-1, 0);
                if (Input.isKeyDown(Keys.D))
                    entity.transform.position += new Vector2(1, 0);
            }
        }
    }

    public class BasicScene : Scene
    {
    }
}