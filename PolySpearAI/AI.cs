using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Unit;

namespace PolySpearAI
{
    public class AI
    {
        private readonly HexGrid _grid;
        private const int MAX_DEPTH = 7;
        private const int UNIT_VALUE = 100;

        private const int MIN_VALUE = int.MinValue + 1;
        private const int MAX_VALUE = int.MaxValue - 1;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public (Hex From, Hex To) FindBestMove()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Hex bestFrom = null;
            Hex bestTo = null;
            int bestScore = MIN_VALUE;
            PLAYER originalPlayer = Program.CurrentPlayer;

            List<Unit> playerUnits = GetPlayerUnits(originalPlayer);

            foreach (var unit in playerUnits)
            {
                Hex currentPos = _grid.GetHex(unit);
                List<Hex> moves = _grid.AllowedMoves(unit);

                foreach (var move in moves)
                {
                    PreMove previousMove = new PreMove(_grid);

                    Hex simulatedTo = _grid.GetHex(move.Q, move.R);
                    _grid.MoveUnit(unit, simulatedTo);

                    int score = Minimax(MAX_DEPTH - 1, MIN_VALUE, MAX_VALUE, originalPlayer, Program.GetEnemyPlayer(originalPlayer));

                    // Revert the move.
                    _grid.ApplyMove(previousMove);

                    // If this move yields a better score, update our best move.
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFrom = currentPos;
                        bestTo = move;
                    }
                }
            }

            stopwatch.Stop();

            if (bestFrom != null)
            {
                Console.WriteLine($"\nAI Suggestion: Move from ({bestFrom.Q},{bestFrom.R}) to ({bestTo.Q},{bestTo.R})");
                Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                Console.WriteLine("\nAI Suggestion: No valid moves available.");
            }

            return (bestFrom, bestTo);
        }

        private int Minimax(int depth, int alpha, int beta, PLAYER originalPlayer, PLAYER playerToMove)
        {
            if (depth == 0 || IsGameOver())
            {
                return EvaluatePosition(originalPlayer);
            }

            if (playerToMove == originalPlayer)
            {
                int maxEval = MIN_VALUE;
                foreach (var unit in GetPlayerUnits(playerToMove))
                {
                    foreach (var move in _grid.AllowedMoves(unit))
                    {
                        PreMove previousMove = new PreMove(_grid);
                        Hex simulatedTo = _grid.GetHex(move.Q, move.R);
                        _grid.MoveUnit(unit, simulatedTo);

                        int eval = Minimax(depth - 1, alpha, beta, originalPlayer, Program.GetEnemyPlayer(playerToMove));

                        _grid.ApplyMove(previousMove);

                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);

                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                }
                return maxEval;
            }
            else
            {
                int minEval = MAX_VALUE;
                foreach (var unit in GetPlayerUnits(playerToMove))
                {
                    foreach (var move in _grid.AllowedMoves(unit))
                    {
                        PreMove previousMove = new PreMove(_grid);
                        Hex simulatedTo = _grid.GetHex(move.Q, move.R);
                        _grid.MoveUnit(unit, simulatedTo);

                        int eval = Minimax(depth - 1, alpha, beta, originalPlayer, Program.GetEnemyPlayer(playerToMove));

                        _grid.ApplyMove(previousMove);

                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);

                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                }
                return minEval;
            }
        }

        private List<Unit> GetPlayerUnits(PLAYER player)
        {
            return _grid.GetUnitsByPlayer(player).ToList();
        }

        private bool IsGameOver()
        {
            bool player0HasUnits = _grid.GetUnitsByPlayer(PLAYER.ELF).Any();
            bool player1HasUnits = _grid.GetUnitsByPlayer(PLAYER.ORC).Any();

            return !player0HasUnits || !player1HasUnits;
        }

        private int EvaluatePosition(PLAYER aiPlayer)
        {
            int score = 0;
            PLAYER enemyPlayer = Program.GetEnemyPlayer(aiPlayer);

            int aiUnitCount = _grid.GetUnitsByPlayer(aiPlayer).Count;
            int enemyUnitCount = _grid.GetUnitsByPlayer(enemyPlayer).Count;

            score += (aiUnitCount - enemyUnitCount) * UNIT_VALUE * 2;

            if (enemyUnitCount == 0)
                return MAX_VALUE;
            if (aiUnitCount == 0)
                return MIN_VALUE;

            return score;
        }
    }
}
