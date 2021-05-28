using System;
using System.Threading;
using WaTor.Simulation;

namespace WaTor.Display
{
    public static class App
    {
        public static readonly Parameters Parameters = new Parameters {
            SeaSizeX = 200,
            SeaSizeY = 200,
            InitialFishCount = 5000,
            InitialSharkCount = 10,
            BlockWidth = 50,
            BlockHeight = 50,
            ThreadSleepTime = TimeSpan.FromSeconds(0.00),
            ScreenRefreshRate = TimeSpan.FromSeconds(1),
            FishReproductionRate = 1,
            SharkReproductionRate = 500,
            SharkEnergyLoss = 9900,
            EnergyInFish = 10000,
            InitialSharkEnergy = 100000,
        };

        [STAThread]
        public static void Main()
        {
            var random = new Random(11);

            SeaBlock[,] theSea;
            {//Generate initial sea
                theSea = new SeaBlock[Parameters.SeaSizeX, Parameters.SeaSizeY];

                for (int i = 0; i < Parameters.InitialFishCount; i++)
                {
                    int x, y;
                    do
                    {
                        x = random.Next(Parameters.SeaSizeX);
                        y = random.Next(Parameters.SeaSizeY);
                    } while (theSea[x, y] != null);

                    theSea[x, y] = new SeaBlock(OceanBlockType.Fish);
                }
                for (int i = 0; i < Parameters.InitialSharkCount; i++)
                {
                    int x, y;
                    do
                    {
                        x = random.Next(Parameters.SeaSizeX);
                        y = random.Next(Parameters.SeaSizeY);
                    } while (theSea[x, y] != null);

                    theSea[x, y] = new SeaBlock(OceanBlockType.Shark);
                }

                for (int x = 0; x < Parameters.SeaSizeX; x++)
                    for (int y = 0; y < Parameters.SeaSizeY; y++)
                        if (theSea[x, y] is null)
                            theSea[x, y] = new SeaBlock(OceanBlockType.None);
            }

            var cancellationSource = new CancellationTokenSource();

            {//Start the threads
                if (Parameters.SeaSizeX % Parameters.BlockWidth != 0) throw new Exception();
                if (Parameters.SeaSizeY % Parameters.BlockHeight != 0) throw new Exception();
                int blockCountX = Parameters.SeaSizeX / Parameters.BlockWidth;
                int blockCountY = Parameters.SeaSizeY / Parameters.BlockHeight;
                SeaChunk[,] chunks = new SeaChunk[blockCountX, blockCountY];

                int totalEvenCount = 0;
                int totalOddCount = 0;

                for (int i = 0; i < blockCountX; i++)
                {
                    for (int j = 0; j < blockCountY; j++)
                    {
                        int fromX = i * Parameters.BlockWidth;
                        int toX = fromX + Parameters.BlockWidth - 1;

                        int fromY = j * Parameters.BlockHeight;
                        int toY = fromY + Parameters.BlockHeight - 1;

                        bool isEven = (i % 2) == (j % 2);

                        chunks[i, j] = new SeaChunk
                        {
                            GameParameters = Parameters,
                            FromX = fromX,
                            FromY = fromY,
                            ToX = toX,
                            ToY = toY,
                            IsEven = isEven,
                            Ocean = theSea,
                            Random = new Random(random.Next()),
                        };

                        if (isEven) totalEvenCount++;
                        else totalOddCount++;
                    }
                }

                var tracker = new EvenOddTracker(totalEvenCount, totalOddCount);

                for (int i = 0; i < blockCountX; i++)
                    for (int j = 0; j < blockCountY; j++)
                        chunks[i, j].StartThread(cancellationSource.Token, tracker);
            }


            var window = new MainWindow(Parameters, theSea)
            {
                Width = Parameters.SeaSizeX * 10,
                Height = Parameters.SeaSizeY * 10,
            };



            window.ShowDialog();
            cancellationSource.Cancel();
            Environment.Exit(0);
        }
    }
}
