using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WaTor.Simulation
{
    public static class WaTor
    {

        public static SeaChunk[,] ChunkUpSea(Parameters parameters, Random random, SeaBlock[,] theSea)
        {
            SeaChunk[,] chunks;
            {//Chunk up the sea
                int chunkCountOnSide = (int)Math.Sqrt(parameters.TotalThreadCount);
                if (chunkCountOnSide % 2 == 1) ++chunkCountOnSide;


                int defaultChunkSizeX = parameters.SeaSizeX / chunkCountOnSide;
                int defaultChunkSizeY = parameters.SeaSizeY / chunkCountOnSide;
                int chunkSizeRemainderX = parameters.SeaSizeX % chunkCountOnSide;
                int chunkSizeRemainderY = parameters.SeaSizeY % chunkCountOnSide;

                chunks = new SeaChunk[chunkCountOnSide, chunkCountOnSide];

                int fromX = 0, toX;
                for (int i = 0; i < chunkCountOnSide; i++)
                {
                    toX = fromX + defaultChunkSizeX - 1;
                    if (i < chunkSizeRemainderX) ++toX;

                    int fromY = 0, toY;
                    for (int j = 0; j < chunkCountOnSide; j++)
                    {
                        toY = fromY + defaultChunkSizeY - 1;
                        if (j < chunkSizeRemainderY) ++toY;

                        chunks[i, j] = new SeaChunk
                        {
                            FromX = fromX,
                            FromY = fromY,
                            ToX = toX,
                            ToY = toY,
                            GameParameters = parameters,
                            IsEven = i % 2 == j % 2,
                            Ocean = theSea,
                            Random = new Random(random.Next()),
                        };

                        fromY = toY + 1;
                    }
                    fromX = toX + 1;
                }
            }

            VerifyChunks(parameters, chunks);
            return chunks;
        }

        private static void VerifyChunks(Parameters parameters, SeaChunk[,] chunks)
        {
            int[,] verification = new int[parameters.SeaSizeX, parameters.SeaSizeY];
            foreach (SeaChunk chunk in chunks)
            {
                for (int x = chunk.FromX; x <= chunk.ToX; x++)
                    for (int y = chunk.FromY; y <= chunk.ToY; y++)
                        verification[x, y]++;
            }

            if (verification.OfType<int>().Count(x => x == 1) != parameters.SeaSizeX * parameters.SeaSizeY)
                throw new Exception("Bad chunks generated");
        }

        public sealed class ThreadState
        {
            public long Time = -1;
        }

        public static ThreadState[] RunSimulation(Parameters Parameters, SeaChunk[,] chunks)
        {
            IReadOnlyList<(SeaChunk Chunk, int Thread)> chunksAndThreads =
                            chunks
                            .OfType<SeaChunk>()
                            .OrderBy(x => x.IsEven ? 0 : 1)
                            .Select((x, i) => (Chunk: x, Thread: i % Parameters.TotalThreadCount))
                            .ToList();

            int totalEvenChunks = chunksAndThreads.Count(x => x.Chunk.IsEven);
            int totalOddChunks = chunksAndThreads.Count(x => !x.Chunk.IsEven);

            object mutex = new { };

            var tracker = new EvenOddTracker(totalEvenChunks, totalOddChunks);

            var threadStates = Enumerable.Range(0, Parameters.TotalThreadCount).Select(x => new ThreadState()).ToArray();

            for (int threadId = 0; threadId < Parameters.TotalThreadCount; threadId++)
            {
                IReadOnlyList<SeaChunk> evenChunks =
                    chunksAndThreads
                    .Where(x => x.Thread == threadId && x.Chunk.IsEven)
                    .Select(x => x.Chunk)
                    .ToArray();

                IReadOnlyList<SeaChunk> oddChunks =
                    chunksAndThreads
                    .Where(x => x.Thread == threadId && !x.Chunk.IsEven)
                    .Select(x => x.Chunk)
                    .ToArray();

                ThreadState threadState =
                    threadStates[threadId];


                var thread = new Thread(() =>
                {
                    for (long time = 0; ; ++time)
                    {
                        foreach (var even in evenChunks)
                        {
                            tracker.DoEven(() => even.PerformIteration(time));
                        }
                        foreach (var odd in oddChunks)
                        {
                            tracker.DoOdd(() => odd.PerformIteration(time));
                        }

                        threadState.Time = time;
                    }
                });
                thread.Priority = ThreadPriority.Normal;
                thread.Start();
            }

            return threadStates;
        }

        public static SeaBlock[,] GenerateSea(Parameters Parameters, Random random)
        {
            SeaBlock[,] theSea;
            {//Generate initial sea
                theSea = new SeaBlock[Parameters.SeaSizeX, Parameters.SeaSizeY];

                if (Parameters.RandomizeInitialPozitions)
                {
                    (int x, int y) GetRandomPosition()
                    {
                        int x, y;
                        do
                        {
                            x = random.Next(Parameters.SeaSizeX);
                            y = random.Next(Parameters.SeaSizeY);
                        } while (theSea[x, y] != null);
                        return (x, y);
                    }

                    int fishToSpawn = Parameters.InitialFishCount;
                    int sharksToSpawn = Parameters.InitialSharkCount;
                    while (fishToSpawn > 0 && sharksToSpawn > 0)
                    {
                        (int baseX, int baseY) = GetRandomPosition();
                        for (int i = 0; i < 16; i++)
                        {
                            for (int j = 0; j < 16; j++)
                            {
                                int x = (baseX + i) % Parameters.SeaSizeX;
                                int y = (baseY + j) % Parameters.SeaSizeY;
                                if (theSea[x, y] != null) continue;

                                bool willBeFish = (sharksToSpawn <= 0 || (j % 5 <3)) && fishToSpawn >= 0;
                                bool willBeShark = sharksToSpawn >= 0 && !willBeFish;

                                if (willBeFish)
                                {
                                    theSea[x, y] = new SeaBlock(SeaBlockType.Fish);
                                    --fishToSpawn;
                                }
                                else if (willBeShark)
                                {
                                    theSea[x, y] = new SeaBlock(SeaBlockType.Shark);
                                    --sharksToSpawn;
                                }

                            }
                        }

                    }
                }
                else
                {
                    int fishCount = 0, sharkCount = 0;
                    for (int x = 0; x < Parameters.SeaSizeX; x++)
                    {
                        for (int y = 0; y < Parameters.SeaSizeY; y++)
                        {
                            if (++fishCount <= Parameters.InitialFishCount)
                            {
                                theSea[x, y] = new SeaBlock(SeaBlockType.Fish);
                            }
                            else if (++sharkCount <= Parameters.InitialSharkCount)
                            {
                                theSea[x, y] = new SeaBlock(SeaBlockType.Shark);
                            }
                        }
                    }
                }

                for (int x = 0; x < Parameters.SeaSizeX; x++)
                    for (int y = 0; y < Parameters.SeaSizeY; y++)
                        if (theSea[x, y] is null)
                            theSea[x, y] = new SeaBlock(SeaBlockType.None);
            }

            return theSea;
        }
    }
}
