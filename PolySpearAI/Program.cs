using System.Text.Json;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using static PolySpearAI.Unit;
using static PolySpearAI.HexGrid;
using static PolySpearAI.AI;

namespace PolySpearAI
{
    internal class Program
    {
        public const string PRESET_FILE_PATH = "units.json";
        public static PLAYER CurrentPlayer { get; set; } = PLAYER.ELF;

        public static HashSet<Unit> Units { get; private set; } = new();

        private static HexGrid _grid;
        static void Main(string[] args)
        {
            _grid = new HexGrid(5, 5);
            _grid.PrintGrid();

            if (!File.Exists(PRESET_FILE_PATH))
            {
                throw new Exception($"No units.json! at {Path.GetFullPath(PRESET_FILE_PATH)}");
            }
            UnitPreset preset = PresetLoader.LoadPresets(PRESET_FILE_PATH);

            foreach (var unit in preset.Units)
            {
                Units.Add(unit);
            }

            PlaceUnits(preset);

            Console.WriteLine("Autoplay? (y/n): ");
            string autoplay = Console.ReadLine();

            if (autoplay.ToLower() == "y")
            {
                AILoop();
            }
            else
            {
                GameLoop();
            }
        }

        private static void PlaceUnits(UnitPreset preset)
        {
            List<(Unit Unit, int Q, int R, SIDE Side)> placedUnits = new List<(Unit, int, int, SIDE)>();

            if (preset.Placements != null && preset.Placements.Count > 0)
            {
                Console.Write("Initial placements found. Do you want to load them? (y/n): ");
                string loadInput = Console.ReadLine();
                if (loadInput.Trim().ToLower() == "y")
                {
                    LoadPlacement(preset, placedUnits);
                }
            }

            int currentUnitIndex = 0;
            BoardState lastPlace = new BoardState(_grid);

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

                        _grid.SetBoardState(lastPlace);

                        Console.Clear();
                        _grid.PrintGrid();
                    }

                    continue;
                }

                lastPlace = new BoardState(_grid);

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

                    Hex targetHex = _grid.GetHex(q, r);
                    if (targetHex == null)
                    {
                        throw new Exception($"Invalid hex coordinates: {q},{r}");
                    }

                    if (_grid.GetUnitAtHex(targetHex) != null)
                    {
                        throw new Exception($"Hex {q},{r} is already occupied");
                    }

                    currentUnit.Player = (PLAYER)playerValue;
                    _grid.PlaceUnit(q, r, currentUnit, (SIDE)sideValue);
                    placedUnits.Add((currentUnit, q, r, (SIDE)sideValue));
                    currentUnitIndex++;

