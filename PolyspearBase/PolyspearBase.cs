using System;
using System.Text.Json.Serialization;

namespace PolyspearLib
{
    public enum PLAYER { ELF, ORC }
    public enum SIDE
    {
        UP_RIGHT = 0,
        RIGHT = 1,
        DOWN_RIGHT = 2,
        DOWN_LEFT = 3,
        LEFT = 4,
        UP_LEFT = 5
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WEAPON
    {
        EMPTY = 0,
        ATTACK_SHIELD = 1, //a
        AXE = 2, //x
        BOW = 3, //b
        FIST = 4, //f
        MACE = 5, //m
        PUSH = 6, //p
        STRONG_AXE = 7, //X
        STRONG_SHIELD = 8, //H
        SHIELD = 9, //h
        SPEAR = 10, //s
        STAFF = 11, //t
        SWORD = 12 //w
    }

    public class PolyspearBase
    {
        public HexGrid Grid { get; private set; }
        public PLAYER CurrentPlayer { get; private set; } = PLAYER.ELF;

        public HashSet<Unit> Units { get; private set; }
        public PolyspearBase(int size)
        {
            Units = new();
            Grid = new HexGrid(size, size);
        }

        public void LoadPreset(UnitPreset preset)
        {
            List<(Unit Unit, int Q, int R, SIDE Side)> placedUnits = new List<(Unit, int, int, SIDE)>();

            LoadPlacement(preset, placedUnits);

            int currentUnitIndex = 0;
            BoardState lastPlace = new BoardState(Grid);

            while (currentUnitIndex < preset.Units.Count)
            {
                Console.WriteLine();
                Unit currentUnit = preset.Units[currentUnitIndex];
                Console.Write($"Place {currentUnit.ID} (q r side player) or 'u' to undo: ");
                string input = Console.ReadLine();

                // Handle undo
                if (input.ToLower() == "u")
                {
                    if (lastPlace == null) continue;
                    if (currentUnitIndex > 0 && placedUnits.Count > 0)
                    {
                        var lastPlaced = placedUnits[placedUnits.Count - 1];
                        placedUnits.RemoveAt(placedUnits.Count - 1);
                        currentUnitIndex--;

                        Grid.SetBoardState(lastPlace);

                        Console.Clear();
                        Grid.PrintGrid();
                    }

                    continue;
                }

                lastPlace = new BoardState(Grid);

                try
                {
                    if (input.Length < 4)
                    {
                        throw new Exception("Input too short");
                    }

                    int q = int.Parse(input[0].ToString());
                    int r = int.Parse(input[1].ToString());
                    int sideValue = int.Parse(input[2].ToString());
                    int playerValue = int.Parse(input[3].ToString());

                    if (sideValue < 0 || sideValue > Enum.GetValues(typeof(SIDE)).Length - 1)
                    {
                        throw new Exception($"Invalid side value: {sideValue}");
                    }

                    if (playerValue != (int)PLAYER.ELF && playerValue != (int)PLAYER.ORC)
                    {
                        throw new Exception($"Invalid player value: {playerValue}");
                    }

                    Hex targetHex = Grid.GetHex(q, r);
                    if (targetHex == null)
                    {
                        throw new Exception($"Invalid hex coordinates: {q},{r}");
                    }

                    if (Grid.GetUnitAtHex(targetHex) != null)
                    {
                        throw new Exception($"Hex {q},{r} is already occupied");
                    }

                    currentUnit.Player = (PLAYER)playerValue;
                    Grid.PlaceUnit(q, r, currentUnit, (SIDE)sideValue);
                    placedUnits.Add((currentUnit, q, r, (SIDE)sideValue));
                    currentUnitIndex++;

                    Console.Clear();
                    Grid.PrintGrid();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid input: {ex.Message}. Please try again.");
                }
            }
        }

        private void LoadPlacement(UnitPreset preset, List<(Unit Unit, int Q, int R, SIDE Side)> placedUnits)
        {
            List<Unit> autoPlacedUnits = new List<Unit>();

            foreach (var placement in preset.Placements)
            {
                var unit = preset.Units.FirstOrDefault(u => u.ID == placement.UnitId);
                if (unit != null)
                {
                    Hex targetHex = Grid.GetHex(placement.Q, placement.R);
                    if (targetHex == null)
                    {
                        Console.WriteLine($"Warning: Invalid hex coordinates for unit {unit.ID} in placement.");
                        continue;
                    }
                    if (Grid.GetUnitAtHex(targetHex) != null)
                    {
                        Console.WriteLine($"Warning: Hex {placement.Q},{placement.R} already occupied. Skipping placement for unit {unit.ID}.");
                        continue;
                    }
                    unit.Player = placement.Player;
                    Grid.PlaceUnit(placement.Q, placement.R, unit, placement.Side);
                    placedUnits.Add((unit, placement.Q, placement.R, placement.Side));
                    autoPlacedUnits.Add(unit);
                }
                else
                {
                    Console.WriteLine($"Warning: No unit with ID {placement.UnitId} found for placement.");
                }
            }

            preset.Units = preset.Units.Except(autoPlacedUnits).ToList();
        }

        public void SetPlayer(PLAYER player) => CurrentPlayer = player;
    }
}
