using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    [Serializable]
    public record Hex
    {
        public int Q { get; } // Column
        public int R { get; } // Row
        public Hex(int q, int r)
        {
            Q = q;
            R = r;
        }
    }

    [Serializable]
    public record PreMove
    {
        public Dictionary<Hex, Unit> UnitsPositions;
        public HashSet<Unit> AllUnits;

        public PreMove(Dictionary<Hex, Unit> positions, HashSet<Unit> allUnits)
        {
            UnitsPositions = positions;
            AllUnits = allUnits;
        }
    }

    public class HexGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private readonly Dictionary<(int, int), Hex> _hexes = new();

        public static readonly int[][][] OddrDirectionDifferences =
        {
             // Even rows
             new int[][]
             {
                 new[] { 0, -1 }, new[] { 1, 0 }, new[] { 0, 1 },
                 new[] { -1, 1 }, new[] { -1, 0 }, new[] { -1, -1 }
             },
             // Odd rows
             new int[][]
             {
                 new[] { 1, -1 }, new[] { 1, 0 }, new[] { 1, 1 },
                 new[] { 0, 1 }, new[] { -1, 0 }, new[] { 0, -1 }
             }
        };

        private readonly Dictionary<Hex, Unit> _unitsPositions = new();
        private HashSet<Unit> _allUnits = new();

        public Stack<PreMove> Moves = new();

        public HexGrid(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            for (int r = 0; r < height; r++)
            {
                for (int q = 0; q < width - (r % 2 == 0 ? 0 : 1); q++)
                {
                    _hexes[(q, r)] = new Hex(q, r);
                }
            }
        }

        public Hex GetHex(Unit unit)
        {
            return GetHex(unit.Q, unit.R);
        }

        public Hex GetHex(int q, int r)
        {
            _hexes.TryGetValue((q, r), out Hex? hex);
            return hex;
        }

        public Unit GetUnitAtHex(Hex hex)
        {
            _unitsPositions.TryGetValue(hex, out Unit? unit);
            return unit;
        }

        public Side DirectionTo(Hex start, Hex neighbor)
        {
            int parity = start.R & 1;
            int dq = neighbor.Q - start.Q;
            int dr = neighbor.R - start.R;

            for (int i = 0; i < HexGrid.OddrDirectionDifferences[parity].Length; i++)
            {
                var diff = HexGrid.OddrDirectionDifferences[parity][i];
                if (dq == diff[0] && dr == diff[1])
                {
                    return (Side)i;
                }
            }

            throw new ArgumentException("Hexes are not adjacent");
        }

        public Dictionary<Side, Hex> GetNeighbors(Hex hex)
        {
            int parity = hex.R & 1;
            Dictionary<Side, Hex> neighbors = new();

            for (int i = 0; i < OddrDirectionDifferences[parity].Length; i++)
            {
                var diff = OddrDirectionDifferences[parity][i];
                Hex neighbor = GetHex(hex.Q + diff[0], hex.R + diff[1]);
                if (neighbor != null) neighbors[(Side)i] = neighbor;
            }

            return neighbors;
        }

        public Hex GetNeighbor(Hex hex, Side side)
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

            var directions = Enum.GetValues(typeof(Side));
            var neighbors = GetNeighbors(GetHex(unit));
            foreach (int dirValue in directions)
            {
                Side side = (Side)dirValue;
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

        private bool IsAdjacent(Hex a, Hex b)
        {
            try
            {
                DirectionTo(a, b);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool MoveUnit(Unit unit, Hex to)
        {
            if (!AllowedMoves(unit).Contains(to))
            {
                return false;
            }

            Hex from = GetHex(unit);

            Side moveDirection = DirectionTo(from, to);

            return PerformMovement(unit, from, to, moveDirection);
        }

        private bool PerformMovement(Unit unit, Hex currentPosition, Hex destination, Side moveDirection)
        {
            unit.SetRotation(moveDirection);

            if (IsVulnerableToSpear(unit, currentPosition))
            {
                KillUnit(unit);
                return true;
            }

            // Check if destination has an enemy unit that can be attacked
            if (_unitsPositions.TryGetValue(destination, out Unit targetUnit) && targetUnit.Player != unit.Player)
            {
                // Can only move to an occupied hex if the weapon in the move direction would kill the target
                if (!CanKillUnit(unit, targetUnit, moveDirection))
                    return false;

                // Kill the target unit
                KillUnit(targetUnit);
            }
            else if (_unitsPositions.ContainsKey(destination))
            {
                // Can't move to a hex occupied by a friendly unit
                return false;
            }

            ActivateWeaponEffectsBeforeMovement(unit, currentPosition, destination, moveDirection);

            _unitsPositions.Remove(currentPosition);

            _unitsPositions[destination] = unit;
            _unitsPositions.Remove(currentPosition);
            unit.SetPosition(destination);

            if (IsVulnerableToSpear(unit, destination))
            {
                KillUnit(unit);
                return true;
            }

            ActivateWeaponEffectsAfterMovement(unit, destination);

            return true;
        }

        private void ActivateWeaponEffectsBeforeMovement(Unit unit, Hex currentPosition, Hex destination, Side moveDirection)
        {
            Weapon weaponInMoveDirection = unit.GetItemOnSide((int)moveDirection);

            if (weaponInMoveDirection == Weapon.AXE || weaponInMoveDirection == Weapon.STRONG_AXE)
            {
                KillAdjacentEnemies(unit, destination);
            }

            if (weaponInMoveDirection == Weapon.BOW)
            {
                FireBow(unit, currentPosition, moveDirection);
            }

            if (weaponInMoveDirection == Weapon.PUSH)
            {
                PushUnit(unit, destination, moveDirection);
            }
        }

        private void ActivateWeaponEffectsAfterMovement(Unit unit, Hex position)
        {
            foreach (Side side in Enum.GetValues<Side>())
            {
                if (unit.GetItemOnSide((int)side) == Weapon.SPEAR)
                {
                    try
                    {
                        Hex targetPos = GetNeighbor(position,side);
                        if (_unitsPositions.TryGetValue(targetPos, out Unit targetUnit) &&
                            targetUnit.Player != unit.Player)
                        {
                            // Check if target has a shield
                            Side defendDirection = (Side)(((int)side + 3) % 6);
                            Weapon defendWeapon = targetUnit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != Weapon.SHIELD && defendWeapon != Weapon.STRONG_SHIELD)
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
            foreach (Side side in Enum.GetValues<Side>())
            {
                try
                {
                    Hex neighborPos = GetNeighbor(position,side);
                    if (_unitsPositions.TryGetValue(neighborPos, out Unit neighborUnit) &&
                        neighborUnit.Player != unit.Player)
                    {
                        unitsToKill.Add(neighborUnit);
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

        private void FireBow(Unit unit, Hex position, Side direction)
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
                    if (_unitsPositions.TryGetValue(currentPos, out Unit targetUnit))
                    {
                        // If it's an enemy unit, check for shield and kill if not shielded
                        if (targetUnit.Player != unit.Player)
                        {
                            Side defendDirection = (Side)(((int)direction + 3) % 6);
                            Weapon defendWeapon = targetUnit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != Weapon.SHIELD && defendWeapon != Weapon.STRONG_SHIELD)
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

        private void PushUnit(Unit pusher, Hex pushPosition, Side pushDirection)
        {
            // Check if there's a unit at the push position
            if (_unitsPositions.TryGetValue(pushPosition, out Unit targetUnit) &&
                targetUnit.Player != pusher.Player)
            {
                // Calculate the position the target will be pushed to
                Hex pushDestination;
                try
                {
                    pushDestination = GetNeighbor(pushPosition, pushDirection);

                    // If the push destination is occupied or off the board, the target unit dies
                    if (_unitsPositions.ContainsKey(pushDestination))
                    {
                        KillUnit(targetUnit);
                    }
                    else
                    {
                        // Move the target to the push destination
                        _unitsPositions.Remove(pushPosition);
                        _unitsPositions[pushDestination] = targetUnit;
                        targetUnit.SetPosition(pushDestination);

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

        private bool CanKillUnit(Unit attacker, Unit target, Side attackDirection)
        {
            Weapon attackWeapon = attacker.GetItemOnSide((int)attackDirection);

            // The opposite direction from the attack direction
            Side defendDirection = (Side)(((int)attackDirection + 3) % 6);

            // Get the weapon used for defense
            Weapon defendWeapon = target.GetItemOnSide((int)defendDirection);

            // If defender has a shield in the right direction, they're protected
            if (defendWeapon == Weapon.SHIELD || defendWeapon == Weapon.STRONG_SHIELD)
                return false;

            // AXE/STRONG_AXE can kill if not blocked by shield
            if (attackWeapon == Weapon.AXE || attackWeapon == Weapon.STRONG_AXE)
                return true;

            // PUSH can always move to occupied hex (it will push the enemy)
            if (attackWeapon == Weapon.PUSH) //todo
                return true;

            return false;
        }

        private bool IsVulnerableToSpear(Unit unit, Hex position)
        {
            // Check all adjacent hexes for enemy units with spears pointing at this unit
            foreach (Side side in Enum.GetValues<Side>())
            {
                try
                {
                    Hex neighborPos = GetNeighbor(position, side);
                    if (_unitsPositions.TryGetValue(neighborPos, out Unit neighborUnit) &&
                        neighborUnit.Player != unit.Player)
                    {
                        // The side pointing at our unit
                        Side pointingDirection = (Side)(((int)side + 3) % 6);

                        // Check if neighbor has a spear pointing at our unit
                        Weapon neighborWeapon = neighborUnit.GetItemOnSide((int)pointingDirection);
                        if (neighborWeapon == Weapon.SPEAR)
                        {
                            // Check if our unit has a shield in that direction
                            Side defendDirection = side;
                            Weapon defendWeapon = unit.GetItemOnSide((int)defendDirection);

                            if (defendWeapon != Weapon.SHIELD && defendWeapon != Weapon.STRONG_SHIELD)
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
            _unitsPositions.Remove(GetHex(unit));
            _allUnits.Remove(unit);
        }

        public HashSet<Unit> GetUnitsByPlayer(PLAYER player)
        {
            return _allUnits.Where(x => x.Player == player).ToHashSet();
        }

        public void PlaceUnit(int q, int r, Unit unit, Side facing)
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
            _allUnits.Add(unit);
            _unitsPositions[hex] = unit;
            unit.SetRotation(facing);
            _unitsPositions[hex] = unit;
            unit.SetPosition(hex);
        }

        public void PrintGrid()
        {
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
                            case Side.UP_RIGHT:
                                Console.ForegroundColor = originalColor;
                                Console.Write($@" /{q}");
                                Console.ForegroundColor = playerColor;
                                Console.Write(@"\  ");
                                break;
                            case Side.UP_LEFT:
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
                            case Side.LEFT:
                                Console.ForegroundColor = playerColor;
                                Console.Write(@$"|");
                                Console.ForegroundColor = originalColor;
                                Console.Write(@$" {unitId}| ");
                                break;
                            case Side.RIGHT:
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
                            case Side.DOWN_RIGHT:
                                Console.ForegroundColor = originalColor;
                                Console.Write(@" \");
                                Console.ForegroundColor = playerColor;
                                Console.Write($@"{r}/");
                                Console.ForegroundColor = originalColor;
                                Console.Write(@"  ");
                                break;
                            case Side.DOWN_LEFT:
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

        public HexGrid Clone()
        {
            return new HexGrid(this);
        }

        public HexGrid(HexGrid other)
        {
            Width = other.Width;
            Height = other.Height;

            _allUnits = new();
            _hexes = new Dictionary<(int, int), Hex>();
            foreach (var kvp in other._hexes)
            {
                Unit unit = GetUnitAtHex(kvp.Value);
                if(unit != null)
                {
                    Hex newHex = new Hex(kvp.Value.Q, kvp.Value.R);
                    Unit unitClone = unit.Clone();
                    _allUnits.Add(unitClone);
                    _unitsPositions[newHex] = unitClone;
                    unitClone.SetPosition(newHex);
                    _hexes[kvp.Key] = newHex;
                }
                else
                {
                    _hexes[kvp.Key] = new Hex(kvp.Value.Q, kvp.Value.R);
                }
            }

            _unitsPositions = new Dictionary<Hex, Unit>();
            foreach (var kvp in other._unitsPositions)
            {
                Hex hex = GetHex(kvp.Key.Q, kvp.Key.R);
                _unitsPositions[hex] = kvp.Value;
            }
        }
    }
}