                    Console.Clear();
                    _grid.PrintGrid();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid input: {ex.Message}. Please try again.");
                }
            }
        }

        private static void LoadPlacement(UnitPreset preset, List<(Unit Unit, int Q, int R, SIDE Side)> placedUnits)
        {
            List<Unit> autoPlacedUnits = new List<Unit>();

            foreach (var placement in preset.Placements)
            {
                var unit = preset.Units.FirstOrDefault(u => u.ID == placement.UnitId);
                if (unit != null)
                {
                    Hex targetHex = _grid.GetHex(placement.Q, placement.R);
                    if (targetHex == null)
                    {
                        Console.WriteLine($"Warning: Invalid hex coordinates for unit {unit.ID} in placement.");
                        continue;
                    }
                    if (_grid.GetUnitAtHex(targetHex) != null)
                    {
                        Console.WriteLine($"Warning: Hex {placement.Q},{placement.R} already occupied. Skipping placement for unit {unit.ID}.");
                        continue;
                    }
                    unit.Player = placement.Player;
                    _grid.PlaceUnit(placement.Q, placement.R, unit, placement.Side);
                    placedUnits.Add((unit, placement.Q, placement.R, placement.Side));
                    autoPlacedUnits.Add(unit);
                }
                else
                {
                    Console.WriteLine($"Warning: No unit with ID {placement.UnitId} found for placement.");
                }
            }

            preset.Units = preset.Units.Except(autoPlacedUnits).ToList();

            Console.Clear();
            _grid.PrintGrid();
        }

        private static void GameLoop()
        {
            BoardState lastState = new BoardState(_grid);

            while (true)
            {
                Console.Clear();
                _grid.PrintGrid();
                Console.WriteLine($"Curent player: {CurrentPlayer}\n");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                AI ai = new AI(_grid);
                BestMove bestMove = ai.FindBestMove(CurrentPlayer);

                stopwatch.Stop();

                if (bestMove.From != null)
                {
                    Console.WriteLine($"\nAI Suggestion: Move from ({bestMove.From.Q},{bestMove.From.R}) to ({bestMove.To.Q},{bestMove.To.R})");
                    Console.WriteLine($"Eval: {bestMove.CurrentEval} -> {bestMove.EvalAfterMove}");
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
                else if (input.ToLower() == "s")
                {
                    ChangePlayer();
                    continue;
                }
                else if (input.ToLower() == "u")
                {
                    if (lastState != null)
                    {
                        _grid.SetBoardState(lastState);
                        lastState = null;
                        ChangePlayer();
                    }
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

                Hex selectedHex = _grid.GetHex(q, r);
                Unit selectedUnit = _grid.GetUnitAtHex(selectedHex);
                if (selectedHex == null || selectedUnit == null || selectedUnit.Player != CurrentPlayer)
                {
                    Console.WriteLine("No valid unit at that location for the current player.");
                    continue;
                }

                List<Hex> allowedMoves = _grid.AllowedMoves(selectedUnit);
                if (allowedMoves.Count == 0)
                {
                    Console.WriteLine("No allowed moves for that unit.");
                    continue;
                }

                var directionMapping = new Dictionary<SIDE, Hex>();
                foreach (SIDE side in Enum.GetValues(typeof(SIDE)))
                {
                    Hex neighbor = _grid.GetNeighbor(selectedHex, side);
                    if (allowedMoves.Contains(neighbor))
                        directionMapping[side] = neighbor;
                }

                Console.WriteLine("\nAllowed Moves:");
                int index = 0;
                var allowedList = new List<(SIDE direction, Hex hex)>();
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

                bool success = _grid.MoveUnit(selectedUnit, destination);
                if (success)
                {
                    lastState = new BoardState(_grid);
                    ChangePlayer();
                }

                _grid.PrintGrid();
            }
        }

        private static void AILoop()
        {
            long totalTime = 0;
            int moveCount = 0;

            while (true)
            {
                Console.Clear();
                _grid.PrintGrid();
                Console.WriteLine($"Current player: {CurrentPlayer}\n");
                if(moveCount > 0) Console.WriteLine($"Average time per move: {totalTime / moveCount} ms");

                AI ai = new AI(_grid);

                Stopwatch stopwatch = Stopwatch.StartNew();
                BestMove bestMove = ai.FindBestMove(CurrentPlayer);
                stopwatch.Stop();

                totalTime += stopwatch.ElapsedMilliseconds;
                moveCount++;

                if (!_grid.IsGameOver() && bestMove.From != null)
                {
                    bool success = _grid.MoveUnit(_grid.GetUnitAtHex(bestMove.From), bestMove.To);
                    if (success)
                    {
                        ChangePlayer();
                    }
                }
                else
                {
                    if (_grid.IsGameOver())
                    {
                        Console.WriteLine($"GAME ENDED {_grid.GetWinner()} won!");
                    }
                    else
                    {
                        Console.WriteLine("\nAI Suggestion: No valid moves available.");
                    }
                    break;
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        public static void ChangePlayer()
        {
            CurrentPlayer = CurrentPlayer == PLAYER.ELF ? PLAYER.ORC : PLAYER.ELF;
        }

        public static PLAYER GetEnemyPlayer(PLAYER curr)
        {
            return curr == PLAYER.ELF ? PLAYER.ORC : PLAYER.ELF;
        }
    }
}
