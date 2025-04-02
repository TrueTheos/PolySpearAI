using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    [Serializable]
    public sealed class PreMove
    {
        public IReadOnlyDictionary<string, Hex> UnitsPositions { get; }
        public IReadOnlyDictionary<Hex, string> HexesWithUnits { get; }
        public IReadOnlySet<Unit> AllUnits { get; }

        public Dictionary<string, HexGrid.SIDE> UnitRotations { get; }

        public PreMove(HexGrid grid)
        {
            UnitsPositions = new Dictionary<string, Hex>(grid.UnitsPositions);

            HexesWithUnits = new Dictionary<Hex, string>(grid.HexesWithUnits);

            AllUnits = new HashSet<Unit>(grid.AllUnits.Select(u => u with { }));

            UnitRotations = new();

            foreach (var unit in AllUnits)
            {
                UnitRotations[unit.ID] = unit.Rotation;
            }
        }
    }

}
