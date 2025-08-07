﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepWoods.World.Generators
{
    internal class ForestGenerator : Generator
    {
        private readonly Tile[,] tiles;
        private readonly int width;
        private readonly int height;
        private readonly Random rng;

        private static readonly Point[] directions = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

        class Region
        {
            public HashSet<Point> tiles = new();
            public Point anchor;
        }

        public ForestGenerator(int width, int height, int seed)
        {
            tiles = new Tile[width, height];
            this.width = width;
            this.height = height;
            rng = new Random(seed);
        }

        private bool IsInsideGrid(Point next)
        {
            return next.X >= 0 && next.X < width && next.Y >= 0 && next.Y < height;
        }

        private double CurrentRatio()
        {
            int openTiles = 0;
            foreach (var tile in tiles)
            {
                if (tile.isOpen)
                    openTiles++;
            }
            return openTiles / (double)tiles.Length;
        }

        public override Tile[,] Generate()
        {
            int numSteps = Math.Max(10, width * height / 100);
            double goalRatio = 0.5;
            while (CurrentRatio() < goalRatio)
            {
                GenerateOpenPatch(new(rng.Next(width), rng.Next(height)), numSteps);
            }

            int treeBorderSize = 3;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < treeBorderSize; y++)
                {
                    tiles[x, y].isOpen = false;
                    tiles[x, height - 1 - y].isOpen = false;
                }
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < treeBorderSize; x++)
                {
                    tiles[x, y].isOpen = false;
                    tiles[width - 1 - x, y].isOpen = false;
                }
            }

            var regions = CollectRegions();
            ConnectRegions(regions);
            return tiles;
        }

        private List<Region> CollectRegions()
        {
            List<Region> regions = new();
            bool[,] visited = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x, y].isOpen && !visited[x, y])
                    {
                        Region region = new Region();
                        Queue<Point> queue = new Queue<Point>();
                        queue.Enqueue(new Point(x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            Point p = queue.Dequeue();
                            region.tiles.Add(p);
                            foreach (var direction in directions)
                            {
                                Point neighbour = p + direction;
                                if (IsInsideGrid(neighbour) && tiles[neighbour.X, neighbour.Y].isOpen && !visited[neighbour.X, neighbour.Y])
                                {
                                    queue.Enqueue(neighbour);
                                    visited[neighbour.X, neighbour.Y] = true;
                                }
                            }
                        }

                        region.anchor = region.tiles.ToList()[rng.Next(region.tiles.Count)];
                        regions.Add(region);
                    }
                }
            }

            return regions;
        }

        private void ConnectRegions(List<Region> regions)
        {
            if (regions.Count < 2)
                return;

            Point anchorA = regions[0].anchor;
            for (int i = 1; i < regions.Count; i++)
            {
                Point anchorB = regions[i].anchor;

                Point currentPoint = anchorA;
                while (currentPoint != anchorB)
                {
                    int distX = Math.Abs(anchorB.X - currentPoint.X);
                    int distY = Math.Abs(anchorB.Y - currentPoint.Y);

                    if (distY == 0 || rng.NextSingle() * distX > rng.NextSingle() * distY)
                    {
                        currentPoint.X += Math.Sign(anchorB.X - currentPoint.X);
                    }
                    else
                    {
                        currentPoint.Y += Math.Sign(anchorB.Y - currentPoint.Y);
                    }

                    tiles[currentPoint.X, currentPoint.Y].isOpen = true;
                }
            }
        }

        private void GenerateOpenPatch(Point p, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                if (IsInsideGrid(p))
                {
                    tiles[p.X, p.Y].isOpen = true;
                }
                p += directions[rng.Next(directions.Length)];
            }
        }
    }
}
