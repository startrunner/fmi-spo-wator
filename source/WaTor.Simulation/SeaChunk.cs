using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WaTor.Simulation
{

    public class SeaChunk
    {
        public SeaBlock[,] Ocean { get; init; }
        public Parameters GameParameters { get; init; }
        public int FromX { get; init; }
        public int ToX { get; init; }
        public int FromY { get; init; }
        public int ToY { get; init; }
        public bool IsEven { get; init; }
        public Random Random { get; init; }

        public void PerformIteration(long time)
        {
            Debug.WriteLine("Even: " + IsEven);

            IReadOnlyList<(int x, int y)> shuffledCoordinates = GetCoordinates().RandomShuffle(Random).ToList(); ;

            foreach ((int x, int y) in shuffledCoordinates)
            {
                SeaBlock current, next;
                int x1, y1;
                try
                {
                    (x1, y1) = Random.Move(x, y, GameParameters.SeaSizeX, GameParameters.SeaSizeY);
                    current = Ocean[x, y];
                    next = Ocean[x1, y1];
                }
                catch (Exception e) { throw; }

                if (current.Type != SeaBlockType.Fish) continue;
                if (next.Type != SeaBlockType.None) continue;

                long timeLived = time >= current.TimeBorn ? time - current.TimeBorn : 0;

                next.Type = current.Type;
                next.TimeBorn = current.TimeBorn;
                if (timeLived > 0 && timeLived % GameParameters.FishReproductionInterval == 0)
                {
                    current.Type = SeaBlockType.Fish;
                    current.TimeBorn = time;
                }
                else current.Type = SeaBlockType.None;

            }
            foreach ((int currentX, int currentY) in shuffledCoordinates)
            {
                SeaBlock current = Ocean[currentX, currentY];

                if (current.Type != SeaBlockType.Shark) continue;

                long timeLived = time >= current.TimeBorn ? time - current.TimeBorn : 0;
                long energy =
                    GameParameters.InitialSharkEnergy
                    - timeLived * GameParameters.SharkEnergyLoss
                    + current.FishEaten * GameParameters.EnergyInFish;

                if (energy <= 0)
                {
                    current.Type = SeaBlockType.None;
                    continue;
                }

                (int x, int y)[] adjacentFish = new[]{
                    (currentX-1, currentY),
                    (currentX+1, currentY),
                    (currentX, currentY-1),
                    (currentX, currentY+1),
                }.Select(item =>
                {
                    (int x, int y) = item;

                    if (x == -1) x = GameParameters.SeaSizeX - 1;
                    if (y == -1) y = GameParameters.SeaSizeY - 1;
                    if (x == GameParameters.SeaSizeX) x = 0;
                    if (y == GameParameters.SeaSizeY) y = 0;

                    return (x, y);
                })
                .Where(item => Ocean[item.x, item.y].Type == SeaBlockType.Fish)
                .ToArray();

                int nextX, nextY;
                if (adjacentFish.Length > 0)
                {
                    int ix = Random.Next(adjacentFish.Length);
                    (nextX, nextY) = adjacentFish[ix];
                }
                else
                {
                    (nextX, nextY) = Random.Move(currentX, currentY, GameParameters.SeaSizeX, GameParameters.SeaSizeY);
                }
                SeaBlock next = Ocean[nextX, nextY];

                if (next.Type == SeaBlockType.Shark) continue;
                bool ateFish = next.Type == SeaBlockType.Fish;

                next.Type = current.Type;
                next.TimeBorn = current.TimeBorn;
                next.FishEaten = current.FishEaten + (ateFish ? 1 : 0);
                if (timeLived > 0 && timeLived % GameParameters.SharkReproductionInterval == 0)
                {
                    current.Type = SeaBlockType.Shark;
                    current.TimeBorn = time;
                    current.FishEaten = 0;
                }
                else current.Type = SeaBlockType.None;
            }
        }

        private IEnumerable<(int x, int y)> GetCoordinates()
        {
            for (int x = FromX; x <= ToX; x++)
                for (int y = FromY; y <= ToY; y++)
                    yield return (x, y);
        }
    }
}
