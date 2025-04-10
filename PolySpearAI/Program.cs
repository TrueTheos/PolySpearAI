using System.Text.Json;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using PolyspearLib;
namespace PolySpearAI
{
    internal class Program
    {
        public const string PRESET_FILE_PATH = "units.json";

        private static PolyspearBase _base;

        static void Main(string[] args)
        {
            _base = new PolyspearBase(5);

            if (!File.Exists(PRESET_FILE_PATH))
            {
                throw new Exception($"No units.json! at {Path.GetFullPath(PRESET_FILE_PATH)}");
            }
            UnitPreset preset = PresetLoader.LoadPresets(PRESET_FILE_PATH);

            foreach (var unit in preset.Units)
            {
                PolyspearLib.PolyspearBase.Units.Add(unit);
            }

            PolyspearLib.PolyspearBase.PlaceUnits(preset);

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
