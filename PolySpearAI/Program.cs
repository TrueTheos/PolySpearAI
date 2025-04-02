using System.Text.Json;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace PolySpearAI
{
    internal class Program
    {
        public const string PRESET_FILE_PATH = "units.json";
        public static PLAYER CurrentPlayer = PLAYER.ELF;

        static void Main(string[] args)
        {
            HexGrid grid = new HexGrid(5, 5);
            grid.PrintGrid();

            if(!File.Exists(PRESET_FILE_PATH))
            {
                throw new Exception($"No units.json! at {Path.GetFullPath(PRESET_FILE_PATH)}");
            }
            UnitPreset preset = PresetLoader.LoadPresets(PRESET_FILE_PATH);

            foreach (var unit in preset.Units)
            {
                Console.WriteLine();
                Console.Write($"Place {unit.ID} (q r side player): ");
                string pos = Console.ReadLine();

                int q = int.Parse(pos[0].ToString());
                int r = int.Parse(pos[1].ToString());

                unit.Player = (PLAYER)int.Parse(pos[3].ToString());
                grid.PlaceUnit(q, r, unit, (Side)int.Parse(pos[2].ToString()));
                Console.Clear();
                grid.PrintGrid();
            }

            while (true)
            {
                Console.Clear();
                grid.PrintGrid();
                Console.WriteLine($"Curent player: {CurrentPlayer}\n");
                AI ai = new AI(grid);
                Stopwatch stopwatch = Stopwatch.StartNew();
                (Hex from, Hex to) = ai.FindBestMove();
                stopwatch.Stop();

                if (from != null)
                {
                    Console.WriteLine($"\nAI Suggestion: Move from ({from.Q},{from.R}) to ({to.Q},{to.R})");
                    Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    Console.WriteLine("\nAI Suggestion: No valid moves available.");
                }


                Console.WriteLine("\nEnter unit coordinates to move (q,r), 'u' to undo, 's' to skip, 'exit':");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit")
                    break;
                if(input.ToLower() == "s")
                {
                    ChangePlayer();
                    continue;
                }
                if (input.ToLower() == "u")
                {
                    if (grid.UndoMove()) ChangePlayer();
                    grid.MoveHistory = new();
                    continue;
                }

                string[] coords = input.Split(',');
                if (coords.Length < 2 ||
                    !int.TryParse(coords[0], out int q) ||
                    !int.TryParse(coords[1], out int r))
                {
                    Console.WriteLine("Invalid coordinates.");
                    continue;
                }

                Hex selectedHex = grid.GetHex(q, r);
                Unit selectedUnit = grid.GetUnitAtHex(selectedHex);
                if (selectedHex == null || selectedUnit == null || selectedUnit.Player != CurrentPlayer)
                {
                    Console.WriteLine("No valid unit at that location for the current player.");
                    continue;
                }

                List<Hex> allowedMoves = grid.AllowedMoves(selectedUnit);
                if (allowedMoves.Count == 0)
                {
                    Console.WriteLine("No allowed moves for that unit.");
                    continue;
                }

                var directionMapping = new Dictionary<Side, Hex>();
                foreach (Side side in Enum.GetValues(typeof(Side)))
                {
                    Hex neighbor = grid.GetNeighbor(selectedHex,side);
                    if (allowedMoves.Contains(neighbor))
                        directionMapping[side] = neighbor;
                }

                Console.WriteLine("\nAllowed Moves:");
                int index = 0;
                var allowedList = new List<(Side direction, Hex hex)>();
                foreach (var kvp in directionMapping)
                {
                    allowedList.Add((kvp.Key, kvp.Value));
                    Console.WriteLine($"{index}: {kvp.Key} -> Hex ({kvp.Value.Q},{kvp.Value.R})");
                    index++;
                }

                Console.Write("Select move index: ");
                string indexInput = Console.ReadLine();
                if (!int.TryParse(indexInput, out int moveIndex) || moveIndex < 0 || moveIndex >= allowedList.Count)
                {
                    Console.WriteLine("Invalid move index.");
                    continue;
                }

                Hex destination = allowedList[moveIndex].hex;

                // Attempt to move the unit.
                bool success = grid.MoveUnit(selectedUnit, destination);
                if (success) ChangePlayer();

                grid.PrintGrid();
            }
        }

        private static void ChangePlayer()
        {
            CurrentPlayer = CurrentPlayer == PLAYER.ELF ? PLAYER.ORC : PLAYER.ELF;
        }

        public static PLAYER GetEnemyPlayer(PLAYER curr)
        {
            return curr == PLAYER.ELF ? PLAYER.ORC : PLAYER.ELF;
        }
    }

    public static class PresetLoader
    {
        public static UnitPreset LoadPresets(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var presets = JsonSerializer.Deserialize<UnitPreset>(json);
            return presets;
        }

        public static void SaveUnit(Unit unit)
        {
            UnitPreset preset;

            if (File.Exists(Program.PRESET_FILE_PATH))
            {
                string json = File.ReadAllText(Program.PRESET_FILE_PATH);
                preset = JsonSerializer.Deserialize<UnitPreset>(json);
                if (preset == null || preset.Units == null)
                {
                    preset = new UnitPreset();
                }
            }
            else
            {
                preset = new UnitPreset();
            }

            preset.Units.Add(unit);

            string outputJson = JsonSerializer.Serialize(preset);
            File.WriteAllText(Program.PRESET_FILE_PATH, outputJson);
        }
    }

    public class UnitPreset
    {
        public List<Unit> Units { get; set; }

        public UnitPreset()
        {
            Units = new();
        }
    }
}
