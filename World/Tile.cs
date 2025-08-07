﻿
using DeepWoods.World.Biomes;

namespace DeepWoods.World
{
    internal struct Tile
    {
        public IBiome biome;
        public GroundType groundType;
        public int x;
        public int y;
        public bool isOpen;
    }
}
