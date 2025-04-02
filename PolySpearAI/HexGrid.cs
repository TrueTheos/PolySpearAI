using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Unit;

namespace PolySpearAI
{
    public class HexGrid
    {
        public int Width { get; }
        public int Height { get; }
        private readonly Hex[,] _hexes;

        public static readonly int[][][] OddrDirectionDifferences =
        {
             // Even rows
             [
                 [0, -1], [1, 0], [0, 1],
                 [-1, 1], [-1, 0], [-1, -1]
             ],
             // Odd rows
             [
                 [1, -1], [1, 0], [1, 1],
                 [0, 1], [-1, 0], [0, -1]
             ]
        };

        public Dictionary<string, Hex> UnitsPositions { get; private set; } = new();
        public Dictionary<Hex, string> HexesWithUnits { get; private set; } = new();
        public HashSet<Unit> AllUnits { get; private set; } = new();

        public Stack<PreMove> MoveHistory = new();

        public enum SIDE
        {
            UP_RIGHT = 0,
            RIGHT = 1,
            DOWN_RIGHT = 2,
            DOWN_LEFT = 3,
            LEFT = 4,
            UP_LEFT = 5
        }
        private static readonly SIDE[] _sides = (SIDE[])Enum.GetValues(typeof(SIDE));

        public HexGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _hexes = new Hex[width, height];

            for (int r = 0; r < height; r++)
            {
                for (int q = 0; q < width - (r % 2 == 0 ? 0 : 1); q++)
                {
                    _hexes[q,r] = new Hex(q, r);
                }
            }
        }

        public Hex GetHex(Unit unit)
        {
            UnitsPositions.TryGetValue(unit.ID, out Hex? result);
            return result;
        }

        public Hex GetHex(int q, int r)
        {
            if (r >= 0 && r < Height && q >= 0 && q < Width)
                return _hexes[q,r];
            return null;
        }

        public Unit GetUnitById(string id)
        {
            return AllUnits.FirstOrDefault(x => x.ID == id);
        }

        public Unit GetUnitAtHex(Hex hex)
        {
            if (hex == null) return null;
            HexesWithUnits.TryGetValue(hex, out string? unitID);
            if (unitID == null) return null;

            return AllUnits.FirstOrDefault(x => x.ID == unitID);
        }

        public SIDE DirectionTo(Hex start, Hex neighbor)
        {
            int parity = start.R & 1;
            int dq = neighbor.Q - start.Q;
            int dr = neighbor.R - start.R;

            for (int i = 0; i < HexGrid.OddrDirectionDifferences[parity].Length; i++)
            {
                var diff = HexGrid.OddrDirectionDifferences[parity][i];
                if (dq == diff[0] && dr == diff[1])
                {
                    return (SIDE)i;
                }
            }

            throw new ArgumentException("Hexes are not adjacent");
        }

        public Dictionary<SIDE, Hex> GetNeighbors(Hex hex)
        {
            if (hex == null) return new();
            int parity = hex.R & 1;
            Dictionary<SIDE, Hex> neighbors = new();

            for (int i = 0; i < OddrDirectionDifferences[parity].Length; i++)
            {
                var diff = OddrDirectionDifferences[parity][i];
                Hex neighbor = GetHex(hex.Q + diff[0], hex.R + diff[1]);
                if (neighbor != null) neighbors[(SIDE)i] = neighbor;
            }

            return neighbors;
        }

        public Hex GetNeighbor(Hex hex, SIDE side)
        {
            GetNeighbors(hex).TryGetValue(side, out Hex result);
            return result;
        }

        public List<Hex> AllowedMoves(Unit unit)
        {
            var moves = new List<Hex>();

            if (unit == null)
                return moves;

            int currentRotation = (int)unit.Rotation;

            var neighbors = GetNeighbors(GetHex(unit));
            foreach (int dirValue in _sides)
            {
                SIDE side = (SIDE)dirValue;
                if (neighbors.TryGetValue(side, out Hex neighborHex))
                {
                    Unit neighborUnit = GetUnitAtHex(neighborHex);
                    if (neighborUnit == null || neighborUnit.Player != unit.Player)
                    {
                        moves.Add(neighborHex);
                    }
                }
            }
            return moves;
        }

        public bool MoveUnit(Unit unit, Hex to)
        {
            if (!AllowedMoves(unit).Contains(to))
            {
                return false;
            }

            Hex from = GetHex(unit);

            SIDE moveDirection = DirectionTo(from, to);

            MoveHistory.Push(new PreMove(this));
            return PerformMovement(unit, from, to, moveDirection);
        }

        public bool UndoMove()
        {
            if (MoveHistory.Count == 0) return false;
            PreMove move = MoveHistory.Pop();
            ApplyMove(move);
            return true;
        }

