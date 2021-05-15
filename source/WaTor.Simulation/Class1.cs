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
            _totalEven = totalOdd;
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

        private void DoEven(Action action)
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

        private void DoOdd(Action action)
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
        public int BlockWidth { get; init; }
        public int BlockHeight { get; init; }
        public TimeSpan ThreadSleepTime { get; init; }

        public uint FishReproductionRate { get; init; }
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
        public ulong TimeBorn { get; internal set; } = 0;
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

        int started = 0;
        CancellationToken loopCancellationToken;
        EvenOddTracker evenOddTracker;
        public void StartThread(CancellationToken loopCancellationToken, EvenOddTracker evenOddTracker)
        {
            if (Interlocked.CompareExchange(ref started, 1, comparand: 0) is 0)
            {
                this.evenOddTracker = evenOddTracker;
                this.loopCancellationToken = loopCancellationToken;
                var thread = new Thread(new ThreadStart(Loop));
                thread.Start();
            }
        }

        private void Loop()
        {
            for (ulong time = 0; ; ++time)
            {
                evenOddTracker.Do(IsEven, () => {
                    PerformIteration(time);
                    Thread.Sleep(GameParameters.ThreadSleepTime);
                });
            }
        }

        private void PerformIteration(ulong time)
        {
            Debug.WriteLine("Even: " + IsEven);

            foreach ((int x, int y) in GetCoordinates().RandomShuffle(Random))
            {
                (int x1, int y1) = Random.Move(x, y, GameParameters.SeaSizeX, GameParameters.SeaSizeY);
                SeaBlock current = Ocean[x, y];
                SeaBlock next = Ocean[x1, y1];

                if (current.Type != OceanBlockType.Fish) continue;
                if (next.Type != OceanBlockType.None) continue;

                ulong timeLived = time >= current.TimeBorn ? time - current.TimeBorn : 0;

                next.Type = current.Type;
                next.TimeBorn = current.TimeBorn;
                if (timeLived % GameParameters.FishReproductionRate == 0)
                {
                    current.Type = OceanBlockType.Fish;
                    current.TimeBorn = time;
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
