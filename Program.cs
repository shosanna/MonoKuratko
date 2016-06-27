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
using System.Linq;

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
        private Kuratko _kuratko;
        private static Map _map;
        static Sprite<Animations> _animation;

        enum Animations
        {
            Default,
            Walk,
        }

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
            _map = new Map(defaultScene);
            
            // KURATKO - PLAYER
            _kuratko = new Kuratko(defaultScene);

            Core.scene = defaultScene;
        }

        class Map
        {
            public Entity entity;
            
            public Map(Scene defaultScene)
            {
                entity = defaultScene.createEntity("tiled-map");
                var tiledMap = contentManager.Load<TiledMap>("mapa");

                // Pozadi a zdi
                var tiledComponent = entity.addComponent(new TiledMapComponent(tiledMap, "Collision"));
                tiledComponent.setLayersToRender("Background", "Boruvky");

                // Boruvky
                var boruvkyComponent = entity.addComponent(new TiledMapComponent(tiledMap, "Boruvky"));
                boruvkyComponent.setLayersToRender("Boruvky");
            }
        }

        class Kuratko
        {
            public Entity entity;
            private Mover _mover;
            private KuratkoInputHandler _inputHandler;
            private CircleCollider _collider;

            public Kuratko(Scene defaultScene)
            {
                entity = defaultScene.createEntity("entity", new Vector2(50, 50));
                _inputHandler = entity.addComponent(new KuratkoInputHandler());
                _mover = entity.addComponent(new Mover());

                _collider = entity.colliders.add(new CircleCollider());                

                // ANIMACE
                var texture = defaultScene.contentManager.Load<Texture2D>("sprites/zviratka");
                var subtextures = Subtexture.subtexturesFromAtlas(texture, 32, 32);
                _animation = entity.addComponent(new Sprite<Animations>(subtextures[8]));

                _animation.addAnimation(Animations.Default, new SpriteAnimation(new List<Subtexture>() {
                    subtextures[8],
                }));

                _animation.addAnimation(Animations.Walk, new SpriteAnimation(new List<Subtexture>() {
                    subtextures[9],
                    subtextures[8],
                    subtextures[16],
                }));
            }
        }

        public class KuratkoInputHandler : Component, IUpdatable
        {
            public void update()
            {
                var animation = Animations.Default;

                var mover = entity.getComponent<Mover>();

                CollisionResult collisionResult;

                if (Input.isKeyDown(Keys.W)) {
                    mover.move(new Vector2(0, -1), out collisionResult);
                }

                if (Input.isKeyDown(Keys.S)) {
                    mover.move(new Vector2(0, 1), out collisionResult);
                }

                if (Input.isKeyDown(Keys.A)) {
                    mover.move(new Vector2(-1, 0), out collisionResult);
                }

                if (Input.isKeyDown(Keys.D)) {
                    mover.move(new Vector2(1, 0), out collisionResult);
                }

                if (Input.isKeyDown(Keys.W) || Input.isKeyDown(Keys.S) || Input.isKeyDown(Keys.A) ||
                    Input.isKeyDown(Keys.D)) {


                    var tile =  _map.entity.getComponents<TiledMapComponent>().Last().getTileAtWorldPosition(entity.transform.position);

                    animation = Animations.Walk;
                    if (!_animation.isAnimationPlaying(animation))
                        _animation.play(animation);
                } else {
                    _animation.stop();
                }
            }
        }
    }
}