        public void ApplyMove(PreMove move)
        {
            UnitsPositions = new Dictionary<string, Hex>(move.UnitsPositions);
            HexesWithUnits = new Dictionary<Hex, string>(move.HexesWithUnits);
            AllUnits = new HashSet<Unit>(move.AllUnits);

            foreach (var unit in AllUnits)
            {
                unit.Rotation = move.UnitRotations[unit.ID];
            }
        }

        private bool PerformMovement(Unit unit, Hex currentPosition, Hex destination, SIDE moveDirection)
        {
            unit.Rotation = moveDirection;

            if (IsVulnerableToSpear(unit, currentPosition))
            {
                KillUnit(unit);
                return true;
            }

            // Check if destination has an enemy unit that can be attacked
            if (HexesWithUnits.TryGetValue(destination, out string targetUnitId) && GetUnitById(targetUnitId).Player != unit.Player)
            {
                Unit targetUnit = GetUnitById(targetUnitId);
                // Can only move to an occupied hex if the weapon in the move direction would kill the target
                if (!CanKillUnit(unit, targetUnit, moveDirection))
                    return false;

                // Kill the target unit
                KillUnit(targetUnit);
            }
            else if (HexesWithUnits.ContainsKey(destination))
            {
                // Can't move to a hex occupied by a friendly unit
                return false;
            }

            ActivateWeaponEffectsBeforeMovement(unit, currentPosition, destination, moveDirection);

            UnitsPositions.Remove(unit.ID);
            HexesWithUnits.Remove(currentPosition);

            UnitsPositions[unit.ID] = destination;
            HexesWithUnits[destination] = unit.ID;

            if (IsVulnerableToSpear(unit, destination))
            {
                KillUnit(unit);
                return true;
            }

            ActivateWeaponEffectsAfterMovement(unit, destination);

            return true;
        }

        private void ActivateWeaponEffectsBeforeMovement(Unit unit, Hex currentPosition, Hex destination, SIDE moveDirection)
        {
            WEAPON weaponInMoveDirection = unit.GetItemOnSide((int)moveDirection);

            if (weaponInMoveDirection == WEAPON.AXE || weaponInMoveDirection == WEAPON.STRONG_AXE)
            {
                KillAdjacentEnemies(unit, destination);
            }

            if (weaponInMoveDirection == WEAPON.BOW)
            {
                FireBow(unit, currentPosition, moveDirection);
            }

            if (weaponInMoveDirection == WEAPON.PUSH)
            {
                PushUnit(unit, destination, moveDirection);
            }
        }

        private void ActivateWeaponEffectsAfterMovement(Unit unit, Hex position)
        {
            foreach (SIDE side in _sides)
            {
                if (unit.GetItemOnSide((int)side) == WEAPON.SPEAR)
                {
                    try
                    {
                        Hex targetPos = GetNeighbor(position,side);
                        if (HexesWithUnits.TryGetValue(targetPos, out string targetUnitId) &&
                            GetUnitById(targetUnitId).Player != unit.Player)
                        {
                            // Check if target has a shield
                            Unit targetUnit = GetUnitById(targetUnitId);
                            SIDE defendDirection = (SIDE)(((int)side + 3) % 6);
                            WEAPON defendWeapon = targetUnit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != WEAPON.SHIELD && defendWeapon != WEAPON.STRONG_SHIELD)
                            {
                                KillUnit(targetUnit);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore if hex is outside the board
                    }
                }
            }
        }

        private void KillAdjacentEnemies(Unit unit, Hex position)
        {
            List<Unit> unitsToKill = new List<Unit>();  

            // Check all adjacent hexes for enemies
            foreach (SIDE side in _sides)
            {
                try
                {
                    Hex neighborPos = GetNeighbor(position,side);
                    if (HexesWithUnits.TryGetValue(neighborPos, out string neighborUnitId) &&
                        GetUnitById(neighborUnitId).Player != unit.Player)
                    {
                        unitsToKill.Add(GetUnitById(neighborUnitId));
                    }
                }
                catch
                {
                    // Ignore if hex is outside the board
                }
            }

            // Kill all marked units
            foreach (var unitToKill in unitsToKill)
            {
                KillUnit(unitToKill);
            }
        }

        private void FireBow(Unit unit, Hex position, SIDE direction)
        {
            // Start at the unit's position
            Hex currentPos = position;

            // Keep going in the direction until we hit something or reach the edge
            while (true)
            {
                try
                {
                    // Move to the next hex in the direction
                    currentPos = GetNeighbor(currentPos,direction);

                    // Check if there's a unit there
                    if (HexesWithUnits.TryGetValue(currentPos, out string targetUnitId))
                    {
                        Unit targetUnit = GetUnitById(targetUnitId);
                        // If it's an enemy unit, check for shield and kill if not shielded
                        if (targetUnit.Player != unit.Player)
                        {
                            SIDE defendDirection = (SIDE)(((int)direction + 3) % 6);
                            WEAPON defendWeapon = targetUnit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != WEAPON.SHIELD && defendWeapon != WEAPON.STRONG_SHIELD)
                            {
                                KillUnit(targetUnit);
                            }

                            // Arrow stops regardless of whether it killed or not
                            break;
                        }
                        else
                        {
                            // Friendly units block arrows
                            break;
                        }
                    }
                }
                catch
                {
                    // Reached the edge of the board
                    break;
                }
            }
        }

