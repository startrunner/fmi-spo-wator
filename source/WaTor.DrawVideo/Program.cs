using System;
using WaTor.Simulation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace WaTor.DrawVideo
{
    class Program
    {
        static void Main(string[] args)
        {
            var gameParameters = new Parameters {
                SeaSizeX = 1000,
                SeaSizeY = 1000,
                InitialFishCount = 100,
                InitialSharkCount = 10,
                ThreadSleepTime = TimeSpan.FromSeconds(0.00),
                ScreenRefreshRate = TimeSpan.FromSeconds(.1),
                TotalThreadCount = 10,
                FishReproductionRate = 5,
                SharkReproductionRate = 20,
                SharkEnergyLoss = 80,
                EnergyInFish = 100,
                InitialSharkEnergy = 250,
            };


            var random = new Random(11);
            SeaBlock[,] theSea = WaTor.Simulation.WaTor.GenerateSea(gameParameters, random);
            SeaChunk[,] chunks = WaTor.Simulation.WaTor.ChunkUpSea(gameParameters, random, theSea);
            WaTor.Simulation.WaTor.RunSimulation(gameParameters, chunks);

            List<(int x, int y)> coordinates = new();
            for (int x = 0; x < gameParameters.SeaSizeX; x++)
                for (int y = 0; y < gameParameters.SeaSizeY; y++)
                    coordinates.Add((x, y));

            coordinates =
                coordinates
                .Select(x => (x, comparand: Guid.NewGuid()))
                .OrderBy(x => x.comparand)
                .Select(x => x.x)
                .ToList();

            var bitmap = new Image<Rgb24>(gameParameters.SeaSizeX, gameParameters.SeaSizeY);

            Directory.Delete("frames", recursive: true);

            long TotalFrames = 30 * (long)TimeSpan.FromMinutes(1).TotalSeconds;
            for (long frame = 0; frame < TotalFrames; ++frame)
            {
                for (int x = 0; x < gameParameters.SeaSizeX; ++x)
                {
                    Span<Rgb24> bitmapRow = bitmap.GetPixelRowSpan(x);
                    for (int y = 0; y < gameParameters.SeaSizeY; ++y)
                    {
                        Color color = Color.White;
                        var block = theSea[x, y];
                        if (block.Type == OceanBlockType.Fish) color = Color.Green;
                        if (block.Type == OceanBlockType.Shark) color = Color.Blue;

                        bitmapRow[y] = color.ToPixel<Rgb24>();
                    }
                }

                if (frame % 1000 == 0)
                {
                    Console.WriteLine($"Frame {frame} of {TotalFrames}...");
                }

                string fileName = $"frames/{frame}.bmp";
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fileName)));
                bitmap.Save(fileName);
                Thread.Sleep(gameParameters.ScreenRefreshRate);
            }

            Console.WriteLine("Done");
            Environment.Exit(0);
        }
    }
}
