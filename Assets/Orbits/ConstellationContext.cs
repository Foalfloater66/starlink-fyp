using Orbits.Satellites;

namespace Orbits
{
    public struct ConstellationContext
    {
        public Satellite[] satlist;
        public float km_per_unit;
        public int maxsats;
        public float maxdist;
        public float margin;
    }


    // maxsats, maxdist, margin, km_per_unit, graph_on, grid
}