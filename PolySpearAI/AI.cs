using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    public class AI
    {
        private readonly HexGrid _grid;
        private const int MAX_DEPTH = 3;
        private const int UNIT_VALUE = 100;

        private const int MIN_VALUE = int.MinValue + 1;
        private const int MAX_VALUE = int.MaxValue - 1;

        public AI(HexGrid grid)
        {
            _grid = grid;
        }

        public (Hex From, Hex To) FindBestMove()
        {
            Hex bestFrom = default;
            Hex bestTo = default;
            int bestScore = MIN_VALUE;
            int alpha = MIN_VALUE;
            int beta = MAX_VALUE;
            PLAYER aiPlayer = Program.CurrentPlayer;

            var playerUnits = GetPlayerUnits(aiPlayer);
            var originalUnitPositions = new Dictionary<string, Hex>();

            foreach (var unit in playerUnits)
            {
                originalUnitPositions[unit.ID] = _grid.GetHex(unit);
            }

            foreach (var unit in playerUnits)
            {
                Hex unitCurrentPos = _grid.GetHex(unit);
                var moves = _grid.AllowedMoves(unit);

                foreach (var move in moves)
                {
                    Hex simulatedTo = _grid.GetHex(move.Q, move.R);

                    var previousMove = new PreMove(_grid);
                    _grid.MoveUnit(unit, simulatedTo);

                    int score = Minimax(MAX_DEPTH - 1, alpha, beta, false, aiPlayer);

                    _grid.ApplyMove(previousMove);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFrom = unitCurrentPos;
                        bestTo = move;
                    }
                    alpha = Math.Max(alpha, bestScore);
                }
            }

            return (bestFrom, bestTo);
        }

        private class MoveEvaluation
        {
            public int Score { get; set; }
            public Hex From { get; set; }
            public Hex To { get; set; }
        }

        private int Minimax(int depth, int alpha, int beta, bool maximizingPlayer, PLAYER aiPlayer)
        {
            PLAYER currentPlayer = maximizingPlayer ? aiPlayer : Program.GetEnemyPlayer(aiPlayer);

            // Base case: depth reached or game over
            if (depth == 0 || IsGameOver())
            {
                return EvaluatePosition(aiPlayer);
            }

            if (maximizingPlayer)
            {
                int maxEval = MIN_VALUE;
                var playerUnits = GetPlayerUnits(currentPlayer);

                foreach (var unit in playerUnits)
                {
                    var moves = _grid.AllowedMoves(unit);

                    foreach (var move in moves)
                    {
                        Hex simulatedTo = _grid.GetHex(move.Q, move.R);

                        var previousMove = new PreMove(_grid);
                        _grid.MoveUnit(unit, simulatedTo);
                        int eval = Minimax(depth - 1, alpha, beta, false, aiPlayer);

                        _grid.ApplyMove(previousMove);

                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);

                        if (beta <= alpha)
                            break; // Alpha-beta pruning
                    }
                }

                return maxEval == MIN_VALUE ? EvaluatePosition(aiPlayer) : maxEval;
            }
            else
            {
                int minEval = MAX_VALUE;
                var playerUnits = GetPlayerUnits(currentPlayer);

                foreach (var unit in playerUnits)
                {
                    var moves = _grid.AllowedMoves(unit);

                    foreach (var move in moves)
                    {
                        Hex simulatedTo = _grid.GetHex(move.Q, move.R);

                        var previousMove = new PreMove(_grid);
                        _grid.MoveUnit(unit, simulatedTo);
                        int eval = Minimax( depth - 1, alpha, beta, true, aiPlayer);

                        _grid.ApplyMove(previousMove);

                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);

                        if (beta <= alpha)
                            break; // Alpha-beta pruning
                    }
                }

                return minEval == MAX_VALUE ? EvaluatePosition(aiPlayer) : minEval;
            }
        }

        private List<Unit> GetPlayerUnits(PLAYER player)
        {
            List<Unit> units = _grid.GetUnitsByPlayer(player).ToList();

            return units;
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
