using System;
using System.Collections.Generic;
using System.Linq;

namespace WaTor.Simulation
{
    public class Parameters
    {
        public int SeaSizeX { get; init; }
        public int SeaSizeY { get; init; }
        public int InitialFishCount { get; init; }
        public int InitialSharkCount { get; init; }
        public TimeSpan ThreadSleepTime { get; init; }

        public int FishReproductionInterval { get; init; }
        public int SharkReproductionInterval { get; init; }
        public int SharkEnergyLoss { get; init; }
        public int EnergyInFish { get; init; }
        public int InitialSharkEnergy { get; init; }
        public int TotalThreadCount { get; init; }
        public TimeSpan ScreenRefreshRate { get; init; }
        public TimeSpan SimulationDuration { get; init; }
        public bool RandomizeInitialPozitions { get; set; }

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
}
