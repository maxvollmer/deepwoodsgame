﻿using DeepWoods.Helpers;
using DeepWoods.Loaders;
using DeepWoods.Players;
using DeepWoods.World;
using DeepWoods.World.Biomes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepWoods.Objects
{
    internal class ObjectManager
    {
        private readonly List<DWObjectDefinition> objectDefinitions;
        private readonly Random rng;
        private readonly int width;
        private readonly int height;

        private readonly InstancedObjects instancedObjects;
        private readonly InstancedObjects instancedCritters;

        private readonly List<DWObject> objects = [];
        private readonly List<DWObject> critters = [];
        private readonly Dictionary<(int, int), int> objectIndices = [];

        public ObjectManager(ContentManager content, GraphicsDevice graphicsDevice, int seed, int width, int height, Terrain terrain)
        {
            rng = new Random(seed);
            objectDefinitions = content.Load<List<DWObjectDefinition>>("objects/objects");

            this.width = width;
            this.height = height;

            TemperateForestBiome biome = new TemperateForestBiome();


            GenerateObjects(biome, terrain, objects, critters);

            instancedObjects = new InstancedObjects(graphicsDevice, objects, TextureLoader.ObjectsTexture);
            instancedCritters = new InstancedObjects(graphicsDevice, critters, TextureLoader.Critters);
        }

        private void GenerateObjects(IBiome biome, Terrain terrain, List<DWObject> objects, List<DWObject> critters)
        {
            var critterIDs = new List<CritterDefinitions.Critter>(Enum.GetValues<CritterDefinitions.Critter>());

            // TODO TEMP Sprite Test
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (terrain.CanSpawnCritter(x, y) && rng.NextSingle() < biome.StuffDensity)
                    {
                        CritterDefinitions.Critter critter = critterIDs[rng.Next(critterIDs.Count)];
                        var def = CritterDefinitions.GetCritterDefinition(critter);
                        critters.Add(new DWObject(new Vector2(x, y), def));
                    }
                    else if (terrain.CanSpawnStuff(x, y) && rng.NextSingle() < biome.StuffDensity)
                    {
                        var o = SpawnRandomObject(biome.Stuff, x, y);
                        if (o != null)
                        {
                            objectIndices.Add((x, y), objects.Count);
                            objects.Add(o);
                        }
                    }
                    else if (terrain.CanSpawnBuilding(x, y) && rng.NextSingle() < biome.BuildingDensity)
                    {
                        var o = SpawnRandomObject(biome.Buildings, x, y);
                        if (o != null)
                        {
                            objectIndices.Add((x, y), objects.Count);
                            objects.Add(o);
                        }
                    }
                    else if (terrain.CanSpawnTree(x, y))
                    {
                        var o = SpawnRandomObject(biome.Trees, x, y);
                        if (o != null)
                        {
                            objectIndices.Add((x, y), objects.Count);
                            objects.Add(o);
                        }
                    }
                }
            }

            /*
            SpawnObject("tree1", 2, 3);
            SpawnObject("tree1", 3, 3);
            SpawnObject("tree1", 4, 3);
            SpawnObject("tree1", 5, 3);
            SpawnObject("tree1", 6, 3);
            SpawnObject("tree1", 7, 3);
            SpawnObject("tower", 5, 2);
            */
        }

        private DWObject SpawnRandomObject(List<string> objectList, int x, int y)
        {
            if (objectList.Count == 0)
            {
                return null;
            }
            var objectName = objectList[rng.Next(objectList.Count)];
            return SpawnObject(objectName, x, y);
        }

        private DWObject SpawnObject(string name, int x, int y)
        {
            var def = objectDefinitions.Where(o => o.Name == name).FirstOrDefault();
            if (def == null)
            {
                return null;
            }
            return new DWObject(new Vector2(x, y), def);
        }


        internal void DrawShadowMap(GraphicsDevice graphicsDevice, List<Player> players, Camera camera)
        {
            Matrix view = camera.ShadowView;
            Matrix projection = camera.ShadowProjection;

            graphicsDevice.SetRenderTarget(TextureLoader.ShadowMap);
            graphicsDevice.Clear(Color.Black);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            EffectLoader.SpriteEffect.Parameters["CellSize"].SetValue(Terrain.CellSize);
            EffectLoader.SpriteEffect.Parameters["ViewProjection"].SetValue(view * projection);
            EffectLoader.SpriteEffect.Parameters["IsShadow"].SetValue(1);


            instancedObjects.Draw(graphicsDevice);
            instancedCritters.Draw(graphicsDevice);


            foreach (var player in players)
            {
                player.DrawShadow(graphicsDevice, camera);
            }

            graphicsDevice.SetRenderTarget(null);
        }


        internal void Draw(GraphicsDevice graphicsDevice, Camera camera)
        {
            Matrix view = camera.View;
            Matrix projection = camera.Projection;

            var spriteEffect = EffectLoader.SpriteEffect;

            spriteEffect.Parameters["CellSize"].SetValue(Terrain.CellSize);
            spriteEffect.Parameters["ViewProjection"].SetValue(view * projection);
            spriteEffect.Parameters["IsShadow"].SetValue(0);
            spriteEffect.Parameters["ShadowMap"].SetValue(TextureLoader.ShadowMap);
            spriteEffect.Parameters["ShadowMapBounds"].SetValue(camera.ShadowRectangle.GetBoundsV4());
            spriteEffect.Parameters["ShadowMapTileSize"].SetValue(camera.ShadowRectangle.GetSizeV2());

            instancedObjects.Draw(graphicsDevice);
            instancedCritters.Draw(graphicsDevice);
        }

        internal DWObject GetObject(Terrain terrain, int x, int y)
        {
            if (terrain.IsTreeTile(x, y))
            {
                return null;
            }

            if (objectIndices.TryGetValue((x,y), out var index))
            {
                instancedObjects.HideInstance(index);
                objectIndices.Remove((x, y));
                return objects[index];
            }

            return null;
        }
    }
}