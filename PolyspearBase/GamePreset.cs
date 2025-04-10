using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyspearLib
{
    public class UnitPreset
    {
        public List<Unit> Units { get; set; }
        public List<UnitPlacement> Placements { get; set; }

        public UnitPreset()
        {
            Units = new();
        }
    }

    public class UnitPlacement
    {
        public required string UnitId { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        public SIDE Side { get; set; }
        public PLAYER Player { get; set; }
    }
}
