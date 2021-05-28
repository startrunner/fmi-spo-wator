using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace WaTor.Simulation
{
    public class EvenOddTracker
    {
        public EvenOddTracker(int totalEven, int totalOdd)
        {
            _totalEven = totalEven;
            _totalOdd = totalOdd;
            //this.evenSemaphore = new Semaphore(totalEven, totalEven);
            //this.oddSemaphore = new Semaphore(0, totalOdd);
        }

        readonly int _totalEven, _totalOdd;
        //Semaphore evenSemaphore, oddSemaphore;
        object _mutex = new { };
        volatile bool _doingEven = true;
        volatile int _activeCount = 0;

        public void Do(bool isEven, Action action)
        {
            if (isEven) DoEven(action);
            else DoOdd(action);
        }

        public void DoEven(Action action)
        {
            if (_totalOdd == 0)
            {
                action();
                return;
            }


            while (!_doingEven) { }
            while (true)
            {
                lock (_mutex)
                {
                    if (_doingEven)
                    {
                        _activeCount++;
                        break;
                    }
                }
            }

            action();

            lock (_mutex)
            {
                if (_activeCount >= _totalEven)
                {
                    _activeCount = 0;
                    _doingEven = false;
                }
            }

        }

        public void DoOdd(Action action)
        {
            if (_totalEven == 0)
            {
                action();
                return;
            }

            while (_doingEven) { }
            while (true)
            {
                lock (_mutex)
                {
                    if (!_doingEven)
                    {
                        _activeCount++;
                        break;
                    }
                }
            }

            action();

            lock (_mutex)
            {
                if (_activeCount >= _totalOdd)
                {
                    _activeCount = 0;
                    _doingEven = true;
                }
            }
        }
    }


    public class Parameters
    {
        public int SeaSizeX { get; init; }
        public int SeaSizeY { get; init; }
        public int InitialFishCount { get; init; }
        public int InitialSharkCount { get; init; }
        public TimeSpan ThreadSleepTime { get; init; }

        public int FishReproductionRate { get; init; }
        public int SharkReproductionRate { get; init; }
        public int SharkEnergyLoss { get; init; }
        public int EnergyInFish { get; init; }
        public int InitialSharkEnergy { get; init; }
        public int TotalThreadCount { get; init; }
        public TimeSpan ScreenRefreshRate { get; init; }
        public TimeSpan SimulationDuration { get; init; }

        public void AssignFromArgs(IReadOnlyList<string> args)
        {
            Dictionary<string, string> map = new(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < args.Count - 1; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    map[args[i].Trim('-')] = args[i + 1];
                }
            }
            ;
            var fields = GetType().GetProperties().Where(x => x.GetSetMethod() != null);
            foreach (var field in fields)
            {
                if (!map.TryGetValue(field.Name, out string value))
                    continue;

                if (field.PropertyType == typeof(int))
                    field.SetValue(this, int.Parse(value));
                else if (field.PropertyType == typeof(TimeSpan))
                    field.SetValue(this, TimeSpan.FromMilliseconds(int.Parse(value)));
                else
                    throw new NotSupportedException("ebi si mamata");
            }
        }
    }

    public static class RandomExtensions
    {
        public static (int x, int y) Move(this Random random, int previousX, int previousY, int sizeX, int sizeY)
        {
            int x = previousX, y = previousY;

            if (random.Next() % 2 == 0)
            {
                if (random.Next() % 2 == 0) ++x;
                else --x;
            }
            else
            {
                if (random.Next() % 2 == 0) ++y;
                else --y;
            }

            if (x < 0) x = sizeX - 1;
            else if (x >= sizeX) x = 0;

            if (y < 0) y = sizeY - 1;
            else if (y >= sizeY) y = 0;

            return (x, y);
        }

        public static IEnumerable<T> RandomShuffle<T>(this IEnumerable<T> items, Random random) =>
            items
            .Select(item => (item, order: random.Next()))
            .OrderBy(x => x.order)
            .Select(x => x.item);
    }

    public enum OceanBlockType
    {
        Fish,
        Shark,
        None,
    }

    public sealed class SeaBlock
    {
        public readonly object Mutex = new { };

        public SeaBlock(OceanBlockType type)
        {
            Type = type;
        }

        public OceanBlockType Type { get; internal set; }
        public long TimeBorn { get; internal set; } = 0;
        public int FishEaten { get; internal set; } = 0;
    }

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
                (int x1, int y1) = Random.Move(x, y, GameParameters.SeaSizeX, GameParameters.SeaSizeY);
                SeaBlock current = Ocean[x, y];
                SeaBlock next = Ocean[x1, y1];

                if (current.Type != OceanBlockType.Fish) continue;
                if (next.Type != OceanBlockType.None) continue;

                long timeLived = time >= current.TimeBorn ? time - current.TimeBorn : 0;

                next.Type = current.Type;
                next.TimeBorn = current.TimeBorn;
                if (timeLived > 0 && timeLived % GameParameters.FishReproductionRate == 0)
                {
                    current.Type = OceanBlockType.Fish;
                    current.TimeBorn = time;
                }
                else current.Type = OceanBlockType.None;

            }
            foreach ((int currentX, int currentY) in shuffledCoordinates)
            {
                SeaBlock current = Ocean[currentX, currentY];

                if (current.Type != OceanBlockType.Shark) continue;

                long timeLived = time >= current.TimeBorn ? time - current.TimeBorn : 0;
                long energy =
                    GameParameters.InitialSharkEnergy
                    - timeLived * GameParameters.SharkEnergyLoss
                    + current.FishEaten * GameParameters.EnergyInFish;

                if (energy <= 0)
                {
                    current.Type = OceanBlockType.None;
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
                .Where(item => Ocean[item.x, item.y].Type == OceanBlockType.Fish)
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

                if (next.Type == OceanBlockType.Shark) continue;
                bool ateFish = next.Type == OceanBlockType.Fish;

                next.Type = current.Type;
                next.TimeBorn = current.TimeBorn;
                next.FishEaten = current.FishEaten + (ateFish ? 1 : 0);
                if (timeLived > 0 && timeLived % GameParameters.SharkReproductionRate == 0)
                {
                    current.Type = OceanBlockType.Shark;
                    current.TimeBorn = time;
                    current.FishEaten = 0;
                }
                else current.Type = OceanBlockType.None;
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
