using System;
using WaTor.Simulation;

namespace Wator_Display_Forms
{
    static class Program
    {

        [STAThread]
        public static void Main()
        {
            var Parameters = new Parameters {
                SeaSizeX = 800,
                SeaSizeY = 800,
                InitialFishCount = 50000,
                InitialSharkCount = 100,
                ThreadSleepTime = TimeSpan.FromSeconds(0.00),
                ScreenRefreshRate = TimeSpan.FromSeconds(1),
                TotalThreadCount = 2,
                FishReproductionRate = 3,
                SharkReproductionRate = 25,
                SharkEnergyLoss = 80,
                EnergyInFish = 100,
                InitialSharkEnergy = 150,
            };

            var random = new Random(11);
            SeaBlock[,] theSea = WaTor.Simulation.WaTor.GenerateSea(Parameters, random);
            SeaChunk[,] chunks = WaTor.Simulation.WaTor.ChunkUpSea(Parameters, random, theSea);
            WaTor.Simulation.WaTor.RunSimulation(Parameters, chunks);

            var window = new Form1(Parameters, theSea) {
                Width = Parameters.SeaSizeX * 10,
                Height = Parameters.SeaSizeY * 10,
            };

            window.ShowDialog();
            Environment.Exit(0);
        }
    }
}
