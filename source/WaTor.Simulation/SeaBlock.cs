namespace WaTor.Simulation
{
    public sealed class SeaBlock
    {
        public readonly object Mutex = new { };

        public SeaBlock(SeaBlockType type)
        {
            Type = type;
        }

        public SeaBlockType Type { get; internal set; }
        public long TimeBorn { get; internal set; } = 0;
        public int FishEaten { get; internal set; } = 0;
    }
}