        private void PushUnit(Unit pusher, Hex pushPosition, SIDE pushDirection)
        {
            // Check if there's a unit at the push position
            if (HexesWithUnits.TryGetValue(pushPosition, out string targetUnitId) &&
                GetUnitById(targetUnitId).Player != pusher.Player)
            {
                // Calculate the position the target will be pushed to
                Unit targetUnit = GetUnitById(targetUnitId);
                Hex pushDestination;
                try
                {
                    pushDestination = GetNeighbor(pushPosition, pushDirection);

                    // If the push destination is occupied or off the board, the target unit dies
                    if (HexesWithUnits.ContainsKey(pushDestination))
                    {
                        KillUnit(targetUnit);
                    }
                    else
                    {
                        // Move the target to the push destination
                        UnitsPositions.Remove(targetUnitId);
                        HexesWithUnits.Remove(pushPosition);
                        UnitsPositions[targetUnitId] = pushDestination;
                        HexesWithUnits[pushDestination] = targetUnitId;

                        // Check if the pushed unit is now vulnerable to a spear
                        if (IsVulnerableToSpear(targetUnit, pushDestination))
                        {
                            KillUnit(targetUnit);
                        }
                    }
                }
                catch
                {
                    // Pushed off the board, unit dies
                    KillUnit(targetUnit);
                }
            }
        }

        private bool CanKillUnit(Unit attacker, Unit target, SIDE attackDirection)
        {
            WEAPON attackWeapon = attacker.GetItemOnSide((int)attackDirection);

            // The opposite direction from the attack direction
            SIDE defendDirection = (SIDE)(((int)attackDirection + 3) % 6);

            // Get the weapon used for defense
            WEAPON defendWeapon = target.GetItemOnSide((int)defendDirection);

            // If defender has a shield in the right direction, they're protected
            if (defendWeapon == WEAPON.SHIELD || defendWeapon == WEAPON.STRONG_SHIELD)
                return false;

            // AXE/STRONG_AXE can kill if not blocked by shield
            if (attackWeapon == WEAPON.AXE || attackWeapon == WEAPON.STRONG_AXE)
                return true;

            // PUSH can always move to occupied hex (it will push the enemy)
            if (attackWeapon == WEAPON.PUSH) //todo
                return true;

            return false;
        }

