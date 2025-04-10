using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyspearLib
{
    [Serializable]
    public sealed class BoardState
    {
        public IReadOnlyDictionary<string, Hex> UnitsPositions { get; }
        public IReadOnlyDictionary<Hex, string> HexesWithUnits { get; }
        public Dictionary<string, Unit> AliveUnits { get; }

        public Dictionary<string, HexGrid.SIDE> UnitRotations { get; }

        public BoardState(HexGrid grid)
        {
            UnitsPositions = new Dictionary<string, Hex>(grid.UnitsPositions);

            HexesWithUnits = new Dictionary<Hex, string>(grid.HexesWithUnits);

            AliveUnits = new Dictionary<string, Unit>(grid.AliveUnits);

            UnitRotations = new();

            foreach (var unit in AliveUnits)
            {
                UnitRotations[unit.Key] = unit.Value.Rotation;
            }
        }
    }

}
