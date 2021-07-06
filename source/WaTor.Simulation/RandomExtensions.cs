using System;
using System.Collections.Generic;
using System.Linq;

namespace WaTor.Simulation
{
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
}