        private bool IsVulnerableToSpear(Unit unit, Hex position)
        {
            // Check all adjacent hexes for enemy units with spears pointing at this unit
            foreach (SIDE side in _sides)
            {   
                try
                {
                    Hex neighborPos = GetNeighbor(position, side);
                    if (HexesWithUnits.TryGetValue(neighborPos, out string neighborUnitId) &&
                        GetUnitById(neighborUnitId).Player != unit.Player)
                    {
                        Unit neighborUnit = GetUnitById(neighborUnitId);
                        // The side pointing at our unit
                        SIDE pointingDirection = (SIDE)(((int)side + 3) % 6);

                        // Check if neighbor has a spear pointing at our unit
                        WEAPON neighborWeapon = neighborUnit.GetItemOnSide((int)pointingDirection);
                        if (neighborWeapon == WEAPON.SPEAR)
                        {
                            // Check if our unit has a shield in that direction
                            SIDE defendDirection = side;
                            WEAPON defendWeapon = unit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != WEAPON.SHIELD && defendWeapon != WEAPON.STRONG_SHIELD)
                                return true; // Unit is vulnerable to spear
                        }
                    }
                }
                catch
                {
                    // Ignore if hex is outside the board
                }
            }
            return false;
        }

        private void KillUnit(Unit unit)
        {
            HexesWithUnits.Remove(UnitsPositions[unit.ID]);
            UnitsPositions.Remove(unit.ID);
            AllUnits.Remove(unit);
        }

        public HashSet<Unit> GetUnitsByPlayer(PLAYER player)
        {
            return AllUnits.Where(x => x.Player == player).ToHashSet();
        }

        public void PlaceUnit(int q, int r, Unit unit, SIDE facing)
        {
            Hex hex = GetHex(q, r);
            if (hex == null)
            {
                Console.WriteLine($"Hex ({q},{r}) does not exist.");
                return;
            }
            Unit occupant = GetUnitAtHex(hex);
            if (occupant != null)
            {
                Console.WriteLine($"Hex ({q},{r}) is already occupied by {occupant}.");
                return;
            }
            AllUnits.Add(unit);
            UnitsPositions[unit.ID] = hex;
            HexesWithUnits[hex] = unit.ID;
            unit.Rotation = facing;
        }

        public void PrintGrid()
        {
            // IGNORE THIS MESS, QUICK SOLUTION

            for (int r = 0; r < Height; r++)
            {
                if (r % 2 == 1) Console.Write("   ");

                int currWidth = Width - (r % 2 == 0 ? 0 : 1);
                for (int q = 0; q < currWidth; q++)
                {
                    Unit unit = GetUnitAtHex(GetHex(q, r));
                    string unitId = "  ";
                    ConsoleColor originalColor = Console.ForegroundColor;
                    ConsoleColor playerColor = ConsoleColor.White;
                    if (unit != null)
                    {
                        unitId = unit.ID;
                        playerColor = unit.Player == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    }

                    if (unit != null)
                    {
                        switch (unit.Rotation)
                        {
                            case SIDE.UP_RIGHT:
                                Console.ForegroundColor = originalColor;
                                Console.Write($@" /{q}");
                                Console.ForegroundColor = playerColor;
                                Console.Write(@"\  ");
                                break;
                            case SIDE.UP_LEFT:
                                Console.ForegroundColor = playerColor;
                                Console.Write($@" /{q}");
                                Console.ForegroundColor = originalColor;
                                Console.Write(@"\  ");
                                break;
                            default:
                                Console.Write($@" /{q}\  ");
                                break;
                        }
                    }
                    else
                    {
                        Console.Write($@" /{q}\  ");
                    }
                    Console.ForegroundColor = originalColor;
                }
                Console.WriteLine();

                if (r % 2 == 1) Console.Write("   ");
                for (int q = 0; q < currWidth; q++)
                {
                    Unit unit = GetUnitAtHex(GetHex(q, r));
                    string unitId = "  ";
                    ConsoleColor originalColor = Console.ForegroundColor;
                    ConsoleColor playerColor = ConsoleColor.White;
                    if (unit != null)
                    {
                        unitId = unit.ID;
                        playerColor = unit.Player == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    }

                    if (unit != null)
                    {
                        switch (unit.Rotation)
                        {
                            case SIDE.LEFT:
                                Console.ForegroundColor = playerColor;
                                Console.Write(@$"|");
                                Console.ForegroundColor = originalColor;
                                Console.Write(@$" {unitId}| ");
                                break;
                            case SIDE.RIGHT:
                                Console.ForegroundColor = originalColor;
                                Console.Write($@"| {unitId}");
                                Console.ForegroundColor = playerColor;
                                Console.Write(@"| ");
                                break;
                            default:
                                Console.Write($@"| {unitId}| ");
                                break;
                        }
                    }
                    else
                    {
                        Console.Write($@"| {unitId}| ");
                    }
                    Console.ForegroundColor = originalColor;
                }
                Console.WriteLine();

                if (r % 2 == 1) Console.Write("   ");
                for (int q = 0; q < currWidth; q++)
                {
                    Unit unit = GetUnitAtHex(GetHex(q, r));
                    ConsoleColor originalColor = Console.ForegroundColor;
                    ConsoleColor playerColor = ConsoleColor.White;
                    if (unit != null)
                    {
                        playerColor = unit.Player == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    }

                    if (unit != null)
                    {
                        switch (unit.Rotation)
                        {
                            case SIDE.DOWN_RIGHT:
                                Console.ForegroundColor = originalColor;
                                Console.Write(@" \");
                                Console.ForegroundColor = playerColor;
                                Console.Write($@"{r}/");
                                Console.ForegroundColor = originalColor;
                                Console.Write(@"  ");
                                break;
                            case SIDE.DOWN_LEFT:
                                Console.ForegroundColor = playerColor;
                                Console.Write(@" \");
                                Console.ForegroundColor = originalColor;
                                Console.Write($@"{r}/");
                                Console.Write(@"  ");
                                break;
                            default:
                                Console.Write($@" \{r}/  ");
                                break;
                        }
                    }
                    else
                    {
                        Console.Write($@" \{r}/  ");
                    }
                    Console.ForegroundColor = originalColor;
                }
                Console.WriteLine();
            }
        }
    }
}
