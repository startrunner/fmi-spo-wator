using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WaTor.Simulation;

namespace WaTor.RenderText
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var simulationParameters = new Parameters
            {
                SeaSizeX = 800,
                SeaSizeY = 800,
                InitialFishCount = 50000,
                InitialSharkCount = 100,
                ThreadSleepTime = TimeSpan.FromSeconds(0.00),
                ScreenRefreshRate = TimeSpan.FromSeconds(1),
                TotalThreadCount = 4,
                FishReproductionRate = 3,
                SharkReproductionRate = 25,
                SharkEnergyLoss = 80,
                EnergyInFish = 100,
                InitialSharkEnergy = 150,
                SimulationDuration = TimeSpan.FromMinutes(1),
            };
            simulationParameters.AssignFromArgs(/*"-TotalThreadCount 3".Split()*/args);

            var random = new Random(11);
            SeaBlock[,] theSea = WaTor.Simulation.WaTor.GenerateSea(simulationParameters, random);
            SeaChunk[,] chunks = WaTor.Simulation.WaTor.ChunkUpSea(simulationParameters, random, theSea);
            var threadStates = Simulation.WaTor.RunSimulation(simulationParameters, chunks);

            DateTime endTime = DateTime.Now + simulationParameters.SimulationDuration;

            using (var logFile = File.OpenWrite("log.txt"))
            using (var logWriter = new StreamWriter(logFile))
            using (var renderFile = File.OpenWrite("rendered.txt"))
            using (var renderWriter = new StreamWriter(renderFile))
            {
                await WriteLine("Config: " + JsonConvert.SerializeObject(simulationParameters, Formatting.Indented) + Environment.NewLine);

                for (long frame = 0; ; ++frame)
                {
                    for (int x = 0; x < simulationParameters.SeaSizeX; x++)
                    {
                        for (int y = 0; y < simulationParameters.SeaSizeY; y++)
                        {
                            int iValue = (int)theSea[x, y].Type;
                            await renderWriter.WriteAsync((char)('0' + iValue));
                        }
                        await renderWriter.WriteLineAsync();
                    }

                    bool willEnd = DateTime.Now >= endTime;
                    if (frame % 100 == 0 || willEnd)
                        await LogStates();

                    if (willEnd)
                    {
                        await logWriter.FlushAsync();
                        await renderWriter.FlushAsync();
                        logWriter.Close();
                        renderWriter.Close();
                        Environment.Exit(0);
                    }

                    async Task LogStates()
                    {
                        await WriteLine($"Time is: {DateTime.Now}");
                        await WriteLine($"Wrote {frame + 1} frames");
                        await WriteLine($"Thread states:{JsonConvert.SerializeObject(threadStates, Formatting.Indented)}");
                        await WriteLine();

                    }
                }
                Task WriteLine(string txt = "")
                {
                    Console.WriteLine(txt);
                    return renderWriter.WriteLineAsync(txt);
                }
            }
        }
    }
}
