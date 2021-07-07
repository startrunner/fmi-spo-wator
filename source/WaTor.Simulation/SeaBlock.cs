namespace WaTor.Simulation
{
    public struct SeaBlock
    {

        public SeaBlock(SeaBlockType type)
        {
            Type = type;
            TimeBorn = 0;
            FishEaten = 0;
        }

        public SeaBlockType Type { get; internal set; }
        public long TimeBorn { get; internal set; }
        public int FishEaten { get; internal set; }
    }
}
