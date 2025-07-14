﻿
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DeepWoods.Loaders
{
    internal static class TextureLoader
    {
        public static Texture2D GroundTilesTexture { get; private set; }
        public static Texture2D BluenoiseTexture { get; private set; }
        public static Texture2D ObjectsTexture { get; private set; }
        public static Texture2D MouseCursor { get; private set; }
        public static Texture2D CharacterTileSet { get; private set; }
        

        public static RenderTarget2D ShadowMap { get; private set; }

        public static void Load(ContentManager content, GraphicsDevice graphicsDevice)
        {
            GroundTilesTexture = content.Load<Texture2D>("textures/groundtiles");
            BluenoiseTexture = content.Load<Texture2D>("textures/bluenoise_rgba");
            ObjectsTexture = content.Load<Texture2D>("objects/objects_tileset");
            MouseCursor = content.Load<Texture2D>("icons/cursor");
            CharacterTileSet = content.Load<Texture2D>("characters/temp_character");

            ShadowMap = new RenderTarget2D(graphicsDevice,
                1024, 1024,
                false,
                SurfaceFormat.Single,
                DepthFormat.None,
                0, RenderTargetUsage.DiscardContents, false);

        }
    }
}
