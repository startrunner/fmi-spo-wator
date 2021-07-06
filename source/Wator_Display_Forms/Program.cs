using System;
using WaTor.Simulation;

namespace Wator_Display_Forms
{
    static class Program
    {

        [STAThread]
        public static void Main()
        {
            var Parameters = new Parameters
            {
                SeaSizeX = 1920,
                SeaSizeY = 1080,
                InitialFishCount = 50000 * 100,
                InitialSharkCount = 100 * 100,
                ThreadSleepTime = TimeSpan.FromSeconds(0.00),
                ScreenRefreshRate = TimeSpan.FromSeconds(.2),
                TotalThreadCount = 5,
                FishReproductionInterval = 3,
                SharkReproductionInterval = 40,
                SharkEnergyLoss = 55,
                EnergyInFish = 40,
                InitialSharkEnergy = 1400,
                RandomizeInitialPozitions = true,
            };

            var random = new Random(11);
            SeaBlock[,] theSea = WaTor.Simulation.WaTor.GenerateSea(Parameters, random);
            SeaChunk[,] chunks = WaTor.Simulation.WaTor.ChunkUpSea(Parameters, random, theSea);
            WaTor.Simulation.WaTor.RunSimulation(Parameters, chunks);

            var window = new Form1(Parameters, theSea)
            {
                Width = Parameters.SeaSizeX * 10,
                Height = Parameters.SeaSizeY * 10,
                WindowState = System.Windows.Forms.FormWindowState.Maximized,
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
            };

            window.ShowDialog();
            Environment.Exit(0);
        }
    }
}
